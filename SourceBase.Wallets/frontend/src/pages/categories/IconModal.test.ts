import { describe, expect, it } from "vitest";
import { detectMode } from "./IconModal";

describe("detectMode", () => {
  it("detects svg, image and emoji values", () => {
    expect(detectMode('  <path d="M0" />')).toBe("svg");
    expect(detectMode("https://cdn.example.com/a.png")).toBe("image");
    expect(detectMode("💳")).toBe("emoji");
  });
});
