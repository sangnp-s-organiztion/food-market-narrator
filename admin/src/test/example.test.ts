import { afterEach, describe, expect, it, vi } from "vitest";
import { analyticsApi } from "@/lib/analyticsApi";
import { authApi } from "@/lib/authApi";

describe("admin api clients", () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('getMovementPaths sends 0 when sessionLimit is "all"', async () => {
    const fetchSpy = vi.spyOn(globalThis, "fetch").mockResolvedValue({
      ok: true,
      json: async () => ({ sessions: [] }),
    } as Response);

    await analyticsApi.getMovementPaths("all");

    expect(fetchSpy).toHaveBeenCalledTimes(1);
    const [url, options] = fetchSpy.mock.calls[0];
    expect(String(url)).toContain(
      "/api/analytics/movement-paths?sessionLimit=0",
    );
    expect(options).toMatchObject({ credentials: "include" });
  });

  it("login throws unified invalid-credentials message on any non-ok response", async () => {
    vi.spyOn(globalThis, "fetch").mockResolvedValue({
      ok: false,
      status: 401,
      statusText: "Unauthorized",
    } as Response);

    await expect(
      authApi.login({ username: "admin", password: "wrong" }),
    ).rejects.toThrow("Thông tin đăng nhập không hợp lệ");
  });
});
