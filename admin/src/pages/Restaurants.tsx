import { useState } from "react";
import AdminLayout from "@/components/AdminLayout";
import { Search, Lock, Unlock } from "lucide-react";
import { Input } from "@/components/ui/input";
import { toast } from "sonner";
import ConfirmDialog from "@/components/ConfirmDialog";
import StatusBadge from "@/components/StatusBadge";

type EntityStatus = "active" | "inactive";

interface Restaurant {
  restaurant_id: number;
  name: string;
  address: string;
  phone: string;
  open_time: string;
  close_time: string;
  status: EntityStatus;
  created_at: string;
}

const RestaurantsPage = () => {
  const [data, setData] = useState<Restaurant[]>([]);
  const [search, setSearch] = useState("");
  const [confirmAction, setConfirmAction] = useState<{ id: number; action: "lock" | "unlock"; name: string } | null>(null);

  const filtered = data.filter(
    (r) =>
      r.name.toLowerCase().includes(search.toLowerCase()) ||
      r.address.toLowerCase().includes(search.toLowerCase())
  );

  const handleConfirmAction = () => {
    if (!confirmAction) return;
    const { id, action } = confirmAction;
    const newStatus: EntityStatus = action === "lock" ? "inactive" : "active";
    setData((d) => d.map((r) => (r.restaurant_id === id ? { ...r, status: newStatus } : r)));
    toast.success("Thao tác thành công");
    setConfirmAction(null);
  };

  return (
    <AdminLayout>
      <div className="page-header">
        <h1 className="page-title">Quản lý nhà hàng</h1>
      </div>

      <div className="max-w-7xl mx-auto px-8 py-6">
        <div className="stat-card">
          <div className="flex items-center gap-3 mb-4">
            <div className="relative flex-1 max-w-xs">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
              <Input placeholder="Tìm kiếm nhà hàng..." value={search} onChange={(e) => setSearch(e.target.value)} className="pl-9 h-9 text-sm" />
            </div>
            <span className="text-xs text-muted-foreground">{filtered.length} kết quả</span>
          </div>

          <table className="data-table">
            <thead>
              <tr>
                <th>Nhà hàng</th>
                <th>Địa chỉ</th>
                <th>Điện thoại</th>
                <th>Giờ mở cửa</th>
                <th>Trạng thái</th>
                <th>Ngày tạo</th>
                <th className="w-12"></th>
              </tr>
            </thead>
            <tbody>
              {filtered.map((r) => (
                <tr key={r.restaurant_id}>
                  <td className="font-medium">{r.name}</td>
                  <td className="text-muted-foreground text-xs">{r.address}</td>
                  <td className="mono text-xs">{r.phone}</td>
                  <td className="mono text-xs">{r.open_time} - {r.close_time}</td>
                  <td><StatusBadge status={r.status} /></td>
                  <td className="mono text-xs text-muted-foreground">{r.created_at}</td>
                  <td>
                    <button
                      onClick={() => setConfirmAction({ id: r.restaurant_id, action: r.status === "active" ? "lock" : "unlock", name: r.name })}
                      className={`p-1.5 rounded-md hover:bg-muted transition-colors ${r.status === "active" ? "text-destructive" : "text-muted-foreground"}`}
                      title={r.status === "active" ? "Khóa nhà hàng" : "Mở khóa"}
                    >
                      {r.status === "active" ? <Lock className="h-4 w-4" /> : <Unlock className="h-4 w-4" />}
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      <ConfirmDialog
        open={!!confirmAction}
        onOpenChange={(open) => !open && setConfirmAction(null)}
        title={confirmAction?.action === "lock" ? "Khóa nhà hàng" : "Mở khóa nhà hàng"}
        description="Bạn có chắc chắn muốn thực hiện hành động này không? Hành động này có thể ảnh hưởng đến dữ liệu và người dùng."
        onConfirm={handleConfirmAction}
        variant={confirmAction?.action === "lock" ? "destructive" : "default"}
      />
    </AdminLayout>
  );
};

export default RestaurantsPage;
