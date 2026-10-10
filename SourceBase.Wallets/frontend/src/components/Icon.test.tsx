import { render } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { Icon } from "./Icon";

describe("Icon", () => {
  it("renders an emoji as text", () => {
    const { container } = render(<Icon value="👛" />);
    expect(container.textContent).toBe("👛");
  });

  it("renders an image url as <img>", () => {
    const { container } = render(<Icon value="https://example.com/a.png" />);
    expect(container.querySelector("img")?.getAttribute("src")).toBe("https://example.com/a.png");
  });

  it("wraps an svg path fragment in an <svg>", () => {
    const { container } = render(<Icon value='<path d="M0 0h24v24H0z" />' />);
    expect(container.querySelector("svg path")).not.toBeNull();
  });

  it("falls back when there is no value", () => {
    const { container } = render(<Icon value={undefined} fallback="💰" />);
    expect(container.textContent).toBe("💰");
  });

  it("renders nothing without value or fallback", () => {
    const { container } = render(<Icon value={null} />);
    expect(container.firstChild).toBeNull();
  });
});
