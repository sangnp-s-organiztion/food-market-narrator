import { useState, useEffect } from "react";
import { getRestaurantAudios, mockLanguages } from "@/services/mockData";
import { useRestaurant } from "@/contexts/RestaurantContext";
import type { Audio } from "@/types";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Switch } from "@/components/ui/switch";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from "@/components/ui/dialog";
import { toast } from "sonner";
import { Plus, Volume2, Trash2 } from "lucide-react";

export default function AudioPage() {
  const { selectedRestaurant } = useRestaurant();
  const [audios, setAudios] = useState<Audio[]>([]);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [selectedLang, setSelectedLang] = useState<string>("");

  useEffect(() => {
    if (selectedRestaurant) {
      setAudios(getRestaurantAudios(selectedRestaurant.restaurant_id));
    }
  }, [selectedRestaurant]);

  const toggleActive = (id: number) => {
    setAudios((prev) =>
      prev.map((a) => (a.audio_id === id ? { ...a, is_active: !a.is_active } : a))
    );
    toast.success("Đã cập nhật trạng thái âm thanh");
  };

  const deleteAudio = (id: number) => {
    setAudios((prev) => prev.filter((a) => a.audio_id !== id));
    toast.success("Đã xóa âm thanh");
  };

  const handleUpload = () => {
    if (!selectedLang) {
      toast.error("Vui lòng chọn ngôn ngữ");
      return;
    }
    const langId = parseInt(selectedLang);
    const existing = audios.filter((a) => a.language_id === langId);
    const newAudio: Audio = {
      audio_id: Date.now(),
      restaurant_id: selectedRestaurant!.restaurant_id,
      language_id: langId,
      audio_url: `/audio/new-${Date.now()}.mp3`,
      version: existing.length + 1,
      is_active: true,
      date_generation: new Date().toISOString(),
    };
    setAudios((prev) => [...prev, newAudio]);
    setDialogOpen(false);
    setSelectedLang("");
    toast.success("Đã tải âm thanh lên (demo)");
  };

  const getLangName = (id: number) => mockLanguages.find((l) => l.language_id === id)?.name ?? "Không rõ";

  return (
    <div className="max-w-3xl mx-auto animate-fade-in">
      <div className="page-header flex items-start justify-between">
        <div>
          <h1 className="page-title">Mô tả âm thanh</h1>
          <p className="page-description">Quản lý mô tả âm thanh theo các ngôn ngữ khác nhau</p>
        </div>
        <Button onClick={() => setDialogOpen(true)}>
          <Plus className="w-4 h-4 mr-2" /> Tải lên âm thanh
        </Button>
      </div>

      {audios.length === 0 ? (
        <div className="form-section text-center py-12">
          <p className="text-muted-foreground">Chưa có tệp âm thanh nào. Tải lên mô tả âm thanh đầu tiên.</p>
        </div>
      ) : (
        <div className="space-y-3">
          {audios.map((audio) => (
            <div key={audio.audio_id} className="dashboard-card flex items-center gap-4">
              <div className="flex items-center justify-center w-10 h-10 rounded-lg bg-accent shrink-0">
                <Volume2 className="w-5 h-5 text-accent-foreground" />
              </div>
              <div className="flex-1 min-w-0">
                <h3 className="font-medium text-foreground">{getLangName(audio.language_id)}</h3>
                <p className="text-sm text-muted-foreground">
                  Phiên bản {audio.version} · {new Date(audio.date_generation).toLocaleDateString("vi-VN")}
                </p>
              </div>
              <div className="flex items-center gap-3 shrink-0">
                <div className="flex items-center gap-2">
                  <span className={`text-xs font-medium ${audio.is_active ? "text-success" : "text-muted-foreground"}`}>
                    {audio.is_active ? "Hoạt động" : "Không hoạt động"}
                  </span>
                  <Switch checked={audio.is_active} onCheckedChange={() => toggleActive(audio.audio_id)} />
                </div>
                <Button variant="ghost" size="icon" onClick={() => deleteAudio(audio.audio_id)}>
                  <Trash2 className="w-4 h-4 text-destructive" />
                </Button>
              </div>
            </div>
          ))}
        </div>
      )}

      <Dialog open={dialogOpen} onOpenChange={setDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Tải lên mô tả âm thanh</DialogTitle>
          </DialogHeader>
          <div className="space-y-4 py-2">
            <div className="space-y-2">
              <Label>Ngôn ngữ</Label>
              <Select value={selectedLang} onValueChange={setSelectedLang}>
                <SelectTrigger>
                  <SelectValue placeholder="Chọn ngôn ngữ" />
                </SelectTrigger>
                <SelectContent>
                  {mockLanguages.map((lang) => (
                    <SelectItem key={lang.language_id} value={String(lang.language_id)}>
                      {lang.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2">
              <Label>Tệp âm thanh</Label>
              <div className="border-2 border-dashed rounded-lg p-8 text-center text-muted-foreground cursor-pointer hover:border-primary/50 transition-colors">
                <Volume2 className="w-8 h-8 mx-auto mb-2 opacity-50" />
                <p className="text-sm">Nhấp để chọn tệp âm thanh</p>
                <p className="text-xs mt-1">MP3, WAV tối đa 10MB</p>
              </div>
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setDialogOpen(false)}>Hủy</Button>
            <Button onClick={handleUpload}>Tải lên</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
