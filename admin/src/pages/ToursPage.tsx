import { useEffect, useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Eye, GripVertical, Plus } from "lucide-react";
import { toast } from "sonner";
import AdminLayout from "@/components/AdminLayout";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Label } from "@/components/ui/label";
import {
  restaurantApi,
  tourApi,
  type TourResponse,
  type TourStopResponse,
} from "@/lib/adminApi";

function getNextStopOrder(tour: TourResponse | undefined): number {
  if (!tour || tour.stops.length === 0) return 1;
  const maxStop = Math.max(...tour.stops.map((s) => s.stopOrder));
  return maxStop + 1;
}

const ToursPage = () => {
  const qc = useQueryClient();
  const [detailOpen, setDetailOpen] = useState(false);
  const [selectedTourId, setSelectedTourId] = useState<number | null>(null);
  const [addRestaurantId, setAddRestaurantId] = useState("");
  const [draftStops, setDraftStops] = useState<TourStopResponse[]>([]);
  const [draggingRestaurantId, setDraggingRestaurantId] = useState<string | null>(null);

  const {
    data: tours = [],
    isLoading,
    isError,
  } = useQuery({
    queryKey: ["admin", "tours"],
    queryFn: tourApi.getAll,
    staleTime: 60_000,
  });

  const { data: restaurants = [] } = useQuery({
    queryKey: ["admin", "restaurants"],
    queryFn: restaurantApi.getAll,
    staleTime: 60_000,
  });

  const {
    data: selectedTour,
    isLoading: isDetailLoading,
    isError: isDetailError,
  } = useQuery({
    queryKey: ["admin", "tour", selectedTourId],
    queryFn: () => tourApi.getById(selectedTourId ?? 0),
    enabled: detailOpen && selectedTourId !== null,
    staleTime: 30_000,
  });

  useEffect(() => {
    if (!selectedTour) {
      setDraftStops([]);
      return;
    }

    const sorted = [...selectedTour.stops].sort((a, b) => a.stopOrder - b.stopOrder);
    setDraftStops(sorted);
  }, [selectedTour]);

  const availableRestaurants = useMemo(() => {
    const stopIds = new Set(draftStops.map((s) => s.restaurantId));
    return restaurants
      .filter((r) => r.isActive && !stopIds.has(r.restaurantId))
      .sort((a, b) => a.name.localeCompare(b.name));
  }, [restaurants, draftStops]);

  const hasOrderChanges = useMemo(() => {
    if (!selectedTour) return false;
    const original = [...selectedTour.stops]
      .sort((a, b) => a.stopOrder - b.stopOrder)
      .map((s) => s.restaurantId);
    const draft = draftStops.map((s) => s.restaurantId);
    if (original.length !== draft.length) return true;

    return original.some((restaurantId, index) => restaurantId !== draft[index]);
  }, [selectedTour, draftStops]);

  const addRestaurantMutation = useMutation({
    mutationFn: (payload: { tourId: number; restaurantId: string }) =>
      tourApi.addRestaurant(payload.tourId, {
        restaurantId: payload.restaurantId,
      }),
    onSuccess: async () => {
      await qc.invalidateQueries({ queryKey: ["admin", "tours"] });

      if (selectedTourId !== null) {
        await qc.invalidateQueries({ queryKey: ["admin", "tour", selectedTourId] });
      }

      setAddRestaurantId("");
      toast.success("Thêm nhà hàng vào tour thành công");
    },
    onError: (err: Error) => {
      toast.error(err.message ?? "Thêm nhà hàng vào tour thất bại");
    },
  });

  const reorderStopsMutation = useMutation({
    mutationFn: (payload: { tourId: number; restaurantIds: string[] }) =>
      tourApi.reorderStops(payload.tourId, { restaurantIds: payload.restaurantIds }),
    onSuccess: async () => {
      await qc.invalidateQueries({ queryKey: ["admin", "tours"] });
      if (selectedTourId !== null) {
        await qc.invalidateQueries({ queryKey: ["admin", "tour", selectedTourId] });
      }

      toast.success("Đã cập nhật thứ tự stop_order");
    },
    onError: (err: Error) => {
      toast.error(err.message ?? "Cập nhật thứ tự thất bại");
    },
  });

  const handleOpenDetail = (tourId: number) => {
    setSelectedTourId(tourId);
    setAddRestaurantId("");
    setDetailOpen(true);
  };

  const handleDialogOpenChange = (open: boolean) => {
    setDetailOpen(open);
    if (!open) {
      setSelectedTourId(null);
      setAddRestaurantId("");
      setDraftStops([]);
      setDraggingRestaurantId(null);
    }
  };

  const handleAddRestaurant = () => {
    if (selectedTourId === null) return;

    const restaurantId = addRestaurantId.trim();
    if (!restaurantId) {
      toast.error("Vui lòng chọn nhà hàng");
      return;
    }

    if (hasOrderChanges) {
      toast.error("Vui lòng lưu thứ tự hiện tại trước khi thêm nhà hàng mới");
      return;
    }

    addRestaurantMutation.mutate({
      tourId: selectedTourId,
      restaurantId,
    });
  };

  const handleDropOnStop = (targetRestaurantId: string) => {
    if (!draggingRestaurantId || draggingRestaurantId === targetRestaurantId) return;

    const current = [...draftStops];
    const fromIndex = current.findIndex((s) => s.restaurantId === draggingRestaurantId);
    const toIndex = current.findIndex((s) => s.restaurantId === targetRestaurantId);
    if (fromIndex < 0 || toIndex < 0) return;

    const [moved] = current.splice(fromIndex, 1);
    current.splice(toIndex, 0, moved);

    const normalized = current.map((stop, index) => ({
      ...stop,
      stopOrder: index + 1,
    }));

    setDraftStops(normalized);
    setDraggingRestaurantId(null);
  };

  const handleSaveOrder = () => {
    if (selectedTourId === null || !hasOrderChanges) return;
    reorderStopsMutation.mutate({
      tourId: selectedTourId,
      restaurantIds: draftStops.map((s) => s.restaurantId),
    });
  };

  return (
    <AdminLayout>
      <div className="page-header">
        <h1 className="page-title">Quản lý tour</h1>
      </div>

      <div className="mx-auto max-w-7xl px-8 py-6">
        <div className="stat-card">
          <table className="data-table">
            <thead>
              <tr>
                <th>Tên tour</th>
                <th className="w-36">Số điểm dừng</th>
                <th className="w-40">Thời gian dự kiến</th>
                <th className="w-28">Ưu tiên</th>
                <th className="w-24">Nổi bật</th>
                <th className="w-24">Hành động</th>
              </tr>
            </thead>
            <tbody>
              {isLoading && (
                <tr>
                  <td colSpan={6} className="py-8 text-center text-muted-foreground">
                    Đang tải danh sách tour...
                  </td>
                </tr>
              )}
              {isError && (
                <tr>
                  <td colSpan={6} className="py-8 text-center text-destructive">
                    Không thể tải danh sách tour. Vui lòng thử lại.
                  </td>
                </tr>
              )}
              {!isLoading && !isError && tours.length === 0 && (
                <tr>
                  <td colSpan={6} className="py-8 text-center text-muted-foreground">
                    Chưa có tour nào.
                  </td>
                </tr>
              )}
              {!isLoading &&
                !isError &&
                tours.map((tour) => (
                  <tr key={tour.tourId}>
                    <td className="font-medium">{tour.name}</td>
                    <td>{tour.stopCount}</td>
                    <td>{tour.estimatedDurationMinutes ? `${tour.estimatedDurationMinutes} phút` : "-"}</td>
                    <td>{tour.sortPriority}</td>
                    <td>{tour.isFeatured ? "Có" : "Không"}</td>
                    <td>
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={() => handleOpenDetail(tour.tourId)}
                        title="Xem tour"
                      >
                        <Eye className="h-4 w-4" />
                      </Button>
                    </td>
                  </tr>
                ))}
            </tbody>
          </table>
        </div>
      </div>

      <Dialog open={detailOpen} onOpenChange={handleDialogOpenChange}>
        <DialogContent className="max-h-[85vh] max-w-4xl overflow-y-auto">
          <DialogHeader>
            <DialogTitle>Chi tiết tour</DialogTitle>
          </DialogHeader>

          {isDetailLoading && (
            <p className="text-sm text-muted-foreground">Đang tải chi tiết tour...</p>
          )}

          {isDetailError && (
            <p className="text-sm text-destructive">Không thể tải chi tiết tour. Vui lòng thử lại.</p>
          )}

          {!isDetailLoading && !isDetailError && selectedTour && (
            <div className="space-y-5">
              <div className="rounded-md border p-4">
                <p className="text-sm text-muted-foreground">Tour</p>
                <p className="mt-1 text-base font-semibold">{selectedTour.name}</p>
                <p className="mt-1 text-sm text-muted-foreground">
                  Tổng số điểm dừng: {selectedTour.stopCount}
                </p>
              </div>

              <div className="rounded-md border p-4">
                <div className="mb-3 flex items-center justify-between">
                  <h3 className="text-sm font-semibold">Danh sách nhà hàng theo stop_order</h3>
                </div>

                <table className="data-table">
                  <thead>
                    <tr>
                      <th className="w-12"></th>
                      <th className="w-28">Stop order</th>
                      <th className="w-64">Restaurant ID</th>
                      <th>Tên nhà hàng</th>
                      <th>Địa chỉ</th>
                    </tr>
                  </thead>
                  <tbody>
                    {draftStops.length === 0 && (
                      <tr>
                        <td colSpan={5} className="py-6 text-center text-muted-foreground">
                          Tour này chưa có nhà hàng nào.
                        </td>
                      </tr>
                    )}
                    {draftStops.map((stop) => (
                      <tr
                        key={stop.restaurantId}
                        draggable
                        onDragStart={() => setDraggingRestaurantId(stop.restaurantId)}
                        onDragEnd={() => setDraggingRestaurantId(null)}
                        onDragOver={(e) => e.preventDefault()}
                        onDrop={() => handleDropOnStop(stop.restaurantId)}
                        className={draggingRestaurantId === stop.restaurantId ? "opacity-60" : ""}
                      >
                        <td>
                          <button
                            type="button"
                            className="text-muted-foreground hover:text-foreground"
                            title="Kéo thả để sắp xếp thứ tự"
                          >
                            <GripVertical className="h-4 w-4" />
                          </button>
                        </td>
                        <td>{stop.stopOrder}</td>
                        <td className="mono text-xs">{stop.restaurantId}</td>
                        <td className="font-medium">{stop.restaurantName}</td>
                        <td className="text-xs text-muted-foreground">{stop.address || "-"}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
                <div className="mt-4 flex items-center justify-end">
                  <Button
                    onClick={handleSaveOrder}
                    disabled={!hasOrderChanges || reorderStopsMutation.isPending}
                  >
                    {reorderStopsMutation.isPending ? "Đang cập nhật..." : "Lưu cập nhật thứ tự"}
                  </Button>
                </div>
              </div>

              <div className="rounded-md border p-4">
                <h3 className="mb-3 text-sm font-semibold">Thêm nhà hàng vào tour</h3>

                <div className="grid gap-3">
                  <div>
                    <Label className="text-xs">Nhà hàng</Label>
                    <select
                      value={addRestaurantId}
                      onChange={(e) => setAddRestaurantId(e.target.value)}
                      className="mt-1 h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
                    >
                      <option value="">Chọn nhà hàng</option>
                      {availableRestaurants.map((restaurant) => (
                        <option key={restaurant.restaurantId} value={restaurant.restaurantId}>
                          {restaurant.name} ({restaurant.restaurantId})
                        </option>
                      ))}
                    </select>
                  </div>
                </div>
                <p className="mt-2 text-xs text-muted-foreground">
                  Stop order sẽ tự động là: {getNextStopOrder(selectedTour)}
                </p>

                <div className="mt-4">
                  <Button
                    onClick={handleAddRestaurant}
                    disabled={
                      addRestaurantMutation.isPending ||
                      availableRestaurants.length === 0 ||
                      hasOrderChanges
                    }
                    className="gap-2"
                  >
                    <Plus className="h-4 w-4" />
                    {addRestaurantMutation.isPending ? "Đang thêm..." : "Thêm nhà hàng"}
                  </Button>
                  {availableRestaurants.length === 0 && (
                    <p className="mt-2 text-xs text-muted-foreground">
                      Không còn nhà hàng nào để thêm vào tour này.
                    </p>
                  )}
                </div>
              </div>
            </div>
          )}
        </DialogContent>
      </Dialog>
    </AdminLayout>
  );
};

export default ToursPage;
