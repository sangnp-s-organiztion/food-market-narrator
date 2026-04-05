import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import AdminLayout from "@/components/AdminLayout";
import {
  restaurantApi,
  userApi,
  type RestaurantResponse,
  type CreateRestaurantRequest,
} from "@/lib/adminApi";
import { Search, Lock, Unlock, Plus, Eye } from "lucide-react";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { toast } from "sonner";
import ConfirmDialog from "@/components/ConfirmDialog";
import StatusBadge from "@/components/StatusBadge";

const API_BASE =
  (import.meta.env.VITE_API_BASE_URL as string | undefined) ??
  "http://localhost:5044";

function normalizeImageUrl(url: string | null | undefined): string | null {
  if (!url) return null;
  if (/^https?:\/\//i.test(url)) return url;

  const normalized = url.replace(/\\/g, "/").trim();
  if (!normalized) return null;

  if (normalized.startsWith("/")) {
    return new URL(normalized, API_BASE).toString();
  }

  return new URL(`/maui-images/${normalized}`, API_BASE).toString();
}

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

type RestaurantForm = {
  name: string;
  userId: string;
  description: string;
  phone: string;
  address: string;
  latitude: string;
  longitude: string;
  openTime: string;
  closeTime: string;
};

function toNullableNumber(value: string): number | null {
  const trimmed = value.trim();
  if (!trimmed) return null;
  const parsed = Number(trimmed);
  return Number.isFinite(parsed) ? parsed : null;
}

const RestaurantsPage = () => {
  const qc = useQueryClient();
  const [search, setSearch] = useState("");
  const [confirmAction, setConfirmAction] = useState<{
    id: string;
    action: "lock" | "unlock";
    name: string;
  } | null>(null);
  const [createOpen, setCreateOpen] = useState(false);
  const [detailOpen, setDetailOpen] = useState(false);
  const [selectedRestaurantId, setSelectedRestaurantId] = useState<
    string | null
  >(null);
  const [createForm, setCreateForm] = useState<RestaurantForm>({
    name: "",
    userId: "",
    description: "",
    phone: "",
    address: "",
    latitude: "",
    longitude: "",
    openTime: "",
    closeTime: "",
  });

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

  const { data: users = [] } = useQuery({
    queryKey: ["admin", "users"],
    queryFn: userApi.getAll,
    staleTime: 60_000,
  });

  const {
    data: selectedRestaurant,
    isLoading: isDetailLoading,
    isError: isDetailError,
  } = useQuery({
    queryKey: ["admin", "restaurants", "detail", selectedRestaurantId],
    queryFn: () => restaurantApi.getById(selectedRestaurantId ?? ""),
    enabled: detailOpen && !!selectedRestaurantId,
    staleTime: 60_000,
  });

  const sellerOptions = users.filter(
    (u) => u.role?.toLowerCase() === "saler" && u.isActive,
  );

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

  const createMutation = useMutation({
    mutationFn: (data: CreateRestaurantRequest) => restaurantApi.create(data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["admin", "restaurants"] });
      toast.success("Thêm nhà hàng thành công");
      setCreateOpen(false);
      setCreateForm({
        name: "",
        userId: "",
        description: "",
        phone: "",
        address: "",
        latitude: "",
        longitude: "",
        openTime: "",
        closeTime: "",
      });
    },
    onError: (err: Error) => {
      toast.error(err.message ?? "Thêm nhà hàng thất bại");
    },
  });

  // ── Local page model (derived from API response) ────────────────────────────
  const pageRestaurants = restaurants.map(toPageRestaurant);

  const filtered = pageRestaurants.filter(
    (r) =>
      r.name.toLowerCase().includes(search.toLowerCase()) ||
      r.address.toLowerCase().includes(search.toLowerCase()),
  );

  const handleConfirmAction = () => {
    if (!confirmAction) return;
    const { id, action } = confirmAction;
    statusMutation.mutate({ id, isActive: action === "unlock" });
    setConfirmAction(null);
  };

  const handleCreateRestaurant = () => {
    const trimmedName = createForm.name.trim();
    if (!trimmedName) {
      toast.error("Tên nhà hàng là bắt buộc");
      return;
    }

    const parsedUserId = Number(createForm.userId);
    if (!Number.isInteger(parsedUserId) || parsedUserId <= 0) {
      toast.error("Vui lòng chọn người bán quản lý nhà hàng");
      return;
    }

    createMutation.mutate({
      name: trimmedName,
      userId: parsedUserId,
      description: createForm.description.trim() || null,
      phone: createForm.phone.trim() || null,
      address: createForm.address.trim() || null,
      latitude: toNullableNumber(createForm.latitude),
      longitude: toNullableNumber(createForm.longitude),
      openTime: createForm.openTime || null,
      closeTime: createForm.closeTime || null,
      isActive: true,
    });
  };

  const handleOpenDetail = (restaurantId: string) => {
    setSelectedRestaurantId(restaurantId);
    setDetailOpen(true);
  };

  const handleDetailDialogChange = (open: boolean) => {
    setDetailOpen(open);
    if (!open) {
      setSelectedRestaurantId(null);
    }
  };

  const detailPrimaryImage = selectedRestaurant?.images?.find(
    (img) => img.isPrimary,
  );
  const detailImageFallback = selectedRestaurant?.images?.[0];
  const detailPreviewImageUrl = normalizeImageUrl(
    detailPrimaryImage?.imageUrl ?? detailImageFallback?.imageUrl ?? null,
  );

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
            <Button onClick={() => setCreateOpen(true)} className="h-9 gap-2">
              <Plus className="h-4 w-4" />
              Thêm nhà hàng
            </Button>
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
                <th className="w-24"></th>
              </tr>
            </thead>
            <tbody>
              {isLoading && (
                <tr>
                  <td
                    colSpan={7}
                    className="text-center py-8 text-muted-foreground"
                  >
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
                  <td
                    colSpan={7}
                    className="text-center py-8 text-muted-foreground"
                  >
                    Không có nhà hàng nào.
                  </td>
                </tr>
              )}
              {!isLoading &&
                !isError &&
                filtered.map((r) => (
                  <tr key={r.restaurant_id}>
                    <td className="font-medium">{r.name}</td>
                    <td className="text-muted-foreground text-xs">
                      {r.address}
                    </td>
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
                      <div className="flex items-center gap-1">
                        <button
                          onClick={() => handleOpenDetail(r.restaurant_id)}
                          className="p-1.5 rounded-md hover:bg-muted transition-colors text-muted-foreground"
                          title="Xem chi tiết"
                        >
                          <Eye className="h-4 w-4" />
                        </button>
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
                          title={
                            r.status === "active" ? "Khóa nhà hàng" : "Mở khóa"
                          }
                        >
                          {r.status === "active" ? (
                            <Lock className="h-4 w-4" />
                          ) : (
                            <Unlock className="h-4 w-4" />
                          )}
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
            </tbody>
          </table>
        </div>
      </div>

      <Dialog
        open={createOpen}
        onOpenChange={(open) => {
          setCreateOpen(open);
        }}
      >
        <DialogContent className="max-w-lg">
          <DialogHeader>
            <DialogTitle>Thêm nhà hàng mới</DialogTitle>
          </DialogHeader>
          <div className="grid gap-3 py-2">
            <div>
              <Label className="text-xs">Tên nhà hàng</Label>
              <Input
                value={createForm.name}
                onChange={(e) =>
                  setCreateForm({ ...createForm, name: e.target.value })
                }
                className="mt-1"
                placeholder="Tên nhà hàng"
              />
            </div>
            <div>
              <Label className="text-xs">Người bán quản lý</Label>
              <select
                value={createForm.userId}
                onChange={(e) =>
                  setCreateForm({ ...createForm, userId: e.target.value })
                }
                className="mt-1 h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
              >
                <option value="">Chọn người bán</option>
                {sellerOptions.map((u) => (
                  <option key={u.userId} value={u.userId.toString()}>
                    {u.username} (ID: {u.userId})
                  </option>
                ))}
              </select>
            </div>
            <div>
              <Label className="text-xs">Mô tả</Label>
              <Input
                value={createForm.description}
                onChange={(e) =>
                  setCreateForm({ ...createForm, description: e.target.value })
                }
                className="mt-1"
                placeholder="Mô tả"
              />
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div>
                <Label className="text-xs">Điện thoại</Label>
                <Input
                  value={createForm.phone}
                  onChange={(e) =>
                    setCreateForm({ ...createForm, phone: e.target.value })
                  }
                  className="mt-1"
                  placeholder="Số điện thoại"
                />
              </div>
              <div>
                <Label className="text-xs">Địa chỉ</Label>
                <Input
                  value={createForm.address}
                  onChange={(e) =>
                    setCreateForm({ ...createForm, address: e.target.value })
                  }
                  className="mt-1"
                  placeholder="Địa chỉ"
                />
              </div>
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div>
                <Label className="text-xs">Vĩ độ</Label>
                <Input
                  value={createForm.latitude}
                  onChange={(e) =>
                    setCreateForm({ ...createForm, latitude: e.target.value })
                  }
                  className="mt-1"
                  placeholder="10.123456"
                />
              </div>
              <div>
                <Label className="text-xs">Kinh độ</Label>
                <Input
                  value={createForm.longitude}
                  onChange={(e) =>
                    setCreateForm({ ...createForm, longitude: e.target.value })
                  }
                  className="mt-1"
                  placeholder="106.123456"
                />
              </div>
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div>
                <Label className="text-xs">Giờ mở cửa</Label>
                <Input
                  type="time"
                  value={createForm.openTime}
                  onChange={(e) =>
                    setCreateForm({ ...createForm, openTime: e.target.value })
                  }
                  className="mt-1"
                />
              </div>
              <div>
                <Label className="text-xs">Giờ đóng cửa</Label>
                <Input
                  type="time"
                  value={createForm.closeTime}
                  onChange={(e) =>
                    setCreateForm({ ...createForm, closeTime: e.target.value })
                  }
                  className="mt-1"
                />
              </div>
            </div>
            <Button
              onClick={handleCreateRestaurant}
              className="mt-2"
              disabled={createMutation.isPending}
            >
              {createMutation.isPending ? "Đang tạo..." : "Tạo nhà hàng"}
            </Button>
          </div>
        </DialogContent>
      </Dialog>

      <Dialog open={detailOpen} onOpenChange={handleDetailDialogChange}>
        <DialogContent className="max-w-2xl">
          <DialogHeader>
            <DialogTitle>Chi tiết nhà hàng</DialogTitle>
          </DialogHeader>

          {isDetailLoading && (
            <p className="text-sm text-muted-foreground">
              Đang tải chi tiết...
            </p>
          )}

          {isDetailError && (
            <p className="text-sm text-destructive">
              Không thể tải chi tiết nhà hàng. Vui lòng thử lại.
            </p>
          )}

          {!isDetailLoading && !isDetailError && selectedRestaurant && (
            <div className="space-y-4 py-1">
              <div className="grid grid-cols-2 gap-3 text-sm">
                <div>
                  <Label className="text-xs text-muted-foreground">
                    Tên nhà hàng
                  </Label>
                  <p className="mt-1 font-medium">{selectedRestaurant.name}</p>
                </div>
                <div>
                  <Label className="text-xs text-muted-foreground">
                    Mã nhà hàng
                  </Label>
                  <p className="mt-1 mono text-xs">
                    {selectedRestaurant.restaurantId}
                  </p>
                </div>
                <div>
                  <Label className="text-xs text-muted-foreground">
                    Trạng thái
                  </Label>
                  <div className="mt-1">
                    <StatusBadge
                      status={
                        selectedRestaurant.isActive ? "active" : "inactive"
                      }
                    />
                  </div>
                </div>
                <div>
                  <Label className="text-xs text-muted-foreground">
                    Người bán quản lý (User ID)
                  </Label>
                  <p className="mt-1">{selectedRestaurant.userId}</p>
                </div>
                <div>
                  <Label className="text-xs text-muted-foreground">
                    Điện thoại
                  </Label>
                  <p className="mt-1 mono text-xs">
                    {selectedRestaurant.phone || "—"}
                  </p>
                </div>
                <div>
                  <Label className="text-xs text-muted-foreground">
                    Ngày tạo
                  </Label>
                  <p className="mt-1 mono text-xs">
                    {new Date(selectedRestaurant.createdAt).toLocaleString(
                      "vi-VN",
                    )}
                  </p>
                </div>
                <div className="col-span-2">
                  <Label className="text-xs text-muted-foreground">
                    Địa chỉ
                  </Label>
                  <p className="mt-1">{selectedRestaurant.address || "—"}</p>
                </div>
                <div>
                  <Label className="text-xs text-muted-foreground">
                    Giờ mở cửa
                  </Label>
                  <p className="mt-1 mono text-xs">
                    {selectedRestaurant.openTime || "—"}
                  </p>
                </div>
                <div>
                  <Label className="text-xs text-muted-foreground">
                    Giờ đóng cửa
                  </Label>
                  <p className="mt-1 mono text-xs">
                    {selectedRestaurant.closeTime || "—"}
                  </p>
                </div>
                <div>
                  <Label className="text-xs text-muted-foreground">Vĩ độ</Label>
                  <p className="mt-1 mono text-xs">
                    {selectedRestaurant.latitude ?? "—"}
                  </p>
                </div>
                <div>
                  <Label className="text-xs text-muted-foreground">
                    Kinh độ
                  </Label>
                  <p className="mt-1 mono text-xs">
                    {selectedRestaurant.longitude ?? "—"}
                  </p>
                </div>
                <div>
                  <Label className="text-xs text-muted-foreground">
                    Số ảnh
                  </Label>
                  <p className="mt-1">{selectedRestaurant.images.length}</p>
                </div>
                <div>
                  <Label className="text-xs text-muted-foreground">
                    Số audio
                  </Label>
                  <p className="mt-1">{selectedRestaurant.audios.length}</p>
                </div>
                <div className="col-span-2">
                  <Label className="text-xs text-muted-foreground">Mô tả</Label>
                  <p className="mt-1 whitespace-pre-wrap">
                    {selectedRestaurant.description || "—"}
                  </p>
                </div>
              </div>

              {detailPreviewImageUrl && (
                <div>
                  <Label className="text-xs text-muted-foreground">
                    Ảnh đại diện
                  </Label>
                  <img
                    src={detailPreviewImageUrl}
                    alt={selectedRestaurant.name}
                    className="mt-2 h-[22rem] w-full rounded-md object-cover border"
                  />
                </div>
              )}
            </div>
          )}
        </DialogContent>
      </Dialog>

      <ConfirmDialog
        open={!!confirmAction}
        onOpenChange={(open) => !open && setConfirmAction(null)}
        title={
          confirmAction?.action === "lock"
            ? "Khóa nhà hàng"
            : "Mở khóa nhà hàng"
        }
        description="Bạn có chắc chắn muốn thực hiện hành động này không?"
        onConfirm={handleConfirmAction}
        variant={confirmAction?.action === "lock" ? "destructive" : "default"}
      />
    </AdminLayout>
  );
};

export default RestaurantsPage;
