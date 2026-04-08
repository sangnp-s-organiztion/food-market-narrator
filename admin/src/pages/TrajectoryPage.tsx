import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import AdminLayout from "@/components/AdminLayout";
import TrajectorySection from "@/components/TrajectorySection";
import { analyticsApi } from "@/lib/analyticsApi";

const TrajectoryPage = () => {
  const [sessionLimit] = useState<20 | 50 | 100 | 200 | "all">(100);

  const { data: movementPathsData } = useQuery({
    queryKey: ["analytics", "movement-paths", sessionLimit],
    queryFn: () => analyticsApi.getMovementPaths(sessionLimit),
    staleTime: 60_000,
  });

  return (
    <AdminLayout>
      <div className="page-header">
        <div>
          <h1 className="page-title">Tuyến di chuyển người dùng</h1>
          <p className="mt-0.5 text-sm text-muted-foreground">
            Theo dõi đường đi ẩn danh theo session để phân tích hành vi nghe audio
          </p>
        </div>
      </div>

      <div className="mx-auto max-w-7xl space-y-6 px-8 py-6">
        <TrajectorySection movementPaths={movementPathsData?.sessions} />
      </div>
    </AdminLayout>
  );
};

export default TrajectoryPage;
