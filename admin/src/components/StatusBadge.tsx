import { EntityStatus } from "@/lib/mockData";

const statusLabels: Record<string, string> = {
  active: "Hoạt động",
  inactive: "Ngừng hoạt động",
};

const statusClasses: Record<string, string> = {
  active: "status-active",
  inactive: "status-inactive",
  banned: "status-banned",
};

const StatusBadge = ({ status }: { status: EntityStatus }) => (
  <span className={statusClasses[status] || "status-inactive"}>{statusLabels[status] || status}</span>
);

export default StatusBadge;
