import { useRef, useState, useEffect } from "react";
import {
  deleteAudio,
  getLanguages,
  getRestaurantAudios,
  updateAudioActive,
  uploadRestaurantAudio,
} from "@/services/api";
import { useRestaurant } from "@/contexts/RestaurantContext";
import type { Audio, Language } from "@/types";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Switch } from "@/components/ui/switch";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
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
  const [languages, setLanguages] = useState<Language[]>([]);
  const [loading, setLoading] = useState(false);
  const [uploading, setUploading] = useState(false);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [selectedLang, setSelectedLang] = useState<string>("");
  const fileInputRef = useRef<HTMLInputElement | null>(null);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);

  useEffect(() => {
    const loadAudios = async () => {
      if (!selectedRestaurant) {
        setAudios([]);
        return;
      }

      setLoading(true);
      try {
        const data = await getRestaurantAudios(
          selectedRestaurant.restaurant_id,
        );
        setAudios(data);
      } catch (error) {
        console.error(error);
        toast.error("Không thể tải danh sách âm thanh");
      } finally {
        setLoading(false);
      }
    };

    void loadAudios();
  }, [selectedRestaurant]);

  useEffect(() => {
    const loadLanguages = async () => {
      try {
        const data = await getLanguages();
        setLanguages(data);
      } catch (error) {
        console.error(error);
        toast.error("Không thể tải danh sách ngôn ngữ");
      }
    };

    void loadLanguages();
  }, []);

  const toggleActive = async (id: number) => {
    const target = audios.find((a) => a.audio_id === id);
    if (!target) return;

    try {
      await updateAudioActive(id, !target.is_active);
      setAudios((prev) =>
        prev.map((a) =>
          a.audio_id === id ? { ...a, is_active: !a.is_active } : a,
        ),
      );
      toast.success("Đã cập nhật trạng thái âm thanh");
    } catch (error) {
      console.error(error);
      toast.error("Không thể cập nhật trạng thái âm thanh");
    }
  };

  const removeAudio = async (id: number) => {
    try {
      await deleteAudio(id);
      setAudios((prev) => prev.filter((a) => a.audio_id !== id));
      toast.success("Đã xóa âm thanh");
    } catch (error) {
      console.error(error);
      toast.error("Không thể xóa âm thanh");
    }
  };

  const handleUpload = async () => {
    if (!selectedRestaurant) return;

    if (!selectedLang) {
      toast.error("Vui lòng chọn ngôn ngữ");
      return;
    }

    if (!selectedFile) {
      toast.error("Vui lòng chọn tệp âm thanh");
      return;
    }

    const langId = parseInt(selectedLang, 10);
    setUploading(true);
    try {
      const created = await uploadRestaurantAudio(
        selectedRestaurant.restaurant_id,
        langId,
        selectedFile,
      );
      setAudios((prev) => [...prev, created]);
      setDialogOpen(false);
      setSelectedLang("");
      setSelectedFile(null);
      toast.success("Đã tải âm thanh lên");
    } catch (error) {
      console.error(error);
      toast.error("Không thể tải âm thanh lên");
    } finally {
      setUploading(false);
    }
  };

  const getLangName = (id: number) =>
    languages.find((l) => l.language_id === id)?.name ?? `Ngôn ngữ #${id}`;

  return (
    <div className="max-w-3xl mx-auto animate-fade-in">
      <div className="page-header flex items-start justify-between">
        <div>
          <h1 className="page-title">Mô tả âm thanh</h1>
          <p className="page-description">
            Quản lý mô tả âm thanh theo các ngôn ngữ khác nhau
          </p>
        </div>
        <Button onClick={() => setDialogOpen(true)}>
          <Plus className="w-4 h-4 mr-2" /> Tải lên âm thanh
        </Button>
      </div>

      {loading ? (
        <div className="form-section text-center py-12">
          <p className="text-muted-foreground">
            Đang tải danh sách âm thanh...
          </p>
        </div>
      ) : audios.length === 0 ? (
        <div className="form-section text-center py-12">
          <p className="text-muted-foreground">
            Chưa có tệp âm thanh nào. Tải lên mô tả âm thanh đầu tiên.
          </p>
        </div>
      ) : (
        <div className="space-y-3">
          {audios.map((audio) => (
            <div
              key={audio.audio_id}
              className="dashboard-card flex items-center gap-4"
            >
              <div className="flex items-center justify-center w-10 h-10 rounded-lg bg-accent shrink-0">
                <Volume2 className="w-5 h-5 text-accent-foreground" />
              </div>
              <div className="flex-1 min-w-0">
                <h3 className="font-medium text-foreground">
                  {getLangName(audio.language_id)}
                </h3>
                <p className="text-sm text-muted-foreground">
                  Phiên bản {audio.version} ·{" "}
                  {new Date(audio.date_generation).toLocaleDateString("vi-VN")}
                </p>
              </div>
              <div className="flex items-center gap-3 shrink-0">
                <div className="flex items-center gap-2">
                  <span
                    className={`text-xs font-medium ${audio.is_active ? "text-success" : "text-muted-foreground"}`}
                  >
                    {audio.is_active ? "Hoạt động" : "Không hoạt động"}
                  </span>
                  <Switch
                    checked={audio.is_active}
                    onCheckedChange={() => void toggleActive(audio.audio_id)}
                  />
                </div>
                <Button
                  variant="ghost"
                  size="icon"
                  onClick={() => void removeAudio(audio.audio_id)}
                >
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
                  {languages.map((lang) => (
                    <SelectItem
                      key={lang.language_id}
                      value={String(lang.language_id)}
                    >
                      {lang.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2">
              <Label>Tệp âm thanh</Label>
              <input
                ref={fileInputRef}
                type="file"
                accept="audio/*"
                className="hidden"
                onChange={(event) => {
                  const file = event.target.files?.[0] ?? null;
                  setSelectedFile(file);
                }}
              />
              <div
                className="border-2 border-dashed rounded-lg p-8 text-center text-muted-foreground cursor-pointer hover:border-primary/50 transition-colors"
                onClick={() => fileInputRef.current?.click()}
              >
                <Volume2 className="w-8 h-8 mx-auto mb-2 opacity-50" />
                <p className="text-sm">
                  {selectedFile
                    ? selectedFile.name
                    : "Nhấp để chọn tệp âm thanh"}
                </p>
                <p className="text-xs mt-1">MP3, WAV tối đa 10MB</p>
              </div>
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setDialogOpen(false)}>
              Hủy
            </Button>
            <Button onClick={() => void handleUpload()} disabled={uploading}>
              {uploading ? "Đang tải..." : "Tải lên"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
