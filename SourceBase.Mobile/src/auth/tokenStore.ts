import * as SecureStore from "expo-secure-store";

const ACCESS = "sb.accessToken";
const REFRESH = "sb.refreshToken";

export const tokenStore = {
  async get() {
    const [accessToken, refreshToken] = await Promise.all([
      SecureStore.getItemAsync(ACCESS),
      SecureStore.getItemAsync(REFRESH),
    ]);
    return { accessToken, refreshToken };
  },
  async set(accessToken: string, refreshToken: string) {
    await Promise.all([
      SecureStore.setItemAsync(ACCESS, accessToken),
      SecureStore.setItemAsync(REFRESH, refreshToken),
    ]);
  },
  async clear() {
    await Promise.all([
      SecureStore.deleteItemAsync(ACCESS),
      SecureStore.deleteItemAsync(REFRESH),
    ]);
  },
};
