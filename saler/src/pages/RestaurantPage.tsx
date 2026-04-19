import { useState, useEffect, useCallback } from "react";
import { useRestaurant } from "@/contexts/RestaurantContext";
import type {
  Language,
  Restaurant,
  RestaurantFieldTranslations,
} from "@/types";
import {
  getLanguagesApi,
  getRestaurantTranslationsApi,
  resolveMapCoordinatesApi,
  updateRestaurantApi,
  updateRestaurantStatusApi,
} from "@/services/api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Switch } from "@/components/ui/switch";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { toast } from "sonner";
import { MapPin, Phone, Save, Clock } from "lucide-react";

function isWithinSchedule(openTime: string, closeTime: string): boolean {
  const now = new Date();
  const [openH, openM] = openTime.split(":").map(Number);
  const [closeH, closeM] = closeTime.split(":").map(Number);
  const currentMinutes = now.getHours() * 60 + now.getMinutes();
  const openMinutes = openH * 60 + openM;
  const closeMinutes = closeH * 60 + closeM;

  if (closeMinutes > openMinutes) {
    return currentMinutes >= openMinutes && currentMinutes < closeMinutes;
  }
  return currentMinutes >= openMinutes || currentMinutes < closeMinutes;
}

function isValidCoordinatePair(latitude: number, longitude: number): boolean {
  return (
    Number.isFinite(latitude) &&
    Number.isFinite(longitude) &&
    latitude >= -90 &&
    latitude <= 90 &&
    longitude >= -180 &&
    longitude <= 180
  );
}

function extractCoordinatesFromGoogleMapsUrl(
  rawUrl: string,
): { latitude: number; longitude: number } | null {
  const decoded = (() => {
    try {
      return decodeURIComponent(rawUrl);
    } catch {
      return rawUrl;
    }
  })();

  const candidates: RegExp[] = [
    /@(-?\d+(?:\.\d+)?),\s*(-?\d+(?:\.\d+)?)/i,
    /!3d(-?\d+(?:\.\d+)?)!4d(-?\d+(?:\.\d+)?)/i,
    /[?&]q=(-?\d+(?:\.\d+)?),\s*(-?\d+(?:\.\d+)?)/i,
    /[?&]ll=(-?\d+(?:\.\d+)?),\s*(-?\d+(?:\.\d+)?)/i,
  ];

  for (const pattern of candidates) {
    const match = decoded.match(pattern);
    if (!match) continue;

    const latitude = Number(match[1]);
    const longitude = Number(match[2]);

    if (isValidCoordinatePair(latitude, longitude)) {
      return { latitude, longitude };
    }
  }

  return null;
}

function getBaseLanguageCode(code: string): string {
  return code.trim().toLowerCase().replace("_", "-").split("-")[0] ?? "";
}

