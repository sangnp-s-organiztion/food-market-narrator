import { useEffect, useMemo, useRef, useState, type ChangeEvent } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Eye, GripVertical, Lock, Plus, Scissors, Unlock, Upload } from "lucide-react";
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

  if (normalized.startsWith("maui-images/") || normalized.startsWith("uploads/")) {
    return new URL(`/${normalized}`, API_BASE).toString();
  }

  return new URL(`/maui-images/${normalized}`, API_BASE).toString();
}

function normalizeImageInput(value: string): string | null {
  const normalized = value.trim();
  return normalized.length > 0 ? normalized : null;
}

function readFileAsDataUrl(file: File): Promise<string> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => resolve(reader.result as string);
    reader.onerror = () => reject(new Error("Không thể đọc file ảnh"));
    reader.readAsDataURL(file);
  });
}

function getFallbackStopImage(tour: TourResponse | undefined): string | null {
  if (!tour) return null;

  return (
    tour.stops
      .map((stop) => stop.primaryImageUrl)
      .find((url): url is string => !!url && url.trim().length > 0) ?? null
  );
}

function getNextStopOrder(tour: TourResponse | undefined): number {
  if (!tour || tour.stops.length === 0) return 1;
  const maxStop = Math.max(...tour.stops.map((s) => s.stopOrder));
  return maxStop + 1;
}

