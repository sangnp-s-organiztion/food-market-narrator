import { afterEach, describe, expect, it, vi } from "vitest";
import { getRestaurantImagesApi, loginApi } from "@/services/api";

describe("saler api client", () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("getRestaurantImagesApi uses canonical /Restaurant/{id}/images endpoint", async () => {
    const fetchSpy = vi.spyOn(globalThis, "fetch").mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => [],
    } as Response);

    await getRestaurantImagesApi("rest-001");

    expect(fetchSpy).toHaveBeenCalledTimes(1);
    const [url] = fetchSpy.mock.calls[0];
    expect(String(url)).toContain("/Restaurant/rest-001/images");
  });

  it("loginApi maps role from backend response", async () => {
    vi.spyOn(globalThis, "fetch").mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => ({
        userId: 2,
        username: "seller1",
        role: "saler",
      }),
    } as Response);

    const user = await loginApi("seller1", "seller123");
    expect(user).toEqual({
      user_id: 2,
      username: "seller1",
      role: "saler",
    });
  });

  it("getRestaurantImagesApi normalizes relative image path to absolute URL", async () => {
    vi.spyOn(globalThis, "fetch").mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => [
        {
          imageId: 10,
          restaurantId: "rest-001",
          imageUrl: "pho_bo.png",
          isPrimary: true,
          sortOrder: 0,
        },
      ],
    } as Response);

    const images = await getRestaurantImagesApi("rest-001");

    expect(images).toHaveLength(1);
    expect(images[0].image_url).toBe(
      "http://localhost:5044/maui-images/pho_bo.png",
    );
  });
});
