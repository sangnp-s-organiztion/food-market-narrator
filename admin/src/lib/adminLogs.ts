import { ActivityLog } from "./mockData";

let nextId = 100;
const logs: ActivityLog[] = [];

export const addLog = (action: string, target: string, target_name: string) => {
  const log: ActivityLog = {
    id: nextId++,
    user: "admin",
    action,
    target,
    target_name,
    timestamp: new Date().toISOString().replace("T", " ").slice(0, 19),
  };
  logs.unshift(log);
  return log;
};

export const getSessionLogs = () => logs;