const ToursPage = () => {
  const qc = useQueryClient();
  const [createOpen, setCreateOpen] = useState(false);
  const [detailOpen, setDetailOpen] = useState(false);
  const [detailMode, setDetailMode] = useState<"view" | "edit">("view");
  const [selectedTourId, setSelectedTourId] = useState<number | null>(null);
  const [addRestaurantId, setAddRestaurantId] = useState("");
  const [confirmTour, setConfirmTour] = useState<{
    id: number;
    name: string;
    lock: boolean;
    estimatedDurationMinutes: number | null;
    imageUrl: string | null;
    sortPriority: number;
    isFeatured: boolean;
  } | null>(null);
  const [draftStops, setDraftStops] = useState<TourStopResponse[]>([]);
  const [draggingRestaurantId, setDraggingRestaurantId] = useState<string | null>(null);
  const [draftEstimatedDurationMinutes, setDraftEstimatedDurationMinutes] = useState("");
  const [draftImageUrl, setDraftImageUrl] = useState("");
  const [draftImageFile, setDraftImageFile] = useState<File | null>(null);
  const [draftImagePreview, setDraftImagePreview] = useState<string | null>(null);
  const [draftSortPriority, setDraftSortPriority] = useState("");
  const [draftIsFeatured, setDraftIsFeatured] = useState(false);
  const [createName, setCreateName] = useState("");
  const [createShortDescription, setCreateShortDescription] = useState("");
  const [createDescription, setCreateDescription] = useState("");
  const [createEstimatedDurationMinutes, setCreateEstimatedDurationMinutes] = useState("");
  const [createImageUrl, setCreateImageUrl] = useState("");
  const [createImageFile, setCreateImageFile] = useState<File | null>(null);
  const [createImagePreview, setCreateImagePreview] = useState<string | null>(null);
  const [createSortPriority, setCreateSortPriority] = useState("0");
  const [createIsFeatured, setCreateIsFeatured] = useState(false);
  const detailImageInputRef = useRef<HTMLInputElement | null>(null);
  const createImageInputRef = useRef<HTMLInputElement | null>(null);

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
      setDraftImageUrl("");
      setDraftImageFile(null);
      setDraftImagePreview(null);
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
    setDraftImageUrl(selectedTour.imageUrl ?? "");
    setDraftImageFile(null);
    setDraftImagePreview(null);
    if (detailImageInputRef.current) {
      detailImageInputRef.current.value = "";
    }
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
      normalizeImageInput(draftImageUrl) !== normalizeImageInput(selectedTour.imageUrl ?? "") ||
      draftImageFile !== null ||
      sortPriority !== selectedTour.sortPriority ||
      draftIsFeatured !== selectedTour.isFeatured
    ) {
      return true;
    }

    return false;
  }, [
    selectedTour,
    draftEstimatedDurationMinutes,
    draftImageUrl,
    draftImageFile,
    draftSortPriority,
    draftIsFeatured,
  ]);

  const hasUnsavedChanges = hasOrderChanges || hasMetaChanges;
  const isDetailEditMode = detailMode === "edit";
  const detailPreviewImageUrl =
    draftImagePreview ??
    normalizeImageUrl(normalizeImageInput(draftImageUrl) ?? getFallbackStopImage(selectedTour));
  const createPreviewImageUrl =
    createImagePreview ?? normalizeImageUrl(normalizeImageInput(createImageUrl));

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
      imageUrl: string | null;
      imageFile: File | null;
      sortPriority: number;
      isActive: boolean;
      isFeatured: boolean;
    }) =>
      (async () => {
        const created = await tourApi.create({
        name: payload.name,
        shortDescription: payload.shortDescription,
        description: payload.description,
        estimatedDurationMinutes: payload.estimatedDurationMinutes,
        imageUrl: payload.imageUrl,
        sortPriority: payload.sortPriority,
        isActive: payload.isActive,
        isFeatured: payload.isFeatured,
        });

        if (payload.imageFile) {
          const upload = await tourApi.uploadImageForTour(created.tourId, payload.imageFile);
          return {
            ...created,
            imageUrl: upload.imageUrl,
          };
        }

        return created;
      })(),
    onSuccess: async () => {
      await qc.invalidateQueries({ queryKey: ["admin", "tours"] });
      setCreateOpen(false);
      setCreateName("");
      setCreateShortDescription("");
      setCreateDescription("");
      setCreateEstimatedDurationMinutes("");
      setCreateImageUrl("");
      setCreateImageFile(null);
      setCreateImagePreview(null);
      if (createImageInputRef.current) {
        createImageInputRef.current.value = "";
      }
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
      imageUrl: string | null;
      sortPriority: number;
      isFeatured: boolean;
    }) =>
      tourApi.update(payload.id, {
        estimatedDurationMinutes: payload.estimatedDurationMinutes,
        imageUrl: payload.imageUrl,
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
      imageUrl: string | null;
      imageFile: File | null;
      sortPriority: number;
      isActive: boolean;
      isFeatured: boolean;
      hasOrderChanges: boolean;
      hasMetaChanges: boolean;
    }) => {
      let imageUrl = payload.imageUrl;
      if (payload.imageFile) {
        const upload = await tourApi.uploadImageForTour(payload.tourId, payload.imageFile);
        imageUrl = upload.imageUrl;
      }

      if (payload.hasOrderChanges) {
        await tourApi.reorderStops(payload.tourId, { restaurantIds: payload.restaurantIds });
      }

      if (payload.hasMetaChanges) {
        await tourApi.update(payload.tourId, {
          estimatedDurationMinutes: payload.estimatedDurationMinutes,
          imageUrl,
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

      setDraftImageFile(null);
      setDraftImagePreview(null);
      if (detailImageInputRef.current) {
        detailImageInputRef.current.value = "";
      }
      toast.success("Đã lưu cập nhật tour");
    },
    onError: (err: Error) => {
      toast.error(err.message ?? "Lưu cập nhật thất bại");
    },
  });

  const handleOpenDetail = (tourId: number, mode: "view" | "edit") => {
    setSelectedTourId(tourId);
    setAddRestaurantId("");
    setDetailMode(mode);
    setDetailOpen(true);
  };

  const handleDialogOpenChange = (open: boolean) => {
    setDetailOpen(open);
    if (!open) {
      setSelectedTourId(null);
      setAddRestaurantId("");
      setDetailMode("view");
      setDraftStops([]);
      setDraggingRestaurantId(null);
      setDraftEstimatedDurationMinutes("");
      setDraftImageUrl("");
      setDraftImageFile(null);
      setDraftImagePreview(null);
      if (detailImageInputRef.current) {
        detailImageInputRef.current.value = "";
      }
      setDraftSortPriority("");
      setDraftIsFeatured(false);
    }
  };

  const handleDraftImageFileChange = async (e: ChangeEvent<HTMLInputElement>) => {
    if (!isDetailEditMode) return;

    const file = e.target.files?.[0] ?? null;
    setDraftImageFile(file);

    if (!file) {
      setDraftImagePreview(null);
      return;
    }

    try {
      const preview = await readFileAsDataUrl(file);
      setDraftImagePreview(preview);
    } catch (error) {
      setDraftImageFile(null);
      setDraftImagePreview(null);
      if (detailImageInputRef.current) {
        detailImageInputRef.current.value = "";
      }
      toast.error((error as Error).message ?? "Không thể đọc file ảnh");
    }
  };

  const handleCreateImageFileChange = async (e: ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0] ?? null;
    setCreateImageFile(file);

    if (!file) {
      setCreateImagePreview(null);
      return;
    }

    try {
      const preview = await readFileAsDataUrl(file);
      setCreateImagePreview(preview);
    } catch (error) {
      setCreateImageFile(null);
      setCreateImagePreview(null);
      if (createImageInputRef.current) {
        createImageInputRef.current.value = "";
      }
      toast.error((error as Error).message ?? "Không thể đọc file ảnh");
    }
  };

  const handleAddRestaurant = () => {
    if (!isDetailEditMode) return;
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
    if (!isDetailEditMode) return;
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
    if (!isDetailEditMode) return;
    if (selectedTourId === null || !hasUnsavedChanges) return;

    const estimatedDuration =
      draftEstimatedDurationMinutes.trim().length === 0
        ? null
        : Number(draftEstimatedDurationMinutes);
    const imageUrl = normalizeImageInput(draftImageUrl);
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
      imageUrl,
      imageFile: draftImageFile,
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
    const imageUrl = normalizeImageInput(createImageUrl);
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
      imageUrl,
      imageFile: createImageFile,
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
                <th className="w-32">Ảnh tour</th>
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
                  <td colSpan={8} className="py-8 text-center text-muted-foreground">
                    Đang tải danh sách tour...
                  </td>
                </tr>
              )}
              {isError && (
                <tr>
                  <td colSpan={8} className="py-8 text-center text-destructive">
                    Không thể tải danh sách tour. Vui lòng thử lại.
                  </td>
                </tr>
              )}
              {!isLoading && !isError && tours.length === 0 && (
                <tr>
                  <td colSpan={8} className="py-8 text-center text-muted-foreground">
                    Chưa có tour nào.
                  </td>
                </tr>
              )}
              {!isLoading &&
                !isError &&
                tours.map((tour) => (
                  <tr key={tour.tourId}>
                    <td className="font-medium">{tour.name}</td>
                    <td>
                      {(() => {
                        const previewUrl = normalizeImageUrl(
                          tour.imageUrl ?? getFallbackStopImage(tour),
                        );
                        if (!previewUrl) {
                          return <span className="text-xs text-muted-foreground">Chưa có ảnh</span>;
                        }

                        return (
                          <img
                            src={previewUrl}
                            alt={`Ảnh tour ${tour.name}`}
                            className="h-12 w-20 rounded-md border object-cover"
                          />
                        );
                      })()}
                    </td>
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
                        onClick={() => handleOpenDetail(tour.tourId, "view")}
                        title="Xem tour"
                      >
                        <Eye className="h-4 w-4" />
                      </Button>
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={() => handleOpenDetail(tour.tourId, "edit")}
                        title="Chỉnh sửa tour"
                      >
                        <Scissors className="h-4 w-4" />
                      </Button>
                      <button
                        disabled={statusMutation.isPending}
                        onClick={() =>
                          setConfirmTour({
                            id: tour.tourId,
                            name: tour.name,
                            lock: tour.isActive,
                            estimatedDurationMinutes: tour.estimatedDurationMinutes,
                            imageUrl: tour.imageUrl,
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
                <p className="text-sm text-muted-foreground">Tên tour</p>
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
                      disabled={!isDetailEditMode}
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
                      disabled={!isDetailEditMode}
                      className="mt-1"
                    />
                  </div>
                  <div>
                    <Label className="text-xs">Nổi bật</Label>
                    <select
                      value={draftIsFeatured ? "true" : "false"}
                      onChange={(e) => setDraftIsFeatured(e.target.value === "true")}
                      disabled={!isDetailEditMode}
                      className="mt-1 h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
                    >
                      <option value="true">Có</option>
                      <option value="false">Không</option>
                    </select>
                  </div>
                </div>
                <div className="mt-4">
                  <Label className="text-xs">Ảnh tour</Label>
                  <div
                    className={`mt-1 rounded-lg border-2 border-dashed p-4 text-center transition-colors ${
                      isDetailEditMode ? "cursor-pointer hover:border-primary" : "cursor-default"
                    }`}
                    onClick={() => {
                      if (isDetailEditMode) {
                        detailImageInputRef.current?.click();
                      }
                    }}
                  >
                    {detailPreviewImageUrl ? (
                      <div className="space-y-2">
                        <img
                          src={detailPreviewImageUrl}
                          alt={`Ảnh tour ${selectedTour.name}`}
                          className="max-h-44 w-full rounded-md object-contain"
                        />
                        <p className="text-xs text-muted-foreground">
                          {isDetailEditMode ? "Nhấn để chọn ảnh khác" : "Ảnh tour"}
                        </p>
                      </div>
                    ) : (
                      <div className="flex flex-col items-center gap-2 py-2 text-muted-foreground">
                        <Upload className="h-6 w-6" />
                        <p className="text-sm">
                          {isDetailEditMode ? "Nhấn để chọn ảnh" : "Chưa có ảnh"}
                        </p>
                        <p className="text-xs">JPG, PNG, WEBP</p>
                      </div>
                    )}
                    <input
                      ref={detailImageInputRef}
                      type="file"
                      accept="image/jpeg,image/png,image/webp"
                      className="hidden"
                      disabled={!isDetailEditMode}
                      onChange={handleDraftImageFileChange}
                    />
                  </div>
                  {draftImageFile && (
                    <p className="mt-2 text-xs text-muted-foreground">Đã chọn: {draftImageFile.name}</p>
                  )}
                </div>
              </div>

              <div className="rounded-md border p-4">
                <div className="mb-3 flex items-center justify-between">
                  <h3 className="text-sm font-semibold">Danh sách nhà hàng theo thứ tự điểm dừng</h3>
                </div>

                <table className="data-table">
                  <thead>
                    <tr>
                      <th className="w-12"></th>
                      <th className="w-28">Thứ tự điểm dừng</th>
                      <th className="w-64">Mã nhà hàng</th>
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
                        draggable={isDetailEditMode}
                        onDragStart={() => {
                          if (isDetailEditMode) {
                            setDraggingRestaurantId(stop.restaurantId);
                          }
                        }}
                        onDragEnd={() => setDraggingRestaurantId(null)}
                        onDragOver={(e) => {
                          if (isDetailEditMode) {
                            e.preventDefault();
                          }
                        }}
                        onDrop={() => handleDropOnStop(stop.restaurantId)}
                        className={draggingRestaurantId === stop.restaurantId ? "opacity-60" : ""}
                      >
                        <td>
                          <button
                            type="button"
                            className={`text-muted-foreground ${
                              isDetailEditMode ? "hover:text-foreground" : "cursor-default"
                            }`}
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
                {isDetailEditMode && (
                  <div className="mt-4 flex items-center justify-end">
                    <Button
                      onClick={handleSaveChanges}
                      disabled={!hasUnsavedChanges || saveChangesMutation.isPending}
                    >
                      {saveChangesMutation.isPending ? "Đang lưu..." : "Lưu cập nhật"}
                    </Button>
                  </div>
                )}
              </div>

              {isDetailEditMode && (
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
                    Thứ tự điểm dừng sẽ tự động là: {getNextStopOrder(selectedTour)}
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
              )}
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

            <div>
              <Label>Ảnh tour</Label>
              <div
                className="mt-1 cursor-pointer rounded-lg border-2 border-dashed p-4 text-center transition-colors hover:border-primary"
                onClick={() => createImageInputRef.current?.click()}
              >
                {createPreviewImageUrl ? (
                  <div className="space-y-2">
                    <img
                      src={createPreviewImageUrl}
                      alt="Xem trước ảnh tour"
                      className="max-h-44 w-full rounded-md object-contain"
                    />
                    <p className="text-xs text-muted-foreground">Nhấn để chọn ảnh khác</p>
                  </div>
                ) : (
                  <div className="flex flex-col items-center gap-2 py-2 text-muted-foreground">
                    <Upload className="h-6 w-6" />
                    <p className="text-sm">Nhấn để chọn ảnh</p>
                    <p className="text-xs">JPG, PNG, WEBP</p>
                  </div>
                )}
                <input
                  ref={createImageInputRef}
                  type="file"
                  accept="image/jpeg,image/png,image/webp"
                  className="hidden"
                  onChange={handleCreateImageFileChange}
                />
              </div>
              {createImageFile && (
                <p className="mt-2 text-xs text-muted-foreground">Đã chọn: {createImageFile.name}</p>
              )}
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
            imageUrl: confirmTour.imageUrl,
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
