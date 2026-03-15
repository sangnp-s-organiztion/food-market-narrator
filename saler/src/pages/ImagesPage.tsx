import { useRef, useState, useEffect } from "react";
import {
  deleteImage,
  getRestaurantImages,
  reorderImages,
  setImagePrimary,
  uploadRestaurantImage,
} from "@/services/api";
import { useRestaurant } from "@/contexts/RestaurantContext";
import type { RestaurantImage } from "@/types";
import { Button } from "@/components/ui/button";
import { toast } from "sonner";
import { Plus, Star, StarOff, ArrowUp, ArrowDown, Trash2 } from "lucide-react";

export default function ImagesPage() {
  const { selectedRestaurant } = useRestaurant();
  const [images, setImages] = useState<RestaurantImage[]>([]);
  const [loading, setLoading] = useState(false);
  const [uploading, setUploading] = useState(false);
  const fileInputRef = useRef<HTMLInputElement | null>(null);

  useEffect(() => {
    const loadImages = async () => {
      if (!selectedRestaurant) {
        setImages([]);
        return;
      }

      setLoading(true);
      try {
        const data = await getRestaurantImages(
          selectedRestaurant.restaurant_id,
        );
        setImages(data);
      } catch (error) {
        console.error(error);
        toast.error("Không thể tải danh sách hình ảnh");
      } finally {
        setLoading(false);
      }
    };

    void loadImages();
  }, [selectedRestaurant]);

  const setPrimary = async (id: number) => {
    if (!selectedRestaurant) return;

    try {
      await setImagePrimary(id, true);
      const refreshed = await getRestaurantImages(
        selectedRestaurant.restaurant_id,
      );
      setImages(refreshed);
      toast.success("Đã cập nhật ảnh chính");
    } catch (error) {
      console.error(error);
      toast.error("Không thể cập nhật ảnh chính");
    }
  };

  const moveImage = async (id: number, direction: "up" | "down") => {
    if (!selectedRestaurant) return;

    const sorted = [...images].sort((a, b) => a.sort_order - b.sort_order);
    const idx = sorted.findIndex((img) => img.image_id === id);
    const swapIdx = direction === "up" ? idx - 1 : idx + 1;
    if (swapIdx < 0 || swapIdx >= sorted.length) return;

    const reordered = [...sorted];
    const current = reordered[idx];
    reordered[idx] = reordered[swapIdx];
    reordered[swapIdx] = current;

    const payload = reordered.map((img, orderIndex) => ({
      image_id: img.image_id,
      sort_order: orderIndex + 1,
    }));

    try {
      await reorderImages(selectedRestaurant.restaurant_id, payload);
      setImages((prev) =>
        prev.map((img) => {
          const next = payload.find((item) => item.image_id === img.image_id);
          return next ? { ...img, sort_order: next.sort_order } : img;
        }),
      );
    } catch (error) {
      console.error(error);
      toast.error("Không thể đổi thứ tự hình ảnh");
    }
  };

  const removeImage = async (id: number) => {
    try {
      await deleteImage(id);
      setImages((prev) => prev.filter((img) => img.image_id !== id));
      toast.success("Đã xóa hình ảnh");
    } catch (error) {
      console.error(error);
      toast.error("Không thể xóa hình ảnh");
    }
  };

  const addImage = () => {
    fileInputRef.current?.click();
  };

  const handleFileSelected = async (
    event: React.ChangeEvent<HTMLInputElement>,
  ) => {
    const file = event.target.files?.[0];
    if (!file || !selectedRestaurant) return;

    setUploading(true);
    try {
      const created = await uploadRestaurantImage(
        selectedRestaurant.restaurant_id,
        file,
        {
          isPrimary: images.length === 0,
          sortOrder: images.length + 1,
        },
      );
      setImages((prev) => [...prev, created]);
      toast.success("Đã tải hình ảnh lên");
    } catch (error) {
      console.error(error);
      toast.error("Không thể tải hình ảnh lên");
    } finally {
      event.target.value = "";
      setUploading(false);
    }
  };

  const sorted = [...images].sort((a, b) => a.sort_order - b.sort_order);

  return (
    <div className="max-w-3xl mx-auto animate-fade-in">
      <div className="page-header flex items-start justify-between">
        <div>
          <h1 className="page-title">Hình ảnh nhà hàng</h1>
          <p className="page-description">Quản lý hình ảnh của nhà hàng</p>
        </div>
        <Button onClick={addImage} disabled={uploading}>
          <Plus className="w-4 h-4 mr-2" />{" "}
          {uploading ? "Đang tải..." : "Tải ảnh lên"}
        </Button>
      </div>

      <input
        ref={fileInputRef}
        type="file"
        accept="image/*"
        className="hidden"
        onChange={(event) => {
          void handleFileSelected(event);
        }}
      />

      {loading ? (
        <div className="form-section text-center py-12">
          <p className="text-muted-foreground">
            Đang tải danh sách hình ảnh...
          </p>
        </div>
      ) : sorted.length === 0 ? (
        <div className="form-section text-center py-12">
          <p className="text-muted-foreground">
            Chưa có hình ảnh nào. Tải lên hình ảnh đầu tiên.
          </p>
        </div>
      ) : (
        <div className="grid gap-4 sm:grid-cols-2">
          {sorted.map((img, i) => (
            <div
              key={img.image_id}
              className="dashboard-card p-0 overflow-hidden group relative"
            >
              <div className="aspect-video relative">
                <img
                  src={img.image_url}
                  alt={`Hình ảnh nhà hàng ${i + 1}`}
                  className="w-full h-full object-cover"
                />
                {img.is_primary && (
                  <span className="absolute top-2 left-2 bg-primary text-primary-foreground text-xs font-medium px-2 py-1 rounded-md">
                    Ảnh chính
                  </span>
                )}
              </div>
              <div className="p-3 flex items-center gap-1">
                <Button
                  variant="ghost"
                  size="icon"
                  onClick={() => void setPrimary(img.image_id)}
                  title={img.is_primary ? "Ảnh chính" : "Đặt làm ảnh chính"}
                >
                  {img.is_primary ? (
                    <Star className="w-4 h-4 text-primary fill-primary" />
                  ) : (
                    <StarOff className="w-4 h-4" />
                  )}
                </Button>
                <Button
                  variant="ghost"
                  size="icon"
                  onClick={() => void moveImage(img.image_id, "up")}
                  disabled={i === 0}
                >
                  <ArrowUp className="w-4 h-4" />
                </Button>
                <Button
                  variant="ghost"
                  size="icon"
                  onClick={() => void moveImage(img.image_id, "down")}
                  disabled={i === sorted.length - 1}
                >
                  <ArrowDown className="w-4 h-4" />
                </Button>
                <div className="flex-1" />
                <Button
                  variant="ghost"
                  size="icon"
                  onClick={() => void removeImage(img.image_id)}
                >
                  <Trash2 className="w-4 h-4 text-destructive" />
                </Button>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
