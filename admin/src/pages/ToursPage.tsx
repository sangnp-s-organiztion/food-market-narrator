import { useEffect, useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Eye, GripVertical, Lock, Plus, Unlock } from "lucide-react";
import { toast } from "sonner";
import AdminLayout from "@/components/AdminLayout";
import ConfirmDialog from "@/components/ConfirmDialog";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
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
  const [createOpen, setCreateOpen] = useState(false);
  const [detailOpen, setDetailOpen] = useState(false);
  const [selectedTourId, setSelectedTourId] = useState<number | null>(null);
  const [addRestaurantId, setAddRestaurantId] = useState("");
  const [confirmTour, setConfirmTour] = useState<{
    id: number;
    name: string;
    lock: boolean;
    estimatedDurationMinutes: number | null;
    sortPriority: number;
    isFeatured: boolean;
  } | null>(null);
  const [draftStops, setDraftStops] = useState<TourStopResponse[]>([]);
  const [draggingRestaurantId, setDraggingRestaurantId] = useState<string | null>(null);
  const [draftEstimatedDurationMinutes, setDraftEstimatedDurationMinutes] = useState("");
  const [draftSortPriority, setDraftSortPriority] = useState("");
  const [draftIsFeatured, setDraftIsFeatured] = useState(false);
  const [createName, setCreateName] = useState("");
  const [createShortDescription, setCreateShortDescription] = useState("");
  const [createDescription, setCreateDescription] = useState("");
  const [createEstimatedDurationMinutes, setCreateEstimatedDurationMinutes] = useState("");
  const [createSortPriority, setCreateSortPriority] = useState("0");
  const [createIsFeatured, setCreateIsFeatured] = useState(false);

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
      setDraftEstimatedDurationMinutes("");
      setDraftSortPriority("");
      setDraftIsFeatured(false);
      return;
    }

    const sorted = [...selectedTour.stops].sort((a, b) => a.stopOrder - b.stopOrder);
    setDraftStops(sorted);
    setDraftEstimatedDurationMinutes(
      selectedTour.estimatedDurationMinutes !== null
        ? `${selectedTour.estimatedDurationMinutes}`
        : "",
    );
    setDraftSortPriority(`${selectedTour.sortPriority}`);
    setDraftIsFeatured(selectedTour.isFeatured);
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

  const hasMetaChanges = useMemo(() => {
    if (!selectedTour) return false;

    const estimatedDuration =
      draftEstimatedDurationMinutes.trim().length === 0
        ? null
        : Number(draftEstimatedDurationMinutes);
    const sortPriority = Number(draftSortPriority);

    if (
      estimatedDuration !== selectedTour.estimatedDurationMinutes ||
      sortPriority !== selectedTour.sortPriority ||
      draftIsFeatured !== selectedTour.isFeatured
    ) {
      return true;
    }

    return false;
  }, [
    selectedTour,
    draftEstimatedDurationMinutes,
    draftSortPriority,
    draftIsFeatured,
  ]);

  const hasUnsavedChanges = hasOrderChanges || hasMetaChanges;

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

  const createTourMutation = useMutation({
    mutationFn: (payload: {
      name: string;
      shortDescription: string | null;
      description: string | null;
      estimatedDurationMinutes: number | null;
      sortPriority: number;
      isActive: boolean;
      isFeatured: boolean;
    }) =>
      tourApi.create({
        name: payload.name,
        shortDescription: payload.shortDescription,
        description: payload.description,
        estimatedDurationMinutes: payload.estimatedDurationMinutes,
        sortPriority: payload.sortPriority,
        isActive: payload.isActive,
        isFeatured: payload.isFeatured,
      }),
    onSuccess: async () => {
      await qc.invalidateQueries({ queryKey: ["admin", "tours"] });
      setCreateOpen(false);
      setCreateName("");
      setCreateShortDescription("");
      setCreateDescription("");
      setCreateEstimatedDurationMinutes("");
      setCreateSortPriority("0");
      setCreateIsFeatured(false);
      toast.success("Tạo tour thành công");
    },
    onError: (err: Error) => {
      toast.error(err.message ?? "Tạo tour thất bại");
    },
  });

  const statusMutation = useMutation({
    mutationFn: (payload: {
      id: number;
      isActive: boolean;
      estimatedDurationMinutes: number | null;
      sortPriority: number;
      isFeatured: boolean;
    }) =>
      tourApi.update(payload.id, {
        estimatedDurationMinutes: payload.estimatedDurationMinutes,
        sortPriority: payload.sortPriority,
        isActive: payload.isActive,
        isFeatured: payload.isFeatured,
      }),
    onSuccess: async () => {
      await qc.invalidateQueries({ queryKey: ["admin", "tours"] });
      if (selectedTourId !== null) {
        await qc.invalidateQueries({ queryKey: ["admin", "tour", selectedTourId] });
      }
      toast.success("Cập nhật trạng thái tour thành công");
      setConfirmTour(null);
    },
    onError: (err: Error) => {
      toast.error(err.message ?? "Cập nhật trạng thái tour thất bại");
    },
  });

  const saveChangesMutation = useMutation({
    mutationFn: async (payload: {
      tourId: number;
      restaurantIds: string[];
      estimatedDurationMinutes: number | null;
      sortPriority: number;
      isActive: boolean;
      isFeatured: boolean;
      hasOrderChanges: boolean;
      hasMetaChanges: boolean;
    }) => {
      if (payload.hasOrderChanges) {
        await tourApi.reorderStops(payload.tourId, { restaurantIds: payload.restaurantIds });
      }

      if (payload.hasMetaChanges) {
        await tourApi.update(payload.tourId, {
          estimatedDurationMinutes: payload.estimatedDurationMinutes,
          sortPriority: payload.sortPriority,
          isActive: payload.isActive,
          isFeatured: payload.isFeatured,
        });
      }
    },
    onSuccess: async () => {
      await qc.invalidateQueries({ queryKey: ["admin", "tours"] });
      if (selectedTourId !== null) {
        await qc.invalidateQueries({ queryKey: ["admin", "tour", selectedTourId] });
      }

      toast.success("Đã lưu cập nhật tour");
    },
    onError: (err: Error) => {
      toast.error(err.message ?? "Lưu cập nhật thất bại");
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
      setDraftEstimatedDurationMinutes("");
      setDraftSortPriority("");
      setDraftIsFeatured(false);
    }
  };

  const handleAddRestaurant = () => {
    if (selectedTourId === null) return;

    const restaurantId = addRestaurantId.trim();
    if (!restaurantId) {
      toast.error("Vui lòng chọn nhà hàng");
      return;
    }

    if (hasUnsavedChanges) {
      toast.error("Vui lòng lưu cập nhật hiện tại trước khi thêm nhà hàng mới");
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

  const handleSaveChanges = () => {
    if (selectedTourId === null || !hasUnsavedChanges) return;

    const estimatedDuration =
      draftEstimatedDurationMinutes.trim().length === 0
        ? null
        : Number(draftEstimatedDurationMinutes);
    const sortPriority = Number(draftSortPriority);

    if (estimatedDuration !== null && (!Number.isInteger(estimatedDuration) || estimatedDuration < 0)) {
      toast.error("Thời gian dự kiến phải là số nguyên lớn hơn hoặc bằng 0");
      return;
    }

    if (!Number.isInteger(sortPriority) || sortPriority < 0) {
      toast.error("Ưu tiên phải là số nguyên lớn hơn hoặc bằng 0");
      return;
    }

    saveChangesMutation.mutate({
      tourId: selectedTourId,
      restaurantIds: draftStops.map((s) => s.restaurantId),
      estimatedDurationMinutes: estimatedDuration,
      sortPriority,
      isActive: selectedTour?.isActive ?? true,
      isFeatured: draftIsFeatured,
      hasOrderChanges,
      hasMetaChanges,
    });
  };

  const handleCreateTour = () => {
    const name = createName.trim();
    if (!name) {
      toast.error("Vui lòng nhập tên tour");
      return;
    }

    const estimatedDuration =
      createEstimatedDurationMinutes.trim().length === 0
        ? null
        : Number(createEstimatedDurationMinutes);
    const sortPriority = Number(createSortPriority);

    if (estimatedDuration !== null && (!Number.isInteger(estimatedDuration) || estimatedDuration < 0)) {
      toast.error("Thời gian dự kiến phải là số nguyên lớn hơn hoặc bằng 0");
      return;
    }

    if (!Number.isInteger(sortPriority) || sortPriority < 0) {
      toast.error("Ưu tiên phải là số nguyên lớn hơn hoặc bằng 0");
      return;
    }

    createTourMutation.mutate({
      name,
      shortDescription: createShortDescription.trim() || null,
      description: createDescription.trim() || null,
      estimatedDurationMinutes: estimatedDuration,
      sortPriority,
      isActive: true,
      isFeatured: createIsFeatured,
    });
  };

  return (
    <AdminLayout>
      <div className="page-header flex items-center justify-between gap-3">
        <h1 className="page-title">Quản lý tour</h1>
        <Button onClick={() => setCreateOpen(true)} className="gap-2">
          <Plus className="h-4 w-4" />
          Thêm tour
        </Button>
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
                <th className="w-28">Trạng thái</th>
                <th className="w-24">Nổi bật</th>
                <th className="w-24">Hành động</th>
              </tr>
            </thead>
            <tbody>
              {isLoading && (
                <tr>
                  <td colSpan={7} className="py-8 text-center text-muted-foreground">
                    Đang tải danh sách tour...
                  </td>
                </tr>
              )}
              {isError && (
                <tr>
                  <td colSpan={7} className="py-8 text-center text-destructive">
                    Không thể tải danh sách tour. Vui lòng thử lại.
                  </td>
                </tr>
              )}
              {!isLoading && !isError && tours.length === 0 && (
                <tr>
                  <td colSpan={7} className="py-8 text-center text-muted-foreground">
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
                    <td>
                      <span className={tour.isActive ? "status-active" : "status-inactive"}>
                        {tour.isActive ? "Hoạt động" : "Ngưng hoạt động"}
                      </span>
                    </td>
                    <td>{tour.isFeatured ? "Có" : "Không"}</td>
                    <td className="flex items-center gap-1">
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={() => handleOpenDetail(tour.tourId)}
                        title="Xem tour"
                      >
                        <Eye className="h-4 w-4" />
                      </Button>
                      <button
                        disabled={statusMutation.isPending}
                        onClick={() =>
                          setConfirmTour({
                            id: tour.tourId,
                            name: tour.name,
                            lock: tour.isActive,
                            estimatedDurationMinutes: tour.estimatedDurationMinutes,
                            sortPriority: tour.sortPriority,
                            isFeatured: tour.isFeatured,
                          })
                        }
                        className={`rounded-md p-1.5 transition-colors hover:bg-muted ${
                          !tour.isActive ? "text-destructive" : "text-muted-foreground"
                        }`}
                        title={tour.isActive ? "Ngưng hoạt động tour" : "Kích hoạt tour"}
                      >
                        {tour.isActive ? <Unlock className="h-4 w-4" /> : <Lock className="h-4 w-4" />}
                      </button>
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
                <div className="mt-4 grid gap-5 md:grid-cols-3">
                  <div>
                    <Label className="text-xs">Thời gian dự kiến (phút)</Label>
                    <Input
                      type="number"
                      min={0}
                      value={draftEstimatedDurationMinutes}
                      onChange={(e) => setDraftEstimatedDurationMinutes(e.target.value)}
                      className="mt-1"
                      placeholder="Để trống nếu không đặt"
                    />
                  </div>
                  <div>
                    <Label className="text-xs">Ưu tiên</Label>
                    <Input
                      type="number"
                      min={0}
                      value={draftSortPriority}
                      onChange={(e) => setDraftSortPriority(e.target.value)}
                      className="mt-1"
                    />
                  </div>
                  <div>
                    <Label className="text-xs">Nổi bật</Label>
                    <select
                      value={draftIsFeatured ? "true" : "false"}
                      onChange={(e) => setDraftIsFeatured(e.target.value === "true")}
                      className="mt-1 h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
                    >
                      <option value="true">Có</option>
                      <option value="false">Không</option>
                    </select>
                  </div>
                </div>
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
                    onClick={handleSaveChanges}
                    disabled={!hasUnsavedChanges || saveChangesMutation.isPending}
                  >
                    {saveChangesMutation.isPending ? "Đang lưu..." : "Lưu cập nhật"}
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
                      hasUnsavedChanges
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

      <Dialog open={createOpen} onOpenChange={setCreateOpen}>
        <DialogContent className="max-w-3xl">
          <DialogHeader>
            <DialogTitle>Thêm tour mới</DialogTitle>
          </DialogHeader>

          <div className="grid gap-4">
            <div>
              <Label htmlFor="create-tour-name">Tên tour</Label>
              <Input
                id="create-tour-name"
                value={createName}
                onChange={(e) => setCreateName(e.target.value)}
                placeholder="Nhập tên tour"
                className="mt-1"
              />
            </div>

            <div>
              <Label htmlFor="create-tour-short-description">Mô tả ngắn</Label>
              <Input
                id="create-tour-short-description"
                value={createShortDescription}
                onChange={(e) => setCreateShortDescription(e.target.value)}
                placeholder="Mô tả ngắn (không bắt buộc)"
                className="mt-1"
              />
            </div>

            <div>
              <Label htmlFor="create-tour-description">Mô tả chi tiết</Label>
              <Textarea
                id="create-tour-description"
                value={createDescription}
                onChange={(e) => setCreateDescription(e.target.value)}
                placeholder="Mô tả chi tiết (không bắt buộc)"
                className="mt-1"
                rows={4}
              />
            </div>

            <div className="grid gap-5 md:grid-cols-2 xl:grid-cols-3">
              <div>
                <Label htmlFor="create-tour-estimated-duration" className="whitespace-nowrap">
                  Thời gian dự kiến (phút)
                </Label>
                <Input
                  id="create-tour-estimated-duration"
                  type="number"
                  min={0}
                  value={createEstimatedDurationMinutes}
                  onChange={(e) => setCreateEstimatedDurationMinutes(e.target.value)}
                  placeholder="Để trống nếu chưa đặt"
                  className="mt-1 min-w-0"
                />
              </div>

              <div>
                <Label htmlFor="create-tour-priority" className="whitespace-nowrap">
                  Ưu tiên
                </Label>
                <Input
                  id="create-tour-priority"
                  type="number"
                  min={0}
                  value={createSortPriority}
                  onChange={(e) => setCreateSortPriority(e.target.value)}
                  className="mt-1 min-w-0"
                />
              </div>

              <div>
                <Label htmlFor="create-tour-featured" className="whitespace-nowrap">
                  Nổi bật
                </Label>
                <select
                  id="create-tour-featured"
                  value={createIsFeatured ? "true" : "false"}
                  onChange={(e) => setCreateIsFeatured(e.target.value === "true")}
                  className="mt-1 h-10 w-full min-w-0 rounded-md border border-input bg-background px-3 py-2 pr-10 text-sm"
                >
                  <option value="false">Không</option>
                  <option value="true">Có</option>
                </select>
              </div>

            </div>

            <div className="flex justify-end gap-2">
              <Button
                variant="outline"
                onClick={() => setCreateOpen(false)}
                disabled={createTourMutation.isPending}
              >
                Hủy
              </Button>
              <Button onClick={handleCreateTour} disabled={createTourMutation.isPending}>
                {createTourMutation.isPending ? "Đang tạo..." : "Tạo tour"}
              </Button>
            </div>
          </div>
        </DialogContent>
      </Dialog>

      <ConfirmDialog
        open={!!confirmTour}
        onOpenChange={(open) => !open && setConfirmTour(null)}
        title={confirmTour?.lock ? "Ngưng hoạt động tour" : "Kích hoạt tour"}
        description={
          confirmTour?.lock
            ? "Tour sẽ bị ngưng hoạt động. Bạn có chắc không?"
            : "Tour sẽ được kích hoạt hoạt động trở lại."
        }
        onConfirm={() => {
          if (!confirmTour) return;
          statusMutation.mutate({
            id: confirmTour.id,
            isActive: !confirmTour.lock,
            estimatedDurationMinutes: confirmTour.estimatedDurationMinutes,
            sortPriority: confirmTour.sortPriority,
            isFeatured: confirmTour.isFeatured,
          });
        }}
        variant={confirmTour?.lock ? "destructive" : "default"}
      />
    </AdminLayout>
  );
};

export default ToursPage;
