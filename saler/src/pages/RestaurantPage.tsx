import { useState, useEffect, useCallback } from "react";
import { useRestaurant } from "@/contexts/RestaurantContext";
import type { Restaurant } from "@/types";
import { updateRestaurantApi, updateRestaurantStatusApi } from "@/services/api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Switch } from "@/components/ui/switch";
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

export default function RestaurantPage() {
  const { selectedRestaurant, refreshRestaurants } = useRestaurant();
  const [restaurant, setRestaurant] = useState<Restaurant | null>(selectedRestaurant ? { ...selectedRestaurant } : null);
  const [saving, setSaving] = useState(false);
  const [autoMode, setAutoMode] = useState(true);

  // Sync when switching restaurants
  useEffect(() => {
    if (selectedRestaurant) {
      setRestaurant({ ...selectedRestaurant });
      setAutoMode(true);
    }
  }, [selectedRestaurant]);

  const updateField = <K extends keyof Restaurant>(key: K, value: Restaurant[K]) => {
    setRestaurant((prev) => (prev ? { ...prev, [key]: value } : prev));
  };

  const updateAutoStatus = useCallback(() => {
    if (!autoMode || !restaurant) return;
    const shouldBeActive = isWithinSchedule(restaurant.open_time, restaurant.close_time);
    setRestaurant((prev) => (prev && prev.is_active !== shouldBeActive ? { ...prev, is_active: shouldBeActive } : prev));
  }, [autoMode, restaurant?.open_time, restaurant?.close_time]);

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
      const shouldBeActive = isWithinSchedule(restaurant.open_time, restaurant.close_time);
      updateField("is_active", shouldBeActive);
    }
  };

  const handleSave = async () => {
    if (!restaurant) return;

    setSaving(true);
    try {
      const updatedRestaurant = await updateRestaurantApi(restaurant.restaurant_id, restaurant);
      await updateRestaurantStatusApi(restaurant.restaurant_id, restaurant.is_active);
      setRestaurant(updatedRestaurant);
      await refreshRestaurants();
      toast.success("Lưu thay đổi thành công!");
    } catch {
      toast.error("Không thể lưu thay đổi");
    }
    setSaving(false);
  };

  if (!selectedRestaurant || !restaurant) return null;

  return (
    <div className="max-w-3xl mx-auto animate-fade-in">
      <div className="page-header">
        <h1 className="page-title">Thông tin nhà hàng</h1>
        <p className="page-description">Quản lý hồ sơ và thông tin liên hệ của nhà hàng</p>
      </div>

      <div className="space-y-6">
        {/* Status Toggle */}
        <div className="form-section">
          <div className="flex items-center justify-between">
            <div>
              <h3 className="font-medium text-foreground">Trạng thái nhà hàng</h3>
              <p className="text-sm text-muted-foreground">
                {restaurant.is_active
                  ? "Nhà hàng của bạn hiện đang mở cửa"
                  : "Nhà hàng của bạn hiện đang đóng cửa"}
              </p>
            </div>
            <div className="flex items-center gap-3">
              <span className={`text-sm font-medium ${restaurant.is_active ? "text-success" : "text-muted-foreground"}`}>
                {restaurant.is_active ? "Mở cửa" : "Đóng cửa"}
              </span>
              <Switch checked={restaurant.is_active} onCheckedChange={handleManualToggle} />
            </div>
          </div>
        </div>

        {/* Schedule */}
        <div className="form-section space-y-4">
          <h3 className="font-medium text-foreground flex items-center gap-2">
            <Clock className="w-4 h-4 text-primary" /> Giờ hoạt động của nhà hàng
          </h3>
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label htmlFor="open_time">Giờ mở cửa</Label>
              <Input id="open_time" type="time" value={restaurant.open_time} onChange={(e) => updateField("open_time", e.target.value)} />
            </div>
            <div className="space-y-2">
              <Label htmlFor="close_time">Giờ đóng cửa</Label>
              <Input id="close_time" type="time" value={restaurant.close_time} onChange={(e) => updateField("close_time", e.target.value)} />
            </div>
          </div>
          <div className="flex items-center justify-between rounded-lg border p-3">
            <div>
              <p className="text-sm font-medium text-foreground">Bật chế độ tự động</p>
              <p className="text-xs text-muted-foreground">Tự động cập nhật trạng thái theo lịch trình</p>
            </div>
            <Switch checked={autoMode} onCheckedChange={handleAutoModeToggle} />
          </div>
          {!autoMode && (
            <p className="text-xs text-muted-foreground italic">Chế độ thủ công đang bật — lịch trình tạm thời bị bỏ qua.</p>
          )}
        </div>

        {/* Basic Info */}
        <div className="form-section space-y-4">
          <h3 className="font-medium text-foreground">Thông tin cơ bản</h3>
          <div className="space-y-2">
            <Label htmlFor="name">Tên nhà hàng</Label>
            <Input id="name" value={restaurant.name} onChange={(e) => updateField("name", e.target.value)} />
          </div>
          <div className="space-y-2">
            <Label htmlFor="description">Mô tả</Label>
            <Textarea id="description" value={restaurant.description} onChange={(e) => updateField("description", e.target.value)} rows={4} />
          </div>
        </div>

        {/* Contact */}
        <div className="form-section space-y-4">
          <h3 className="font-medium text-foreground flex items-center gap-2">
            <Phone className="w-4 h-4 text-primary" /> Liên hệ
          </h3>
          <div className="space-y-2">
            <Label htmlFor="phone">Số điện thoại</Label>
            <Input id="phone" value={restaurant.phone} onChange={(e) => updateField("phone", e.target.value)} />
          </div>
          <div className="space-y-2">
            <Label htmlFor="address">Địa chỉ</Label>
            <Input id="address" value={restaurant.address} onChange={(e) => updateField("address", e.target.value)} />
          </div>
        </div>

        {/* Location */}
        <div className="form-section space-y-4">
          <h3 className="font-medium text-foreground flex items-center gap-2">
            <MapPin className="w-4 h-4 text-primary" /> Vị trí
          </h3>
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label htmlFor="lat">Vĩ độ</Label>
              <Input id="lat" type="number" step="any" value={restaurant.latitude} onChange={(e) => updateField("latitude", parseFloat(e.target.value) || 0)} />
            </div>
            <div className="space-y-2">
              <Label htmlFor="lng">Kinh độ</Label>
              <Input id="lng" type="number" step="any" value={restaurant.longitude} onChange={(e) => updateField("longitude", parseFloat(e.target.value) || 0)} />
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

        {/* Save */}
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
