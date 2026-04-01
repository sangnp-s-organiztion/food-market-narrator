import { useState, useEffect, useRef } from "react";
import {
  createDishApi,
  deleteImageApi,
  deleteDishApi,
  getRestaurantDishesApi,
  getRestaurantImagesApi,
  uploadRestaurantImageApi,
  updateDishApi,
} from "@/services/api";
import { useRestaurant } from "@/contexts/RestaurantContext";
import type { Dish, RestaurantImage } from "@/types";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
  DialogDescription,
} from "@/components/ui/dialog";
import { toast } from "sonner";
import { Plus, Pencil, Trash2, DollarSign, ImageIcon, Upload } from "lucide-react";

const emptyForm = {
  name: "",
  price: 0,
  description: "",
};

export default function DishesPage() {
  const { selectedRestaurant } = useRestaurant();
  const [dishes, setDishes] = useState<Dish[]>([]);
  const [dishImages, setDishImages] = useState<RestaurantImage[]>([]);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [editing, setEditing] = useState<Dish | null>(null);
  const [form, setForm] = useState(emptyForm);
  // UI-only image preview — image_id stored in backend but not uploaded via dish form
  const [imagePreview, setImagePreview] = useState<string | null>(null);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    if (selectedRestaurant) {
      (async () => {
        try {
          const [dishesData, imagesData] = await Promise.all([
            getRestaurantDishesApi(selectedRestaurant.restaurant_id),
            getRestaurantImagesApi(selectedRestaurant.restaurant_id),
          ]);

          setDishes(dishesData ?? []);
          // Dishes page only uses images with is_primary = 0.
          setDishImages((imagesData ?? []).filter((img) => !img.is_primary));
        } catch {
          toast.error("Không thể tải danh sách món ăn");
        }
      })();
    }
  }, [selectedRestaurant]);

  const getDishImageUrl = (dish: Dish): string | null => {
    if (!dish.image_id) return null;
    const matchedDishImage = dishImages.find((img) => img.image_id === dish.image_id);
    return matchedDishImage?.image_url ?? null;
  };

  const resetDialog = () => {
    setDialogOpen(false);
    setEditing(null);
    setForm({ ...emptyForm });
    setImagePreview(null);
    setSelectedFile(null);
    if (fileInputRef.current) fileInputRef.current.value = "";
  };

  const openNew = () => {
    resetDialog();
    setDialogOpen(true);
  };

  const openEdit = (dish: Dish) => {
    setEditing(dish);
    setForm({ name: dish.name, price: dish.price, description: dish.description });
    setImagePreview(getDishImageUrl(dish));
    setSelectedFile(null);
    setDialogOpen(true);
  };

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    setSelectedFile(file);
    const reader = new FileReader();
    reader.onload = () => setImagePreview(reader.result as string);
    reader.readAsDataURL(file);
  };

  const handleSave = async () => {
    if (!form.name.trim()) {
      toast.error("Vui lòng nhập tên món ăn");
      return;
    }

    if (!selectedRestaurant) return;

    try {
      if (editing) {
        const oldImageId = editing.image_id;
        let uploadedImage: RestaurantImage | null = null;
        let nextImageId = oldImageId;

        if (selectedFile) {
          uploadedImage = await uploadRestaurantImageApi(
            selectedRestaurant.restaurant_id,
            selectedFile,
            false,
            0,
          );
          nextImageId = uploadedImage.image_id;
          setDishImages((prev) => [...prev, uploadedImage]);
        }

        let updated: Dish;
        try {
          updated = await updateDishApi(editing.dish_id, {
            name: form.name,
            price: form.price,
            description: form.description,
            image_id: nextImageId,
          });
        } catch {
          if (uploadedImage) {
            const uploadedImageId = uploadedImage.image_id;
            try {
              await deleteImageApi(uploadedImageId);
              setDishImages((prev) => prev.filter((img) => img.image_id !== uploadedImageId));
            } catch {
              // Keep original update error as primary failure.
            }
          }
          throw new Error("UPDATE_DISH_FAILED");
        }

        if (selectedFile && oldImageId && nextImageId !== oldImageId) {
          try {
            await deleteImageApi(oldImageId);
            setDishImages((prev) => prev.filter((img) => img.image_id !== oldImageId));
          } catch {
            toast.warning("Đã cập nhật ảnh mới, nhưng chưa thể xóa ảnh cũ");
          }
        }

        setDishes((prev) =>
          prev.map((d) => (d.dish_id === editing.dish_id ? updated : d)),
        );
        toast.success("Cập nhật món ăn thành công");
      } else {
        let uploadedImage: RestaurantImage | null = null;
        let imageId: number | null = null;

        if (selectedFile) {
          uploadedImage = await uploadRestaurantImageApi(
            selectedRestaurant.restaurant_id,
            selectedFile,
            false,
            0,
          );
          imageId = uploadedImage.image_id;
          setDishImages((prev) => [...prev, uploadedImage]);
        }

        let created: Dish;
        try {
          created = await createDishApi(selectedRestaurant.restaurant_id, {
            name: form.name,
            price: form.price,
            description: form.description,
            image_id: imageId,
          });
        } catch {
          if (uploadedImage) {
            const uploadedImageId = uploadedImage.image_id;
            try {
              await deleteImageApi(uploadedImageId);
              setDishImages((prev) => prev.filter((img) => img.image_id !== uploadedImageId));
            } catch {
              // Keep original create error as primary failure.
            }
          }
          throw new Error("CREATE_DISH_FAILED");
        }

        setDishes((prev) => [...prev, created]);
        toast.success("Thêm món ăn thành công");
      }
      resetDialog();
    } catch {
      toast.error("Không thể lưu món ăn");
    }
  };

  const handleDelete = async (id: number) => {
    try {
      await deleteDishApi(id);
      setDishes((prev) => prev.filter((d) => d.dish_id !== id));
      toast.success("Xóa món ăn thành công");
    } catch {
      toast.error("Không thể xóa món ăn");
    }
  };

  return (
    <div className="max-w-3xl mx-auto animate-fade-in">
      <div className="page-header flex items-start justify-between">
        <div>
          <h1 className="page-title">Thực đơn</h1>
          <p className="page-description">Quản lý các món ăn của nhà hàng</p>
        </div>
        <Button onClick={openNew}>
          <Plus className="w-4 h-4 mr-2" /> Thêm món
        </Button>
      </div>

      {dishes.length === 0 ? (
        <div className="form-section text-center py-12">
          <p className="text-muted-foreground">Chưa có món ăn nào. Thêm món ăn đầu tiên để bắt đầu.</p>
        </div>
      ) : (
        <div className="space-y-4">
          {dishes.map((dish) => {
            const dishImageUrl = getDishImageUrl(dish);

            return (
              <div key={dish.dish_id} className="dashboard-card flex gap-4 items-start">
                {/* Dish image thumbnail — only render non-primary restaurant images (is_primary = 0). */}
                <div className="w-24 h-24 rounded-lg overflow-hidden border shrink-0 bg-muted flex items-center justify-center">
                  {dishImageUrl ? (
                    <img
                      src={dishImageUrl}
                      alt={dish.name}
                      className="w-full h-full object-cover"
                    />
                  ) : (
                    <ImageIcon className="w-8 h-8 text-muted-foreground opacity-40" />
                  )}
                </div>

                <div className="flex-1 min-w-0">
                  <h3 className="font-medium text-foreground truncate">{dish.name}</h3>
                  <p className="text-sm text-muted-foreground line-clamp-1">{dish.description}</p>
                  <div className="flex items-center gap-1 text-primary font-semibold mt-1">
                    <DollarSign className="w-4 h-4" />
                    {dish.price.toFixed(2)}
                  </div>
                </div>

                <div className="flex gap-1 shrink-0">
                  <Button variant="ghost" size="icon" onClick={() => openEdit(dish)} title="Chỉnh sửa">
                    <Pencil className="w-4 h-4" />
                  </Button>
                  <Button variant="ghost" size="icon" onClick={() => handleDelete(dish.dish_id)} title="Xóa">
                    <Trash2 className="w-4 h-4 text-destructive" />
                  </Button>
                </div>
              </div>
            );
          })}
        </div>
      )}

      <Dialog open={dialogOpen} onOpenChange={(open) => { if (!open) resetDialog(); else setDialogOpen(true); }}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{editing ? "Chỉnh sửa món ăn" : "Thêm món ăn mới"}</DialogTitle>
            <DialogDescription>
              {editing
                ? "Cập nhật thông tin và ảnh món ăn. Ảnh mới sẽ thay thế ảnh cũ."
                : "Điền thông tin và chọn ảnh cho món ăn mới."}
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4 py-2">
            <div className="space-y-2">
              <Label>Tên món</Label>
              <Input value={form.name} onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))} />
            </div>
            <div className="space-y-2">
              <Label>Giá</Label>
              <Input type="number" step="0.01" min="0" value={form.price} onChange={(e) => setForm((f) => ({ ...f, price: parseFloat(e.target.value) || 0 }))} />
            </div>
            <div className="space-y-2">
              <Label>Mô tả</Label>
              <Textarea value={form.description} onChange={(e) => setForm((f) => ({ ...f, description: e.target.value }))} rows={3} />
            </div>
            <div className="space-y-2">
              <Label>Ảnh món ăn</Label>
              {/* UI-only: displays local preview; backend dish upload does not include image */}
              <div
                className="border-2 border-dashed rounded-lg p-4 text-center cursor-pointer hover:border-primary transition-colors"
                onClick={() => fileInputRef.current?.click()}
              >
                {imagePreview ? (
                  <div className="space-y-2">
                    <img src={imagePreview} alt="Preview" className="max-h-36 mx-auto rounded-md object-contain" />
                    <p className="text-xs text-muted-foreground">Nhấn để chọn ảnh khác</p>
                  </div>
                ) : (
                  <div className="flex flex-col items-center gap-2 text-muted-foreground py-2">
                    <Upload className="w-6 h-6" />
                    <p className="text-sm">Nhấn để chọn ảnh</p>
                    <p className="text-xs">JPG, PNG, WEBP</p>
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
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={resetDialog}>Hủy</Button>
            <Button onClick={handleSave}>{editing ? "Cập nhật" : "Thêm"}</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
