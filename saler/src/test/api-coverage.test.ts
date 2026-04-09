import { afterEach, describe, expect, it, vi } from "vitest";
import {
  createAudioFromTextApi,
  getMyTranslationUsageApi,
  getRestaurantDishesApi,
  getRestaurantKpisApi,
  resolveMapCoordinatesApi,
  translateAudioTextApi,
  updateRestaurantStatusApi,
} from "@/services/api";

describe("saler api coverage", () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("getRestaurantDishesApi maps imageFileName to absolute image_url", async () => {
    vi.spyOn(globalThis, "fetch").mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => [
        {
          dishId: 12,
          name: "Mì quảng",
          price: 55000,
          restaurantId: "rest-001",
          imageId: 8,
          imageFileName: "mi_quang.jpg",
          createdAt: "2026-04-10T10:00:00Z",
        },
      ],
    } as Response);

    const dishes = await getRestaurantDishesApi("rest-001");

    expect(dishes).toHaveLength(1);
    expect(dishes[0].dish_id).toBe(12);
    expect(dishes[0].image_url).toBe(
      "http://localhost:5044/maui-images/mi_quang.jpg",
    );
  });

  it("updateRestaurantStatusApi sends PATCH with isActive payload", async () => {
    const fetchSpy = vi.spyOn(globalThis, "fetch").mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => ({ message: "ok" }),
    } as Response);

    await updateRestaurantStatusApi("rest-001", false);

    const [url, options] = fetchSpy.mock.calls[0];
    expect(String(url)).toContain("/Restaurant/rest-001/status");
    expect(options?.method).toBe("PATCH");
    expect(options?.credentials).toBe("include");
    expect(String(options?.body)).toContain('"isActive":false');
  });

  it("translateAudioTextApi maps snake_case payload to backend camelCase", async () => {
    const fetchSpy = vi.spyOn(globalThis, "fetch").mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => ({
        requestId: "req-1",
        sourceLanguageCode: "vi",
        targetLanguageCode: "en",
        translatedText: "Hello",
        inputChars: 20,
        outputChars: 5,
        estimatedCost: 100,
        currency: "VND",
      }),
    } as Response);

    const result = await translateAudioTextApi("rest-001", {
      text: "xin chào",
      source_language_code: "vi",
      target_language_code: "en",
      request_id: "req-1",
    });

    const [, options] = fetchSpy.mock.calls[0];
    expect(String(options?.body)).toContain("sourceLanguageCode");
    expect(String(options?.body)).toContain("targetLanguageCode");
    expect(result.translated_text).toBe("Hello");
    expect(result.request_id).toBe("req-1");
  });

  it("createAudioFromTextApi normalizes relative absolute-path audio URL", async () => {
    vi.spyOn(globalThis, "fetch").mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => ({
        requestId: "req-2",
        audioId: 99,
        audioUrl: "/uploads/audios/sample.mp3",
        languageCode: "en",
        voice: "en-US",
        createdAt: "2026-04-10T10:00:00Z",
      }),
    } as Response);

    const result = await createAudioFromTextApi("rest-001", {
      text: "hello",
      language_code: "en",
      source_text: "xin chào",
      voice: "en-US",
      request_id: "req-2",
    });

    expect(result.audio_url).toBe(
      "http://localhost:5044/uploads/audios/sample.mp3",
    );
    expect(result.audio_id).toBe(99);
  });

  it("getMyTranslationUsageApi applies default paging and maps response", async () => {
    const fetchSpy = vi.spyOn(globalThis, "fetch").mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => ({
        items: [
          {
            usageEventId: "u-1",
            requestId: "r-1",
            sellerUserId: 2,
            sellerUsername: "seller1",
            restaurantId: "rest-001",
            audioId: 10,
            provider: "azure",
            actionType: "tts",
            unitType: "chars",
            inputChars: 120,
            outputChars: 100,
            billableUnits: 220,
            costAmount: 1000,
            taxAmount: 100,
            totalAmount: 1100,
            currency: "VND",
            status: "billable",
            billingMonth: "2026-04",
            createdAtUtc: "2026-04-10T10:00:00Z",
          },
        ],
        totalCount: 1,
        page: 1,
        pageSize: 20,
        summary: {
          billingMonth: "2026-04",
          status: "billable",
          eventCount: 1,
          totalBillableUnits: 220,
          totalAmount: 1100,
          currency: "VND",
        },
      }),
    } as Response);

    const result = await getMyTranslationUsageApi({});

    const [url] = fetchSpy.mock.calls[0];
    expect(String(url)).toContain(
      "/api/translation-billing/my-usage?page=1&pageSize=20",
    );
    expect(result.items).toHaveLength(1);
    expect(result.items[0].usage_event_id).toBe("u-1");
    expect(result.summary.total_amount).toBe(1100);
  });

  it("getRestaurantKpisApi encodes restaurant id and maps fields", async () => {
    const fetchSpy = vi.spyOn(globalThis, "fetch").mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => ({
        totalUsers: 5,
        averageListeningTimeSeconds: 21.5,
        averageListeningTimeFormatted: "21s",
        totalPoiPlays: 40,
      }),
    } as Response);

    const result = await getRestaurantKpisApi("rest 001");

    const [url] = fetchSpy.mock.calls[0];
    expect(String(url)).toContain("/api/analytics/restaurants/rest%20001/kpis");
    expect(result.total_users).toBe(5);
    expect(result.total_poi_plays).toBe(40);
  });

  it("resolveMapCoordinatesApi encodes query URL", async () => {
    const fetchSpy = vi.spyOn(globalThis, "fetch").mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => ({ latitude: 15.88, longitude: 108.36 }),
    } as Response);

    const result = await resolveMapCoordinatesApi(
      "https://www.google.com/maps/@15.88,108.36,17z",
    );

    const [url] = fetchSpy.mock.calls[0];
    expect(String(url)).toContain("/api/maps/resolve-coordinates?url=");
    expect(String(url)).toContain(
      "https%3A%2F%2Fwww.google.com%2Fmaps%2F%4015.88%2C108.36%2C17z",
    );
    expect(result.latitude).toBe(15.88);
  });
});
