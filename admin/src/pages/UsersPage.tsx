import { useState } from "react";
import AdminLayout from "@/components/AdminLayout";
import { Plus, Lock, Unlock, Shield } from "lucide-react";
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { toast } from "sonner";
import ConfirmDialog from "@/components/ConfirmDialog";

interface User {
  user_id: number;
  username: string;
  role: "admin" | "editor";
  is_active: boolean;
  created_at: string;
}

const UsersPage = () => {
  const [data, setData] = useState<User[]>([]);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [form, setForm] = useState<{ username: string; role: "admin" | "editor" }>({ username: "", role: "editor" });
  const [confirmUser, setConfirmUser] = useState<{ id: number; name: string; lock: boolean } | null>(null);

  const handleConfirmToggle = () => {
    if (!confirmUser) return;
    setData((arr) => arr.map((u) => (u.user_id === confirmUser.id ? { ...u, is_active: !u.is_active } : u)));
    toast.success("Thao tác thành công");
    setConfirmUser(null);
  };

  const changeRole = (id: number, role: "admin" | "editor") => {
    setData((arr) => arr.map((u) => (u.user_id === id ? { ...u, role } : u)));
    toast.success("Thao tác thành công");
  };

  const handleCreate = () => {
    if (!form.username) { toast.error("Có lỗi xảy ra"); return; }
    const newU: User = {
      user_id: Date.now(),
      username: form.username,
      role: form.role,
      is_active: true,
      created_at: new Date().toISOString().split("T")[0],
    };
    setData((arr) => [...arr, newU]);
    setDialogOpen(false);
    toast.success("Thao tác thành công");
  };

  return (
    <AdminLayout>
      <div className="page-header">
        <h1 className="page-title">Quản lý người dùng</h1>
        <Button onClick={() => { setForm({ username: "", role: "editor" }); setDialogOpen(true); }} size="sm">
          <Plus className="h-4 w-4 mr-1.5" />Tạo người dùng
        </Button>
      </div>
      <div className="max-w-7xl mx-auto px-8 py-6">
        <div className="stat-card">
          <table className="data-table">
            <thead>
              <tr>
                <th>Tên đăng nhập</th>
                <th>Vai trò</th>
                <th>Trạng thái</th>
                <th>Ngày tạo</th>
                <th className="w-32">Hành động</th>
              </tr>
            </thead>
            <tbody>
              {data.map((u) => (
                <tr key={u.user_id}>
                  <td className="font-medium">{u.username}</td>
                  <td>
                    <span className={`inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium ${u.role === "admin" ? "bg-primary/10 text-primary" : "bg-muted text-muted-foreground"}`}>
                      <Shield className="h-3 w-3" />{u.role === "admin" ? "Quản trị viên" : "Biên tập viên"}
                    </span>
                  </td>
                  <td>
                    <span className={u.is_active ? "status-active" : "status-inactive"}>
                      {u.is_active ? "Hoạt động" : "Ngừng hoạt động"}
                    </span>
                  </td>
                  <td className="mono text-xs text-muted-foreground">{u.created_at}</td>
                  <td className="flex items-center gap-1">
                    <button
                      onClick={() => setConfirmUser({ id: u.user_id, name: u.username, lock: u.is_active })}
                      className={`p-1.5 rounded-md hover:bg-muted transition-colors ${!u.is_active ? "text-destructive" : "text-muted-foreground"}`}
                      title={u.is_active ? "Khóa người dùng" : "Mở khóa người dùng"}
                    >
                      {u.is_active ? <Unlock className="h-4 w-4" /> : <Lock className="h-4 w-4" />}
                    </button>
                    <Select value={u.role} onValueChange={(v) => changeRole(u.user_id, v as "admin" | "editor")}>
                      <SelectTrigger className="h-7 w-24 text-xs">
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="admin">Quản trị viên</SelectItem>
                        <SelectItem value="editor">Biên tập viên</SelectItem>
                      </SelectContent>
                    </Select>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      <Dialog open={dialogOpen} onOpenChange={setDialogOpen}>
        <DialogContent className="max-w-sm">
          <DialogHeader><DialogTitle>Tạo người dùng mới</DialogTitle></DialogHeader>
          <div className="grid gap-3 py-2">
            <div>
              <Label className="text-xs">Tên đăng nhập</Label>
              <Input value={form.username} onChange={(e) => setForm({ ...form, username: e.target.value })} className="mt-1" />
            </div>
            <div>
              <Label className="text-xs">Vai trò</Label>
              <Select value={form.role} onValueChange={(v) => setForm({ ...form, role: v as "admin" | "editor" })}>
                <SelectTrigger className="mt-1"><SelectValue /></SelectTrigger>
                <SelectContent>
                  <SelectItem value="admin">Quản trị viên</SelectItem>
                  <SelectItem value="editor">Biên tập viên</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <Button onClick={handleCreate} className="mt-2">Tạo mới</Button>
          </div>
        </DialogContent>
      </Dialog>

      <ConfirmDialog
        open={!!confirmUser}
        onOpenChange={(open) => !open && setConfirmUser(null)}
        title={confirmUser?.lock ? "Khóa người dùng" : "Mở khóa người dùng"}
        description={confirmUser?.lock
          ? "Bạn có chắc chắn muốn khóa người dùng này không? Hành động này có thể ảnh hưởng đến dữ liệu và người dùng."
          : "Bạn có chắc chắn muốn mở khóa người dùng này không?"
        }
        onConfirm={handleConfirmToggle}
        variant={confirmUser?.lock ? "destructive" : "default"}
      />
    </AdminLayout>
  );
};

export default UsersPage;
