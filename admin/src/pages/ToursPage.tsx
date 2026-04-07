import { useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Eye, Plus } from "lucide-react";
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

  const availableRestaurants = useMemo(() => {
    const stopIds = new Set(selectedTour?.stops.map((s) => s.restaurantId) ?? []);
    return restaurants
      .filter((r) => r.isActive && !stopIds.has(r.restaurantId))
      .sort((a, b) => a.name.localeCompare(b.name));
  }, [restaurants, selectedTour]);

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
      toast.success("Them nha hang vao tour thanh cong");
    },
    onError: (err: Error) => {
      toast.error(err.message ?? "Them nha hang vao tour that bai");
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
    }
  };

  const handleAddRestaurant = () => {
    if (selectedTourId === null) return;

    const restaurantId = addRestaurantId.trim();
    if (!restaurantId) {
      toast.error("Vui long chon nha hang");
      return;
    }

    addRestaurantMutation.mutate({
      tourId: selectedTourId,
      restaurantId,
    });
  };

  return (
    <AdminLayout>
      <div className="page-header">
        <h1 className="page-title">Quan ly tour</h1>
      </div>

      <div className="mx-auto max-w-7xl px-8 py-6">
        <div className="stat-card">
          <table className="data-table">
            <thead>
              <tr>
                <th>Ten tour</th>
                <th className="w-36">So diem dung</th>
                <th className="w-40">Thoi gian du kien</th>
                <th className="w-28">Uu tien</th>
                <th className="w-24">Noi bat</th>
                <th className="w-24">Hanh dong</th>
              </tr>
            </thead>
            <tbody>
              {isLoading && (
                <tr>
                  <td colSpan={6} className="py-8 text-center text-muted-foreground">
                    Dang tai danh sach tour...
                  </td>
                </tr>
              )}
              {isError && (
                <tr>
                  <td colSpan={6} className="py-8 text-center text-destructive">
                    Khong the tai danh sach tour. Vui long thu lai.
                  </td>
                </tr>
              )}
              {!isLoading && !isError && tours.length === 0 && (
                <tr>
                  <td colSpan={6} className="py-8 text-center text-muted-foreground">
                    Chua co tour nao.
                  </td>
                </tr>
              )}
              {!isLoading &&
                !isError &&
                tours.map((tour) => (
                  <tr key={tour.tourId}>
                    <td className="font-medium">{tour.name}</td>
                    <td>{tour.stopCount}</td>
                    <td>{tour.estimatedDurationMinutes ? `${tour.estimatedDurationMinutes} phut` : "-"}</td>
                    <td>{tour.sortPriority}</td>
                    <td>{tour.isFeatured ? "Co" : "Khong"}</td>
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
            <DialogTitle>Chi tiet tour</DialogTitle>
          </DialogHeader>

          {isDetailLoading && (
            <p className="text-sm text-muted-foreground">Dang tai chi tiet tour...</p>
          )}

          {isDetailError && (
            <p className="text-sm text-destructive">Khong the tai chi tiet tour. Vui long thu lai.</p>
          )}

          {!isDetailLoading && !isDetailError && selectedTour && (
            <div className="space-y-5">
              <div className="rounded-md border p-4">
                <p className="text-sm text-muted-foreground">Tour</p>
                <p className="mt-1 text-base font-semibold">{selectedTour.name}</p>
                <p className="mt-1 text-sm text-muted-foreground">
                  Tong so diem dung: {selectedTour.stopCount}
                </p>
              </div>

              <div className="rounded-md border p-4">
                <div className="mb-3 flex items-center justify-between">
                  <h3 className="text-sm font-semibold">Danh sach nha hang theo stop_order</h3>
                </div>

                <table className="data-table">
                  <thead>
                    <tr>
                      <th className="w-28">Stop order</th>
                      <th className="w-64">Restaurant ID</th>
                      <th>Ten nha hang</th>
                      <th>Dia chi</th>
                    </tr>
                  </thead>
                  <tbody>
                    {selectedTour.stops.length === 0 && (
                      <tr>
                        <td colSpan={4} className="py-6 text-center text-muted-foreground">
                          Tour nay chua co nha hang nao.
                        </td>
                      </tr>
                    )}
                    {selectedTour.stops.map((stop) => (
                      <tr key={stop.restaurantId}>
                        <td>{stop.stopOrder}</td>
                        <td className="mono text-xs">{stop.restaurantId}</td>
                        <td className="font-medium">{stop.restaurantName}</td>
                        <td className="text-xs text-muted-foreground">{stop.address || "-"}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              <div className="rounded-md border p-4">
                <h3 className="mb-3 text-sm font-semibold">Them nha hang vao tour</h3>

                <div className="grid gap-3">
                  <div>
                    <Label className="text-xs">Nha hang</Label>
                    <select
                      value={addRestaurantId}
                      onChange={(e) => setAddRestaurantId(e.target.value)}
                      className="mt-1 h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
                    >
                      <option value="">Chon nha hang</option>
                      {availableRestaurants.map((restaurant) => (
                        <option key={restaurant.restaurantId} value={restaurant.restaurantId}>
                          {restaurant.name} ({restaurant.restaurantId})
                        </option>
                      ))}
                    </select>
                  </div>
                </div>
                <p className="mt-2 text-xs text-muted-foreground">
                  Stop order se tu dong la: {getNextStopOrder(selectedTour)}
                </p>

                <div className="mt-4">
                  <Button
                    onClick={handleAddRestaurant}
                    disabled={addRestaurantMutation.isPending || availableRestaurants.length === 0}
                    className="gap-2"
                  >
                    <Plus className="h-4 w-4" />
                    {addRestaurantMutation.isPending ? "Dang them..." : "Them nha hang"}
                  </Button>
                  {availableRestaurants.length === 0 && (
                    <p className="mt-2 text-xs text-muted-foreground">
                      Khong con nha hang nao de them vao tour nay.
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
