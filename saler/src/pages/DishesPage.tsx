import { useState, useEffect } from "react";
import { getRestaurantDishes } from "@/services/mockData";
import { useRestaurant } from "@/contexts/RestaurantContext";
import type { Dish } from "@/types";
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
} from "@/components/ui/dialog";
import { toast } from "sonner";
import { Plus, Pencil, Trash2, DollarSign } from "lucide-react";

const emptyDish: Omit<Dish, "dish_id" | "restaurant_id" | "created_at"> = {
  name: "",
  price: 0,
  description: "",
  image_id: null,
};

export default function DishesPage() {
  const { selectedRestaurant } = useRestaurant();
  const [dishes, setDishes] = useState<Dish[]>([]);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [editing, setEditing] = useState<Dish | null>(null);
  const [form, setForm] = useState(emptyDish);

  useEffect(() => {
    if (selectedRestaurant) {
      setDishes(getRestaurantDishes(selectedRestaurant.restaurant_id));
    }
  }, [selectedRestaurant]);

  const openNew = () => {
    setEditing(null);
    setForm({ ...emptyDish });
    setDialogOpen(true);
  };

  const openEdit = (dish: Dish) => {
    setEditing(dish);
    setForm({ name: dish.name, price: dish.price, description: dish.description, image_id: dish.image_id });
    setDialogOpen(true);
  };

  const handleSave = () => {
    if (!form.name.trim()) {
      toast.error("Vui lòng nhập tên món ăn");
      return;
    }
    if (editing) {
      setDishes((prev) =>
        prev.map((d) => (d.dish_id === editing.dish_id ? { ...d, ...form } : d))
      );
      toast.success("Cập nhật món ăn thành công");
    } else {
      const newDish: Dish = {
        dish_id: Date.now(),
        restaurant_id: selectedRestaurant!.restaurant_id,
        created_at: new Date().toISOString(),
        ...form,
      };
      setDishes((prev) => [...prev, newDish]);
      toast.success("Thêm món ăn thành công");
    }
    setDialogOpen(false);
  };

  const handleDelete = (id: number) => {
    setDishes((prev) => prev.filter((d) => d.dish_id !== id));
    toast.success("Xóa món ăn thành công");
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
        <div className="space-y-3">
          {dishes.map((dish) => (
            <div key={dish.dish_id} className="dashboard-card flex items-center gap-4">
              <div className="flex-1 min-w-0">
                <h3 className="font-medium text-foreground truncate">{dish.name}</h3>
                <p className="text-sm text-muted-foreground line-clamp-1">{dish.description}</p>
              </div>
              <div className="flex items-center gap-1 text-primary font-semibold shrink-0">
                <DollarSign className="w-4 h-4" />
                {dish.price.toFixed(2)}
              </div>
              <div className="flex gap-1 shrink-0">
                <Button variant="ghost" size="icon" onClick={() => openEdit(dish)}>
                  <Pencil className="w-4 h-4" />
                </Button>
                <Button variant="ghost" size="icon" onClick={() => handleDelete(dish.dish_id)}>
                  <Trash2 className="w-4 h-4 text-destructive" />
                </Button>
              </div>
            </div>
          ))}
        </div>
      )}

      <Dialog open={dialogOpen} onOpenChange={setDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{editing ? "Chỉnh sửa món ăn" : "Thêm món ăn mới"}</DialogTitle>
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
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setDialogOpen(false)}>Hủy</Button>
            <Button onClick={handleSave}>{editing ? "Cập nhật" : "Thêm"}</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
