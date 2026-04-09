import { useState, useEffect, useRef } from "react";
import {
  deleteImageApi,
  getRestaurantDishesApi,
  getRestaurantImagesApi,
  uploadRestaurantImageApi,
  updateDishApi,
} from "@/services/api";
import { useRestaurant } from "@/contexts/RestaurantContext";
import type { RestaurantImage } from "@/types";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
  DialogDescription,
} from "@/components/ui/dialog";
import { toast } from "sonner";
import { ImageIcon, Upload } from "lucide-react";

export default function ImagesPage() {
  const { selectedRestaurant } = useRestaurant();
  const [images, setImages] = useState<RestaurantImage[]>([]);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [preview, setPreview] = useState<string | null>(null);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    if (selectedRestaurant) {
      (async () => {
        try {
          const data = await getRestaurantImagesApi(selectedRestaurant.restaurant_id);
          setImages((data ?? []).filter((img) => img.is_primary));
        } catch {
          toast.error("Không thể tải hình ảnh");
        }
      })();
    }
  }, [selectedRestaurant]);

  const avatarImage = [...images].sort((a, b) => a.sort_order - b.sort_order)[0] ?? null;

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    setSelectedFile(file);
    const reader = new FileReader();
    reader.onload = () => setPreview(reader.result as string);
    reader.readAsDataURL(file);
  };

  const handleUpload = async () => {
    if (!selectedFile || !selectedRestaurant) {
      toast.error("Vui lòng chọn ảnh");
      return;
    }

    try {
      const oldPrimaryIds = images.map((img) => img.image_id);

      const created = await uploadRestaurantImageApi(
        selectedRestaurant.restaurant_id,
        selectedFile,
        true,
        1,
      );

      if (oldPrimaryIds.length > 0) {
        const dishes = await getRestaurantDishesApi(selectedRestaurant.restaurant_id);
        const affectedDishes = dishes.filter(
          (dish) => dish.image_id !== null && oldPrimaryIds.includes(dish.image_id),
        );

        await Promise.all(
          affectedDishes.map((dish) =>
            updateDishApi(dish.dish_id, {
              name: dish.name,
              price: dish.price,
              image_id: null,
            }),
          ),
        );

        await Promise.all(oldPrimaryIds.map((imageId) => deleteImageApi(imageId)));
      }

      setImages([created]);
      toast.success("Cập nhật ảnh nhà hàng thành công");
      resetDialog();
    } catch {
      toast.error("Không thể tải ảnh lên");
    }
  };

  const resetDialog = () => {
    setDialogOpen(false);
    setPreview(null);
    setSelectedFile(null);
    if (fileInputRef.current) fileInputRef.current.value = "";
  };

  return (
    <div className="max-w-3xl mx-auto animate-fade-in">
      <div className="page-header flex items-start justify-between">
        <div>
          <h1 className="page-title">Hình ảnh nhà hàng</h1>
          <p className="page-description">Ảnh đại diện của nhà hàng</p>
        </div>
        <Button onClick={() => setDialogOpen(true)}>
          <Upload className="w-4 h-4 mr-2" /> Thay ảnh
        </Button>
      </div>

      <div className="form-section">
        {avatarImage ? (
          <div className="rounded-lg overflow-hidden border">
            <img src={avatarImage.image_url} alt="Ảnh nhà hàng" className="w-full aspect-video object-cover" />
          </div>
        ) : (
          <div className="flex flex-col items-center justify-center py-16 text-muted-foreground gap-3">
            <ImageIcon className="w-12 h-12 opacity-40" />
            <p>Chưa có ảnh. Nhấn "Thay ảnh" để tải lên.</p>
          </div>
        )}
      </div>

      <Dialog
        open={dialogOpen}
        onOpenChange={(open) => {
          if (!open) resetDialog();
          else setDialogOpen(true);
        }}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Thay ảnh nhà hàng</DialogTitle>
            <DialogDescription>
              Chọn ảnh mới để thay thế. Ảnh cũ sẽ bị xóa hoàn toàn.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4 py-2">
            <div
              className="border-2 border-dashed rounded-lg p-6 text-center cursor-pointer hover:border-primary transition-colors"
              onClick={() => fileInputRef.current?.click()}
            >
              {preview ? (
                <img src={preview} alt="Xem trước" className="max-h-48 mx-auto rounded-md object-contain" />
              ) : (
                <div className="flex flex-col items-center gap-2 text-muted-foreground">
                  <Upload className="w-8 h-8" />
                  <p className="text-sm">Nhấn để chọn ảnh hoặc kéo thả vào đây</p>
                  <p className="text-xs">JPG, PNG, WEBP (tối đa 5MB)</p>
                </div>
              )}
              <input
                ref={fileInputRef}
                type="file"
                accept="image/jpeg,image/png,image/webp"
                className="hidden"
                onChange={handleFileChange}
              />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={resetDialog}>Hủy</Button>
            <Button onClick={handleUpload} disabled={!selectedFile}>Tải lên</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
