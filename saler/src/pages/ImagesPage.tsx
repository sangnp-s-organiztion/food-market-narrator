import { useState, useEffect } from "react";
import { getRestaurantImages } from "@/services/mockData";
import { useRestaurant } from "@/contexts/RestaurantContext";
import type { RestaurantImage } from "@/types";
import { Button } from "@/components/ui/button";
import { toast } from "sonner";
import { Plus, Star, StarOff, ArrowUp, ArrowDown, Trash2 } from "lucide-react";

export default function ImagesPage() {
  const { selectedRestaurant } = useRestaurant();
  const [images, setImages] = useState<RestaurantImage[]>([]);

  useEffect(() => {
    if (selectedRestaurant) {
      setImages(getRestaurantImages(selectedRestaurant.restaurant_id));
    }
  }, [selectedRestaurant]);

  const setPrimary = (id: number) => {
    setImages((prev) =>
      prev.map((img) => ({ ...img, is_primary: img.image_id === id }))
    );
    toast.success("Đã cập nhật ảnh chính");
  };

  const moveImage = (id: number, direction: "up" | "down") => {
    setImages((prev) => {
      const sorted = [...prev].sort((a, b) => a.sort_order - b.sort_order);
      const idx = sorted.findIndex((img) => img.image_id === id);
      const swapIdx = direction === "up" ? idx - 1 : idx + 1;
      if (swapIdx < 0 || swapIdx >= sorted.length) return prev;
      const temp = sorted[idx].sort_order;
      sorted[idx] = { ...sorted[idx], sort_order: sorted[swapIdx].sort_order };
      sorted[swapIdx] = { ...sorted[swapIdx], sort_order: temp };
      return sorted;
    });
  };

  const deleteImage = (id: number) => {
    setImages((prev) => prev.filter((img) => img.image_id !== id));
    toast.success("Đã xóa hình ảnh");
  };

  const addImage = () => {
    const newImage: RestaurantImage = {
      image_id: Date.now(),
      restaurant_id: selectedRestaurant!.restaurant_id,
      image_url: `https://images.unsplash.com/photo-1550966871-3ed3cdb51f3a?w=600&t=${Date.now()}`,
      is_primary: images.length === 0,
      sort_order: images.length + 1,
    };
    setImages((prev) => [...prev, newImage]);
    toast.success("Đã thêm hình ảnh (demo)");
  };

  const sorted = [...images].sort((a, b) => a.sort_order - b.sort_order);

  return (
    <div className="max-w-3xl mx-auto animate-fade-in">
      <div className="page-header flex items-start justify-between">
        <div>
          <h1 className="page-title">Hình ảnh nhà hàng</h1>
          <p className="page-description">Quản lý hình ảnh của nhà hàng</p>
        </div>
        <Button onClick={addImage}>
          <Plus className="w-4 h-4 mr-2" /> Tải ảnh lên
        </Button>
      </div>

      {sorted.length === 0 ? (
        <div className="form-section text-center py-12">
          <p className="text-muted-foreground">Chưa có hình ảnh nào. Tải lên hình ảnh đầu tiên.</p>
        </div>
      ) : (
        <div className="grid gap-4 sm:grid-cols-2">
          {sorted.map((img, i) => (
            <div key={img.image_id} className="dashboard-card p-0 overflow-hidden group relative">
              <div className="aspect-video relative">
                <img src={img.image_url} alt={`Hình ảnh nhà hàng ${i + 1}`} className="w-full h-full object-cover" />
                {img.is_primary && (
                  <span className="absolute top-2 left-2 bg-primary text-primary-foreground text-xs font-medium px-2 py-1 rounded-md">
                    Ảnh chính
                  </span>
                )}
              </div>
              <div className="p-3 flex items-center gap-1">
                <Button variant="ghost" size="icon" onClick={() => setPrimary(img.image_id)} title={img.is_primary ? "Ảnh chính" : "Đặt làm ảnh chính"}>
                  {img.is_primary ? <Star className="w-4 h-4 text-primary fill-primary" /> : <StarOff className="w-4 h-4" />}
                </Button>
                <Button variant="ghost" size="icon" onClick={() => moveImage(img.image_id, "up")} disabled={i === 0}>
                  <ArrowUp className="w-4 h-4" />
                </Button>
                <Button variant="ghost" size="icon" onClick={() => moveImage(img.image_id, "down")} disabled={i === sorted.length - 1}>
                  <ArrowDown className="w-4 h-4" />
                </Button>
                <div className="flex-1" />
                <Button variant="ghost" size="icon" onClick={() => deleteImage(img.image_id)}>
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
