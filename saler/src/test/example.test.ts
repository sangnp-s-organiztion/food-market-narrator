import { afterEach, describe, expect, it, vi } from "vitest";
import {
  getRestaurantDishesApi,
  getRestaurantImagesApi,
  loginApi,
  updateMyPasswordApi,
} from "@/services/api";

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

  it("getRestaurantDishesApi uses public endpoint and maps imageFileName", async () => {
    const fetchSpy = vi.spyOn(globalThis, "fetch").mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => [
        {
          dishId: 7,
          name: "Bún bò",
          price: 65000,
          restaurantId: "rest-001",
          imageId: 11,
          imageFileName: "bun_bo.jpg",
          createdAt: "2026-01-01T00:00:00Z",
        },
      ],
    } as Response);

    const dishes = await getRestaurantDishesApi("rest-001");

    expect(fetchSpy).toHaveBeenCalledTimes(1);
    const [url] = fetchSpy.mock.calls[0];
    expect(String(url)).toContain("/public/Restaurant/rest-001/dishes");

    expect(dishes).toHaveLength(1);
    expect(dishes[0].image_url).toBe(
      "http://localhost:5044/maui-images/bun_bo.jpg",
    );
  });

  it("updateMyPasswordApi calls /Auth/password with old/new password", async () => {
    const fetchSpy = vi.spyOn(globalThis, "fetch").mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => ({ message: "Password updated" }),
    } as Response);

    await updateMyPasswordApi(2, "old-123", "new-123");

    expect(fetchSpy).toHaveBeenCalledTimes(1);
    const [url, options] = fetchSpy.mock.calls[0];
    expect(String(url)).toContain("/Auth/password");
    expect(options).toMatchObject({ method: "PATCH", credentials: "include" });
    expect(String(options?.body)).toContain("oldPassword");
    expect(String(options?.body)).toContain("newPassword");
  });
});
