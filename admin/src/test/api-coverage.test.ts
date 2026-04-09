import { afterEach, describe, expect, it, vi } from "vitest";
import { analyticsApi } from "@/lib/analyticsApi";
import { auditApi } from "@/lib/auditApi";
import {
  adminStatsApi,
  mapsApi,
  tourApi,
  translationBillingApi,
} from "@/lib/adminApi";

describe("admin api coverage", () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("analyticsApi.getHeatmap sends hours query", async () => {
    const fetchSpy = vi.spyOn(globalThis, "fetch").mockResolvedValue({
      ok: true,
      json: async () => ({ points: [], count: 0 }),
    } as Response);

    await analyticsApi.getHeatmap(48);

    expect(fetchSpy).toHaveBeenCalledTimes(1);
    const [url, options] = fetchSpy.mock.calls[0];
    expect(String(url)).toContain("/api/analytics/heatmap?hours=48");
    expect(options).toMatchObject({ credentials: "include" });
  });

  it("auditApi.getLogs builds query from provided filters", async () => {
    const fetchSpy = vi.spyOn(globalThis, "fetch").mockResolvedValue({
      ok: true,
      json: async () => ({ items: [], totalCount: 0, page: 1, pageSize: 20 }),
    } as Response);

    await auditApi.getLogs({
      page: 2,
      pageSize: 50,
      action: "MOBILE_PLAY",
      targetType: "AudioLogs",
    });

    const [url] = fetchSpy.mock.calls[0];
    const finalUrl = String(url);
    expect(finalUrl).toContain("/api/audit-logs?");
    expect(finalUrl).toContain("page=2");
    expect(finalUrl).toContain("pageSize=50");
    expect(finalUrl).toContain("action=MOBILE_PLAY");
    expect(finalUrl).toContain("targetType=AudioLogs");
  });

  it("tourApi.create sends FormData body without forcing JSON content type", async () => {
    const fetchSpy = vi.spyOn(globalThis, "fetch").mockResolvedValue({
      ok: true,
      json: async () => ({ tourId: 99, name: "Tour test" }),
    } as Response);

    await tourApi.create({
      name: "Tour test",
      shortDescription: "Mô tả ngắn",
      description: "Mô tả dài",
      estimatedDurationMinutes: 35,
      imageUrl: "/maui-images/tour.png",
      sortPriority: 2,
      isActive: true,
      isFeatured: false,
    });

    const [, options] = fetchSpy.mock.calls[0];
    expect(options?.method).toBe("POST");
    expect(options?.body).toBeInstanceOf(FormData);

    const form = options?.body as FormData;
    expect(form.get("name")).toBe("Tour test");
    expect(form.get("urlImage")).toBe("/maui-images/tour.png");

    const headers = options?.headers as Record<string, string>;
    expect(headers?.["Content-Type"]).toBeUndefined();
  });

  it("tourApi.update uses PATCH and submits FormData", async () => {
    const fetchSpy = vi.spyOn(globalThis, "fetch").mockResolvedValue({
      ok: true,
      json: async () => ({ message: "ok" }),
    } as Response);

    await tourApi.update(15, {
      estimatedDurationMinutes: 50,
      imageUrl: "/maui-images/new.png",
      sortPriority: 3,
      isActive: true,
      isFeatured: true,
    });

    const [url, options] = fetchSpy.mock.calls[0];
    expect(String(url)).toContain("/Tour/15");
    expect(options?.method).toBe("PATCH");
    expect(options?.body).toBeInstanceOf(FormData);
  });

  it("translationBillingApi.getMonthly applies default paging", async () => {
    const fetchSpy = vi.spyOn(globalThis, "fetch").mockResolvedValue({
      ok: true,
      json: async () => ({
        items: [],
        totalCount: 0,
        page: 1,
        pageSize: 20,
        summary: {
          billingMonth: "2026-04",
          sellerCount: 0,
          totalRequests: 0,
          successRequests: 0,
          failedRequests: 0,
          totalBillableUnits: 0,
          totalAmount: 0,
          currency: "VND",
        },
      }),
    } as Response);

    await translationBillingApi.getMonthly({});

    const [url] = fetchSpy.mock.calls[0];
    expect(String(url)).toContain(
      "/api/admin/translation-billing/monthly?page=1&pageSize=20",
    );
  });

  it("mapsApi.resolveCoordinates URL-encodes source link", async () => {
    const fetchSpy = vi.spyOn(globalThis, "fetch").mockResolvedValue({
      ok: true,
      json: async () => ({ latitude: 15.88, longitude: 108.36 }),
    } as Response);

    await mapsApi.resolveCoordinates(
      "https://www.google.com/maps/@15.88,108.36,17z",
    );

    const [url] = fetchSpy.mock.calls[0];
    expect(String(url)).toContain("/api/maps/resolve-coordinates?url=");
    expect(String(url)).toContain(
      "https%3A%2F%2Fwww.google.com%2Fmaps%2F%4015.88%2C108.36%2C17z",
    );
  });

  it("adminStatsApi.getDishCount keeps cookie credentials", async () => {
    const fetchSpy = vi.spyOn(globalThis, "fetch").mockResolvedValue({
      ok: true,
      json: async () => ({ count: 1 }),
    } as Response);

    await adminStatsApi.getDishCount();

    const [url, options] = fetchSpy.mock.calls[0];
    expect(String(url)).toContain("/api/admin/stats/dishes/count");
    expect(options).toMatchObject({ credentials: "include" });
  });
});
