import { useMemo, useRef, useState, useEffect } from "react";
import {
  deleteAudioApi,
  getLanguagesApi,
  getRestaurantAudiosApi,
  updateAudioActiveApi,
  uploadAudioApi,
} from "@/services/api";
import { useRestaurant } from "@/contexts/RestaurantContext";
import type { Audio, Language } from "@/types";
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
import { Pause, Play, Plus, Volume2, Trash2 } from "lucide-react";

const API_BASE = (import.meta.env.VITE_API_BASE_URL as string | undefined) ?? "http://localhost:5042";

export default function AudioPage() {
  const { selectedRestaurant } = useRestaurant();
  const [audios, setAudios] = useState<Audio[]>([]);
  const [languages, setLanguages] = useState<Language[]>([]);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [selectedLang, setSelectedLang] = useState<string>("");
  const fileInputRef = useRef<HTMLInputElement | null>(null);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const playerRef = useRef<HTMLAudioElement | null>(null);
  const [playingAudioId, setPlayingAudioId] = useState<number | null>(null);

  const versionByAudioId = useMemo(() => {
    const sorted = [...audios].sort((a, b) => {
      const dateDiff = new Date(a.date_generation).getTime() - new Date(b.date_generation).getTime();
      if (dateDiff !== 0) return dateDiff;
      return a.audio_id - b.audio_id;
    });

    const counts = new Map<number, number>();
    const versions = new Map<number, number>();

    for (const audio of sorted) {
      const next = (counts.get(audio.language_id) ?? 0) + 1;
      counts.set(audio.language_id, next);
      versions.set(audio.audio_id, next);
    }

    return versions;
  }, [audios]);

  useEffect(() => {
    if (selectedRestaurant) {
      (async () => {
        try {
          const [audioData, languageData] = await Promise.all([
            getRestaurantAudiosApi(selectedRestaurant.restaurant_id),
            getLanguagesApi(),
          ]);
          setAudios(audioData ?? []);
          setLanguages(languageData ?? []);
        } catch {
          toast.error("Không thể tải danh sách âm thanh");
        }
      })();
    }
  }, [selectedRestaurant]);

  useEffect(() => {
    return () => {
      if (playerRef.current) {
        playerRef.current.pause();
        playerRef.current = null;
      }
    };
  }, []);

  const toggleActive = async (id: number) => {
    const current = audios.find((a) => a.audio_id === id);
    if (!current) return;

    try {
      await updateAudioActiveApi(id, !current.is_active);
      setAudios((prev) =>
        prev.map((a) => (a.audio_id === id ? { ...a, is_active: !a.is_active } : a))
      );
      toast.success("Đã cập nhật trạng thái âm thanh");
    } catch {
      toast.error("Không thể cập nhật trạng thái âm thanh");
    }
  };

  const deleteAudio = async (id: number) => {
    try {
      await deleteAudioApi(id);
      setAudios((prev) => prev.filter((a) => a.audio_id !== id));
      toast.success("Đã xóa âm thanh");
    } catch {
      toast.error("Không thể xóa âm thanh");
    }
  };

  const handleUpload = async () => {
    if (!selectedLang) {
      toast.error("Vui lòng chọn ngôn ngữ");
      return;
    }

    if (!selectedFile) {
      toast.error("Vui lòng chọn tệp âm thanh");
      return;
    }

    if (!selectedRestaurant) return;

    const langId = parseInt(selectedLang);

    try {
      const created = await uploadAudioApi(selectedRestaurant.restaurant_id, langId, selectedFile);
      setAudios((prev) => [...prev, created]);
      setDialogOpen(false);
      setSelectedLang("");
      setSelectedFile(null);
      toast.success("Đã tải âm thanh lên thành công");
    } catch {
      toast.error("Không thể tải âm thanh lên");
    }
  };

  const getLangName = (id: number) => languages.find((l) => l.language_id === id)?.name ?? `Ngôn ngữ #${id}`;

  const resolveAudioUrl = (audio: Audio): string => {
    const raw = audio.audio_url?.trim();
    if (!raw) return "";

    if (/^https?:\/\//i.test(raw)) {
      return raw;
    }

    if (raw.startsWith("/")) {
      return new URL(raw, API_BASE).toString();
    }

    const languageCode = languages.find((l) => l.language_id === audio.language_id)?.code;
    if (languageCode) {
      return new URL(`/maui-audios/languages/${languageCode}/${raw}`, API_BASE).toString();
    }

    return new URL(`/maui-audios/${raw}`, API_BASE).toString();
  };

  const togglePlay = (audio: Audio) => {
    if (playingAudioId === audio.audio_id && playerRef.current) {
      playerRef.current.pause();
      setPlayingAudioId(null);
      return;
    }

    if (playerRef.current) {
      playerRef.current.pause();
      playerRef.current = null;
    }

    const audioUrl = resolveAudioUrl(audio);
    if (!audioUrl) {
      toast.error("Không tìm thấy đường dẫn tệp âm thanh");
      return;
    }

    const player = new Audio(audioUrl);
    playerRef.current = player;
    setPlayingAudioId(audio.audio_id);

    player.onended = () => {
      setPlayingAudioId(null);
      playerRef.current = null;
    };

    player.onerror = () => {
      setPlayingAudioId(null);
      playerRef.current = null;
      toast.error("Không thể phát tệp âm thanh");
    };

    void player.play().catch(() => {
      setPlayingAudioId(null);
      playerRef.current = null;
      toast.error("Không thể phát tệp âm thanh");
    });
  };

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
              <Button
                type="button"
                variant="ghost"
                size="icon"
                className="w-10 h-10 rounded-lg bg-accent shrink-0"
                onClick={() => togglePlay(audio)}
                title={playingAudioId === audio.audio_id ? "Tạm dừng" : "Phát âm thanh"}
              >
                {playingAudioId === audio.audio_id ? (
                  <Pause className="w-5 h-5 text-accent-foreground" />
                ) : (
                  <Play className="w-5 h-5 text-accent-foreground" />
                )}
              </Button>
              <div className="flex-1 min-w-0">
                <h3 className="font-medium text-foreground">{getLangName(audio.language_id)}</h3>
                <p className="text-sm text-muted-foreground">
                  Phiên bản {versionByAudioId.get(audio.audio_id) ?? 1} · {new Date(audio.date_generation).toLocaleDateString("vi-VN")}
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
                  {languages.map((lang) => (
                    <SelectItem key={lang.language_id} value={String(lang.language_id)}>
                      {lang.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2">
              <Label>Tệp âm thanh</Label>
              <div
                className="border-2 border-dashed rounded-lg p-8 text-center text-muted-foreground cursor-pointer hover:border-primary/50 transition-colors"
                onClick={() => fileInputRef.current?.click()}
              >
                <Volume2 className="w-8 h-8 mx-auto mb-2 opacity-50" />
                <p className="text-sm">{selectedFile ? selectedFile.name : "Nhấp để chọn tệp âm thanh"}</p>
                <p className="text-xs mt-1">MP3, WAV tối đa 10MB</p>
              </div>
              <input
                ref={fileInputRef}
                type="file"
                accept="audio/*"
                className="hidden"
                onChange={(e) => setSelectedFile(e.target.files?.[0] ?? null)}
              />
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
