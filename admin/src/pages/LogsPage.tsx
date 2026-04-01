import { useState, useEffect } from "react";
import AdminLayout from "@/components/AdminLayout";
import { activityLogs } from "@/lib/mockData";
import { getSessionLogs } from "@/lib/adminLogs";

const actionColors: Record<string, string> = {
  LOCK: "bg-amber-100 text-amber-700",
  UNLOCK: "bg-emerald-100 text-emerald-700",
  BAN: "bg-red-100 text-red-700",
  UPDATE: "bg-blue-100 text-blue-700",
  DISABLE: "bg-red-100 text-red-700",
  ENABLE: "bg-emerald-100 text-emerald-700",
};

const LogsPage = () => {
  const [, setTick] = useState(0);

  // Re-render periodically to pick up new session logs
  useEffect(() => {
    const interval = setInterval(() => setTick((t) => t + 1), 2000);
    return () => clearInterval(interval);
  }, []);

  const allLogs = [...getSessionLogs(), ...activityLogs];

  return (
    <AdminLayout>
      <div className="page-header">
        <h1 className="page-title">Nhật ký hoạt động</h1>
      </div>
      <div className="max-w-7xl mx-auto px-8 py-6">
        <div className="stat-card">
          <table className="data-table">
            <thead>
              <tr>
                <th>Quản trị viên</th>
                <th>Hành động</th>
                <th>Đối tượng</th>
                <th>Tên</th>
                <th>Thời gian</th>
              </tr>
            </thead>
            <tbody>
              {allLogs.map((log) => (
                <tr key={log.id}>
                  <td className="font-medium text-xs">{log.user}</td>
                  <td>
                    <span className={`inline-block px-2 py-0.5 rounded-full text-xs font-medium ${actionColors[log.action] || "bg-muted text-muted-foreground"}`}>
                      {log.action}
                    </span>
                  </td>
                  <td className="text-xs mono">{log.target}</td>
                  <td className="text-xs">{log.target_name}</td>
                  <td className="text-xs mono text-muted-foreground">{log.timestamp}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </AdminLayout>
  );
};

export default LogsPage;