export default function RestaurantPage() {
  const { selectedRestaurant, refreshRestaurants } = useRestaurant();
  const [restaurant, setRestaurant] = useState<Restaurant | null>(
    selectedRestaurant ? { ...selectedRestaurant } : null,
  );
  const [googleMapsUrl, setGoogleMapsUrl] = useState("");
  const [saving, setSaving] = useState(false);
  const [autoMode, setAutoMode] = useState(true);
  const [hasUnsavedChanges, setHasUnsavedChanges] = useState(false);
  const [hydratedRestaurantId, setHydratedRestaurantId] = useState<
    string | null
  >(selectedRestaurant?.restaurant_id ?? null);
  const [languages, setLanguages] = useState<Language[]>([]);
  const [previewLanguageCode, setPreviewLanguageCode] = useState("vi");
  const [translatedFields, setTranslatedFields] =
    useState<RestaurantFieldTranslations>({});
  const [loadingTranslation, setLoadingTranslation] = useState(false);
  const restaurantId = restaurant?.restaurant_id ?? "";
  const openTime = restaurant?.open_time ?? "";
  const closeTime = restaurant?.close_time ?? "";

  useEffect(() => {
    if (selectedRestaurant) {
      const isRestaurantChanged =
        selectedRestaurant.restaurant_id !== hydratedRestaurantId;

      if (!isRestaurantChanged && hasUnsavedChanges) {
        return;
      }

      setRestaurant({ ...selectedRestaurant });
      setHasUnsavedChanges(false);
      setHydratedRestaurantId(selectedRestaurant.restaurant_id);

      if (isRestaurantChanged) {
        setGoogleMapsUrl("");
        setAutoMode(true);
        setPreviewLanguageCode("vi");
        setTranslatedFields({});
      }
    }
  }, [hasUnsavedChanges, hydratedRestaurantId, selectedRestaurant]);

  useEffect(() => {
    let mounted = true;

    const loadLanguages = async () => {
      try {
        const data = await getLanguagesApi();
        if (!mounted) return;

        const normalized = data
          .filter((lang) => Boolean(lang.code))
          .sort((a, b) => a.name.localeCompare(b.name));

        setLanguages(normalized);

        if (
          !normalized.some(
            (lang) => getBaseLanguageCode(lang.code ?? "") === "vi",
          )
        ) {
          setPreviewLanguageCode(normalized[0]?.code ?? "vi");
        }
      } catch {
        if (!mounted) return;
        setLanguages([]);
      }
    };

    loadLanguages();

    return () => {
      mounted = false;
    };
  }, []);

  useEffect(() => {
    let mounted = true;

    const loadTranslation = async () => {
      if (
        !restaurantId ||
        !previewLanguageCode ||
        previewLanguageCode === "vi"
      ) {
        setTranslatedFields({});
        return;
      }

      setLoadingTranslation(true);
      try {
        const translated = await getRestaurantTranslationsApi(
          restaurantId,
          previewLanguageCode,
        );

        if (!mounted) return;
        setTranslatedFields(translated);
      } catch {
        if (!mounted) return;
        setTranslatedFields({});
      } finally {
        if (mounted) {
          setLoadingTranslation(false);
        }
      }
    };

    loadTranslation();

    return () => {
      mounted = false;
    };
  }, [previewLanguageCode, restaurantId]);

  const updateField = <K extends keyof Restaurant>(
    key: K,
    value: Restaurant[K],
  ) => {
    setRestaurant((prev) => {
      if (!prev || prev[key] === value) {
        return prev;
      }

      setHasUnsavedChanges(true);
      return { ...prev, [key]: value };
    });
  };

  const updateAutoStatus = useCallback(() => {
    if (!autoMode || !openTime || !closeTime) return;
    const shouldBeActive = isWithinSchedule(openTime, closeTime);
    setRestaurant((prev) =>
      prev && prev.is_active !== shouldBeActive
        ? { ...prev, is_active: shouldBeActive }
        : prev,
    );
  }, [autoMode, openTime, closeTime]);

  useEffect(() => {
    updateAutoStatus();
    const interval = setInterval(updateAutoStatus, 60000);
    return () => clearInterval(interval);
  }, [updateAutoStatus]);

  const handleManualToggle = (value: boolean) => {
    setAutoMode(false);
    updateField("is_active", value);
  };

  const handleAutoModeToggle = (value: boolean) => {
    setAutoMode(value);
    if (!restaurant) return;
    if (value) {
      const shouldBeActive = isWithinSchedule(
        restaurant.open_time,
        restaurant.close_time,
      );
      updateField("is_active", shouldBeActive);
    }
  };

  const handleSave = async () => {
    if (!restaurant) return;

    setSaving(true);
    try {
      const updatedRestaurant = await updateRestaurantApi(
        restaurant.restaurant_id,
        restaurant,
      );
      await updateRestaurantStatusApi(
        restaurant.restaurant_id,
        restaurant.is_active,
      );
      setRestaurant({ ...updatedRestaurant, is_active: restaurant.is_active });
      setHasUnsavedChanges(false);
      setHydratedRestaurantId(updatedRestaurant.restaurant_id);
      if (previewLanguageCode !== "vi") {
        try {
          const translated = await getRestaurantTranslationsApi(
            restaurant.restaurant_id,
            previewLanguageCode,
          );
          setTranslatedFields(translated);
        } catch {
          setTranslatedFields({});
        }
      }
      await refreshRestaurants();
      toast.success("Lưu thay đổi thành công!");
    } catch {
      toast.error("Không thể lưu thay đổi");
    }
    setSaving(false);
  };

  const applyCoordinates = (latitude: number, longitude: number) => {
    setRestaurant((prev) => {
      if (!prev) {
        return prev;
      }

      if (prev.latitude === latitude && prev.longitude === longitude) {
        return prev;
      }

      setHasUnsavedChanges(true);
      return { ...prev, latitude, longitude };
    });
  };

  const handleGoogleMapsUrlChange = (value: string) => {
    setGoogleMapsUrl(value);

    const trimmedValue = value.trim();
    if (!trimmedValue) return;

    const coords = extractCoordinatesFromGoogleMapsUrl(trimmedValue);
    if (coords) {
      applyCoordinates(coords.latitude, coords.longitude);
    }
  };

  const handleGoogleMapsUrlBlur = async () => {
    const raw = googleMapsUrl.trim();
    if (!raw) return;

    const directCoords = extractCoordinatesFromGoogleMapsUrl(raw);
    if (directCoords) {
      applyCoordinates(directCoords.latitude, directCoords.longitude);
      return;
    }

    try {
      const resolved = await resolveMapCoordinatesApi(raw);
      applyCoordinates(resolved.latitude, resolved.longitude);
    } catch {
      toast.error("Không đọc được tọa độ từ link Google Maps");
    }
  };

  if (!selectedRestaurant || !restaurant) return null;

  const displayName = restaurant.name;
  const displayDescription =
    previewLanguageCode === "vi"
      ? restaurant.description
      : translatedFields.description || restaurant.description;
  const displayAddress =
    previewLanguageCode === "vi"
      ? restaurant.address
      : translatedFields.address || restaurant.address;

  const hasTranslationForPreviewLanguage =
    previewLanguageCode === "vi"
      ? true
      : Boolean(translatedFields.description || translatedFields.address);

  return (
    <div className="max-w-3xl mx-auto animate-fade-in">
      <div className="page-header">
        <h1 className="page-title">Thông tin nhà hàng</h1>
        <p className="page-description">
          Quản lý hồ sơ và thông tin liên hệ của nhà hàng
        </p>
      </div>

      <div className="space-y-6">
        <div className="form-section">
          <div className="flex items-center justify-between">
            <div>
              <h3 className="font-medium text-foreground">
                Trạng thái nhà hàng
              </h3>
              <p className="text-sm text-muted-foreground">
                {restaurant.is_active
                  ? "Nhà hàng của bạn hiện đang mở cửa"
                  : "Nhà hàng của bạn hiện đang đóng cửa"}
              </p>
            </div>
            <div className="flex items-center gap-3">
              <span
                className={`text-sm font-medium ${restaurant.is_active ? "text-success" : "text-muted-foreground"}`}
              >
                {restaurant.is_active ? "Mở cửa" : "Đóng cửa"}
              </span>
              <Switch
                checked={restaurant.is_active}
                onCheckedChange={handleManualToggle}
              />
            </div>
          </div>
        </div>

        <div className="form-section space-y-4">
          <h3 className="font-medium text-foreground flex items-center gap-2">
            <Clock className="w-4 h-4 text-primary" /> Giờ hoạt động của nhà
            hàng
          </h3>
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label htmlFor="open_time">Giờ mở cửa</Label>
              <Input
                id="open_time"
                type="time"
                value={restaurant.open_time}
                onChange={(e) => updateField("open_time", e.target.value)}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="close_time">Giờ đóng cửa</Label>
              <Input
                id="close_time"
                type="time"
                value={restaurant.close_time}
                onChange={(e) => updateField("close_time", e.target.value)}
              />
            </div>
          </div>
          <div className="flex items-center justify-between rounded-lg border p-3">
            <div>
              <p className="text-sm font-medium text-foreground">
                Bật chế độ tự động
              </p>
              <p className="text-xs text-muted-foreground">
                Tự động cập nhật trạng thái theo lịch trình
              </p>
            </div>
            <Switch checked={autoMode} onCheckedChange={handleAutoModeToggle} />
          </div>
          {!autoMode && (
            <p className="text-xs text-muted-foreground italic">
              Chế độ thủ công đang bật, lịch trình tạm thời bị bỏ qua.
            </p>
          )}
        </div>

        <div className="form-section space-y-4">
          <h3 className="font-medium text-foreground">Thông tin cơ bản</h3>
          <div className="space-y-2">
            <Label htmlFor="name">Tên nhà hàng</Label>
            <Input
              id="name"
              value={restaurant.name}
              onChange={(e) => updateField("name", e.target.value)}
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="description">Mô tả</Label>
            <Textarea
              id="description"
              value={restaurant.description}
              onChange={(e) => updateField("description", e.target.value)}
              rows={4}
            />
          </div>
        </div>

        <div className="form-section space-y-4">
          <h3 className="font-medium text-foreground flex items-center gap-2">
            <Phone className="w-4 h-4 text-primary" /> Liên hệ
          </h3>
          <div className="space-y-2">
            <Label htmlFor="phone">Số điện thoại</Label>
            <Input
              id="phone"
              value={restaurant.phone}
              onChange={(e) => updateField("phone", e.target.value)}
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="address">Địa chỉ</Label>
            <Input
              id="address"
              value={restaurant.address}
              onChange={(e) => updateField("address", e.target.value)}
            />
          </div>
        </div>

        <div className="form-section space-y-4">
          <h3 className="font-medium text-foreground flex items-center gap-2">
            <MapPin className="w-4 h-4 text-primary" /> Vị trí
          </h3>
          <div className="space-y-2">
            <Label htmlFor="google_maps_url">Đường dẫn Google Maps</Label>
            <Input
              id="google_maps_url"
              value={googleMapsUrl}
              onChange={(e) => handleGoogleMapsUrlChange(e.target.value)}
              onBlur={handleGoogleMapsUrlBlur}
              placeholder="Dán đường dẫn Google Maps để tự động điền tọa độ"
            />
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label htmlFor="lat">Vĩ độ</Label>
              <Input
                id="lat"
                type="number"
                step="any"
                value={restaurant.latitude}
                onChange={(e) =>
                  updateField("latitude", parseFloat(e.target.value) || 0)
                }
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="lng">Kinh độ</Label>
              <Input
                id="lng"
                type="number"
                step="any"
                value={restaurant.longitude}
                onChange={(e) =>
                  updateField("longitude", parseFloat(e.target.value) || 0)
                }
              />
            </div>
          </div>
          <div className="rounded-lg overflow-hidden border aspect-video">
            <iframe
              title="Vị trí nhà hàng"
              width="100%"
              height="100%"
              style={{ border: 0 }}
              loading="lazy"
              referrerPolicy="no-referrer-when-downgrade"
              src={`https://www.google.com/maps?q=${restaurant.latitude},${restaurant.longitude}&z=15&output=embed`}
            />
          </div>
        </div>

        <div className="form-section space-y-4">
          <div className="flex items-center justify-between gap-4 flex-wrap">
            <h3 className="font-medium text-foreground">
              Xem thông tin nhà hàng theo ngôn ngữ
            </h3>
            <div className="w-full sm:w-56">
              <Select
                value={previewLanguageCode}
                onValueChange={setPreviewLanguageCode}
              >
                <SelectTrigger>
                  <SelectValue placeholder="Chọn ngôn ngữ" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="vi">Tiếng Việt</SelectItem>
                  {languages
                    .filter(
                      (language) =>
                        language.code &&
                        getBaseLanguageCode(language.code) !== "vi",
                    )
                    .map((language) => (
                      <SelectItem
                        key={language.language_id}
                        value={language.code}
                      >
                        {language.name}
                      </SelectItem>
                    ))}
                </SelectContent>
              </Select>
            </div>
          </div>

          <div className="rounded-lg border p-4 space-y-3 bg-muted/20">
            <div>
              <p className="text-xs text-muted-foreground">Tên nhà hàng</p>
              <p className="font-medium text-foreground">{displayName}</p>
            </div>
            <div>
              <p className="text-xs text-muted-foreground">Mô tả</p>
              <p className="text-sm text-foreground whitespace-pre-wrap">
                {displayDescription || "(Chưa có mô tả)"}
              </p>
            </div>
            <div>
              <p className="text-xs text-muted-foreground">Địa chỉ</p>
              <p className="text-sm text-foreground">
                {displayAddress || "(Chưa có địa chỉ)"}
              </p>
            </div>

            {previewLanguageCode !== "vi" &&
              !loadingTranslation &&
              !hasTranslationForPreviewLanguage && (
                <p className="text-xs text-amber-600">
                  Chưa có bản dịch cho ngôn ngữ này, hệ thống đang dùng dữ liệu
                  tiếng Việt.
                </p>
              )}
            {previewLanguageCode !== "vi" && loadingTranslation && (
              <p className="text-xs text-muted-foreground">
                Đang tải bản dịch...
              </p>
            )}
          </div>
        </div>

        <div className="flex justify-end">
          <Button onClick={handleSave} disabled={saving}>
            <Save className="w-4 h-4 mr-2" />
            {saving ? "Đang lưu..." : "Lưu thay đổi"}
          </Button>
        </div>
      </div>
    </div>
  );
}
