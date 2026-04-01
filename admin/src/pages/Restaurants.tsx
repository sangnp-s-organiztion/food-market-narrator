import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import AdminLayout from "@/components/AdminLayout";
import { restaurantApi, type RestaurantResponse } from "@/lib/adminApi";
import { Search, Lock, Unlock } from "lucide-react";
import { Input } from "@/components/ui/input";
import { toast } from "sonner";
import ConfirmDialog from "@/components/ConfirmDialog";
import StatusBadge from "@/components/StatusBadge";

// Map backend RestaurantResponse → page-local shape (keeps existing UI fields intact)
function toPageRestaurant(r: RestaurantResponse) {
  return {
    restaurant_id: r.restaurantId,
    name: r.name,
    description: r.description ?? "",
    latitude: r.latitude,
    longitude: r.longitude,
    phone: r.phone ?? "",
    address: r.address ?? "",
    status: r.isActive ? ("active" as const) : ("inactive" as const),
    created_at: r.createdAt ? r.createdAt.split("T")[0] : "",
    user_id: r.userId,
    open_time: r.openTime ?? "",
    close_time: r.closeTime ?? "",
  };
}

const RestaurantsPage = () => {
  const qc = useQueryClient();
  const [search, setSearch] = useState("");
  const [confirmAction, setConfirmAction] = useState<{
    id: string;
    action: "lock" | "unlock";
    name: string;
  } | null>(null);

  // ── Fetch ──────────────────────────────────────────────────────────────────
  const {
    data: restaurants = [],
    isLoading,
    isError,
  } = useQuery({
    queryKey: ["admin", "restaurants"],
    queryFn: restaurantApi.getAll,
    staleTime: 60_000,
  });

  // ── Status mutation ────────────────────────────────────────────────────────
  const statusMutation = useMutation({
    mutationFn: ({ id, isActive }: { id: string; isActive: boolean }) =>
      restaurantApi.updateStatus(id, { isActive }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["admin", "restaurants"] });
      toast.success("Cập nhật trạng thái thành công");
    },
    onError: (err: Error) => {
      toast.error(err.message ?? "Cập nhật thất bại");
    },
  });

  // ── Local page model (derived from API response) ────────────────────────────
  const pageRestaurants = restaurants.map(toPageRestaurant);

  const filtered = pageRestaurants.filter(
    (r) =>
      r.name.toLowerCase().includes(search.toLowerCase()) ||
      r.address.toLowerCase().includes(search.toLowerCase())
  );

  const handleConfirmAction = () => {
    if (!confirmAction) return;
    const { id, action } = confirmAction;
    statusMutation.mutate({ id, isActive: action === "unlock" });
    setConfirmAction(null);
  };

  return (
    <AdminLayout>
      <div className="page-header">
        <h1 className="page-title">Quản lý nhà hàng</h1>
      </div>

      <div className="max-w-7xl mx-auto px-8 py-6">
        <div className="stat-card">
          {/* Search */}
          <div className="flex items-center gap-3 mb-4">
            <div className="relative flex-1 max-w-xs">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
              <Input
                placeholder="Tìm kiếm nhà hàng…"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                className="pl-9 h-9 text-sm"
              />
            </div>
            <span className="text-xs text-muted-foreground">
              {isLoading ? "…" : `${filtered.length} kết quả`}
            </span>
          </div>

          {/* Table */}
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
              {isLoading && (
                <tr>
                  <td colSpan={7} className="text-center py-8 text-muted-foreground">
                    Đang tải…
                  </td>
                </tr>
              )}
              {isError && (
                <tr>
                  <td colSpan={7} className="text-center py-8 text-destructive">
                    Không thể tải danh sách nhà hàng. Vui lòng thử lại.
                  </td>
                </tr>
              )}
              {!isLoading && !isError && filtered.length === 0 && (
                <tr>
                  <td colSpan={7} className="text-center py-8 text-muted-foreground">
                    Không có nhà hàng nào.
                  </td>
                </tr>
              )}
              {!isLoading &&
                !isError &&
                filtered.map((r) => (
                  <tr key={r.restaurant_id}>
                    <td className="font-medium">{r.name}</td>
                    <td className="text-muted-foreground text-xs">{r.address}</td>
                    <td className="mono text-xs">{r.phone}</td>
                    <td className="mono text-xs">
                      {r.open_time} – {r.close_time}
                    </td>
                    <td>
                      <StatusBadge status={r.status} />
                    </td>
                    <td className="mono text-xs text-muted-foreground">
                      {r.created_at}
                    </td>
                    <td>
                      <button
                        onClick={() =>
                          setConfirmAction({
                            id: r.restaurant_id,
                            action: r.status === "active" ? "lock" : "unlock",
                            name: r.name,
                          })
                        }
                        className={`p-1.5 rounded-md hover:bg-muted transition-colors ${
                          r.status === "active"
                            ? "text-destructive"
                            : "text-muted-foreground"
                        }`}
                        title={r.status === "active" ? "Khóa nhà hàng" : "Mở khóa"}
                      >
                        {r.status === "active" ? (
                          <Lock className="h-4 w-4" />
                        ) : (
                          <Unlock className="h-4 w-4" />
                        )}
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
        description="Bạn có chắc chắn muốn thực hiện hành động này không?"
        onConfirm={handleConfirmAction}
        variant={confirmAction?.action === "lock" ? "destructive" : "default"}
      />
    </AdminLayout>
  );
};

export default RestaurantsPage;
