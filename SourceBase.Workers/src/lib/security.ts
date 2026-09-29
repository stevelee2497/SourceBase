import { jwtVerify, SignJWT } from "jose";
import type { AppBindings, AuthenticatedUser } from "../types";
import { UnauthorizedError } from "./errors";

const encoder = new TextEncoder();
const otpMax = 1_000_000;
const otpMin = 100_000;
const passwordIterations = 600_000;

interface TokenPayload extends AuthenticatedUser {
  tokenType: "access" | "refresh";
}

export interface LoginResponse {
  tokenType: string;
  accessToken: string;
  expiresIn: number;
  refreshToken: string;
}

export async function hashPassword(password: string) {
  const salt = crypto.getRandomValues(new Uint8Array(16));
  const hash = await derivePasswordBits(password, salt);
  return `pbkdf2$sha256$${passwordIterations}$${bytesToBase64(salt)}$${bytesToBase64(hash)}`;
}

export async function verifyPassword(storedHash: string, password: string) {
  const [algorithm, hashName, iterationsText, saltText, hashText] = storedHash.split("$");
  if (algorithm !== "pbkdf2" || hashName !== "sha256" || !iterationsText || !saltText || !hashText) return false;

  const salt = base64ToBytes(saltText);
  const expected = base64ToBytes(hashText);
  const actual = await derivePasswordBits(password, salt, Number(iterationsText));
  if (expected.length !== actual.length) return false;

  let diff = 0;
  for (let index = 0; index < expected.length; index += 1) diff |= expected[index] ^ actual[index];
  return diff === 0;
}

export function generateOtp() {
  const bytes = crypto.getRandomValues(new Uint32Array(1));
  return String((bytes[0] % (otpMax - otpMin)) + otpMin);
}

export function generateSecurityStamp() {
  return crypto.randomUUID();
}

export function getOtpExpiryIso(bindings: AppBindings) {
  const minutes = Number(bindings.OTP_TOKEN_EXPIRATION_MINUTES ?? "15");
  return new Date(Date.now() + minutes * 60 * 1000).toISOString();
}

export async function createLoginResponse(bindings: AppBindings, user: AuthenticatedUser): Promise<LoginResponse> {
  const accessTokenTtl = getAccessTokenSeconds(bindings);
  const refreshTokenTtl = getRefreshTokenSeconds(bindings);
  const accessToken = await signToken(bindings.JWT_SECRET, user, "access", accessTokenTtl);
  const refreshToken = await signToken(bindings.REFRESH_TOKEN_SECRET, user, "refresh", refreshTokenTtl);

  return {
    tokenType: "Bearer",
    accessToken,
    expiresIn: accessTokenTtl,
    refreshToken
  };
}

export async function verifyAccessToken(bindings: AppBindings, token: string) {
  return verifyToken(bindings.JWT_SECRET, token, "access");
}

export async function verifyRefreshToken(bindings: AppBindings, token: string) {
  return verifyToken(bindings.REFRESH_TOKEN_SECRET, token, "refresh");
}

function getAccessTokenSeconds(bindings: AppBindings) {
  return Number(bindings.ACCESS_TOKEN_EXPIRES_IN ?? "3600");
}

function getRefreshTokenSeconds(bindings: AppBindings) {
  return Number(bindings.REFRESH_TOKEN_EXPIRES_IN ?? "2592000");
}

async function signToken(secret: string, user: AuthenticatedUser, tokenType: TokenPayload["tokenType"], ttlSeconds: number) {
  const key = encoder.encode(secret);
  const now = Math.floor(Date.now() / 1000);
  return new SignJWT({
    email: user.email,
    userName: user.userName,
    roles: user.roles,
    securityStamp: user.securityStamp,
    tokenType
  })
    .setProtectedHeader({ alg: "HS256" })
    .setSubject(user.userId)
    .setIssuedAt(now)
    .setExpirationTime(now + ttlSeconds)
    .setIssuer("sourcebase-workers")
    .setAudience("sourcebase")
    .sign(key);
}

async function verifyToken(secret: string, token: string, tokenType: TokenPayload["tokenType"]): Promise<AuthenticatedUser> {
  try {
    const key = encoder.encode(secret);
    const verified = await jwtVerify(token, key, {
      issuer: "sourcebase-workers",
      audience: "sourcebase"
    });

    if (verified.payload.tokenType !== tokenType) throw new UnauthorizedError("Invalid token");
    if (typeof verified.payload.sub !== "string") throw new UnauthorizedError("Invalid token");
    if (typeof verified.payload.email !== "string") throw new UnauthorizedError("Invalid token");
    if (typeof verified.payload.userName !== "string") throw new UnauthorizedError("Invalid token");
    if (typeof verified.payload.securityStamp !== "string") throw new UnauthorizedError("Invalid token");
    if (!Array.isArray(verified.payload.roles) || verified.payload.roles.some(role => typeof role !== "string"))
      throw new UnauthorizedError("Invalid token");

    return {
      userId: verified.payload.sub,
      email: verified.payload.email,
      userName: verified.payload.userName,
      roles: verified.payload.roles,
      securityStamp: verified.payload.securityStamp
    };
  } catch {
    throw new UnauthorizedError("Invalid token");
  }
}

async function derivePasswordBits(password: string, salt: Uint8Array, iterations = passwordIterations) {
  const key = await crypto.subtle.importKey("raw", encoder.encode(password), "PBKDF2", false, ["deriveBits"]);
  const normalizedSalt = Uint8Array.from(salt);
  const bits = await crypto.subtle.deriveBits(
    { name: "PBKDF2", hash: "SHA-256", salt: normalizedSalt, iterations },
    key,
    256
  );
  return new Uint8Array(bits);
}

function bytesToBase64(bytes: Uint8Array) {
  let binary = "";
  for (const value of bytes) binary += String.fromCharCode(value);
  return btoa(binary);
}

function base64ToBytes(value: string) {
  const binary = atob(value);
  const bytes = new Uint8Array(binary.length);
  for (let index = 0; index < binary.length; index += 1) bytes[index] = binary.charCodeAt(index);
  return bytes;
}
