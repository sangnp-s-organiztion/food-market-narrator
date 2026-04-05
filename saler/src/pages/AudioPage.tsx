import { useCallback, useEffect, useMemo, useRef, useState } from "react";
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
import { Pause, Play, Plus, Volume2, Trash2 } from "lucide-react";

const API_BASE =
  (import.meta.env.VITE_API_BASE_URL as string | undefined) ??
  "http://localhost:5044";

export default function AudioPage() {
  const { selectedRestaurant } = useRestaurant();
  const [audios, setAudios] = useState<Audio[]>([]);
  const [languages, setLanguages] = useState<Language[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [selectedLang, setSelectedLang] = useState<string>("");
  const fileInputRef = useRef<HTMLInputElement | null>(null);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const playerRef = useRef<HTMLAudioElement | null>(null);
  const [playingAudioId, setPlayingAudioId] = useState<number | null>(null);

  const versionByAudioId = useMemo(() => {
    const sorted = [...audios].sort((a, b) => {
      const dateDiff =
        new Date(a.date_generation).getTime() -
        new Date(b.date_generation).getTime();
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

  const languageGroups = useMemo(() => {
    const audiosByLanguage = new Map<number, Audio[]>();

    audios.forEach((audio) => {
      const list = audiosByLanguage.get(audio.language_id) ?? [];
      list.push(audio);
      audiosByLanguage.set(audio.language_id, list);
    });

    audiosByLanguage.forEach((list) => {
      list.sort((a, b) => {
        const dateDiff =
          new Date(b.date_generation).getTime() -
          new Date(a.date_generation).getTime();
        if (dateDiff !== 0) return dateDiff;
        return b.audio_id - a.audio_id;
      });
    });

    const languageOrder = new Map<number, number>();
    languages.forEach((lang, idx) => {
      languageOrder.set(lang.language_id, idx);
    });

    const ids = new Set<number>([
      ...languages.map((l) => l.language_id),
      ...audios.map((a) => a.language_id),
    ]);

    return [...ids]
      .map((languageId) => {
        const language = languages.find((l) => l.language_id === languageId);
        const items = audiosByLanguage.get(languageId) ?? [];

        return {
          languageId,
          languageName: language?.name ?? `Ngôn ngữ #${languageId}`,
          items,
          activeCount: items.filter((a) => a.is_active).length,
        };
      })
      .sort((a, b) => {
        const orderA = languageOrder.get(a.languageId);
        const orderB = languageOrder.get(b.languageId);

        if (orderA != null && orderB != null) return orderA - orderB;
        if (orderA != null) return -1;
        if (orderB != null) return 1;
        return a.languageName.localeCompare(b.languageName);
      });
  }, [audios, languages]);

  const fetchAudioData = useCallback(async () => {
    if (!selectedRestaurant) {
      setAudios([]);
      setLanguages([]);
      return;
    }

    setIsLoading(true);
    try {
      const [audioData, languageData] = await Promise.all([
        getRestaurantAudiosApi(selectedRestaurant.restaurant_id),
        getLanguagesApi(),
      ]);
      setAudios(audioData ?? []);
      setLanguages(languageData ?? []);
    } catch {
      toast.error("Không thể tải danh sách âm thanh");
    } finally {
      setIsLoading(false);
    }
  }, [selectedRestaurant]);

  useEffect(() => {
    void fetchAudioData();
  }, [fetchAudioData]);

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
      await fetchAudioData();
      toast.success("Đã cập nhật trạng thái âm thanh");
    } catch {
      toast.error("Không thể cập nhật trạng thái âm thanh");
    }
  };

  const deleteAudio = async (id: number) => {
    try {
      await deleteAudioApi(id);
      await fetchAudioData();
      toast.success("Đã xóa âm thanh");
    } catch {
      toast.error("Không thể xóa âm thanh");
    }
  };

  const openUploadDialog = (languageId?: number) => {
    setSelectedLang(languageId ? String(languageId) : "");
    setSelectedFile(null);
    setDialogOpen(true);
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
      await uploadAudioApi(
        selectedRestaurant.restaurant_id,
        langId,
        selectedFile,
      );
      await fetchAudioData();
      setDialogOpen(false);
      setSelectedLang("");
      setSelectedFile(null);
      toast.success("Đã tải âm thanh lên thành công");
    } catch {
      toast.error("Không thể tải âm thanh lên");
    }
  };

  const resolveAudioUrl = (audio: Audio): string => {
    const raw = audio.audio_url?.trim();
    if (!raw) return "";

    if (/^https?:\/\//i.test(raw)) {
      return raw;
    }

    if (raw.startsWith("/")) {
      return new URL(raw, API_BASE).toString();
    }

    const languageCode = languages.find(
      (l) => l.language_id === audio.language_id,
    )?.code;
    if (languageCode) {
      return new URL(
        `/maui-audios/languages/${languageCode}/${raw}`,
        API_BASE,
      ).toString();
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
          <p className="page-description">
            Quản lý theo từng ngôn ngữ, mỗi ngôn ngữ chỉ nên có 1 âm thanh hoạt
            động
          </p>
        </div>
        <Button onClick={() => openUploadDialog()}>
          <Plus className="w-4 h-4 mr-2" /> Tải lên âm thanh
        </Button>
      </div>

      {isLoading ? (
        <div className="form-section text-center py-12 text-muted-foreground">
          Đang tải dữ liệu âm thanh...
        </div>
      ) : (
        <div className="space-y-3">
          {languageGroups.length === 0 && (
            <div className="form-section text-center py-12">
              <p className="text-muted-foreground">
                Chưa có tệp âm thanh nào. Tải lên mô tả âm thanh đầu tiên.
              </p>
            </div>
          )}

          {languageGroups.map((group) => (
            <div key={group.languageId} className="dashboard-card space-y-3">
              <div className="flex items-start justify-between gap-3">
                <div>
                  <h3 className="font-semibold text-foreground text-base">
                    {group.languageName}
                  </h3>
                  <p className="text-xs text-muted-foreground mt-0.5">
                    {group.items.length} phiên bản - {group.activeCount} đang
                    hoạt động
                  </p>
                  {group.activeCount > 1 && (
                    <p className="text-xs text-destructive mt-1">
                      Có hơn 1 âm thanh đang hoạt động trong cùng ngôn ngữ.
                    </p>
                  )}
                </div>
                <Button
                  size="sm"
                  variant="outline"
                  onClick={() => openUploadDialog(group.languageId)}
                >
                  <Plus className="w-4 h-4 mr-1" /> Thêm bản ghi
                </Button>
              </div>

              {group.items.length === 0 ? (
                <p className="text-sm text-muted-foreground py-2">
                  Chưa có âm thanh cho ngôn ngữ này.
                </p>
              ) : (
                <div className="space-y-2">
                  {group.items.map((audio) => (
                    <div
                      key={audio.audio_id}
                      className="flex items-center gap-4 rounded-lg border border-border/60 px-4 py-3"
                    >
                      <Button
                        type="button"
                        variant="ghost"
                        size="icon"
                        className="w-10 h-10 rounded-lg bg-accent shrink-0"
                        onClick={() => togglePlay(audio)}
                        title={
                          playingAudioId === audio.audio_id
                            ? "Tạm dừng"
                            : "Phát âm thanh"
                        }
                      >
                        {playingAudioId === audio.audio_id ? (
                          <Pause className="w-5 h-5 text-accent-foreground" />
                        ) : (
                          <Play className="w-5 h-5 text-accent-foreground" />
                        )}
                      </Button>

                      <div className="flex-1 min-w-0">
                        <h4 className="font-medium text-foreground">
                          Phiên bản {versionByAudioId.get(audio.audio_id) ?? 1}
                        </h4>
                        <p className="text-sm text-muted-foreground">
                          {new Date(audio.date_generation).toLocaleDateString(
                            "vi-VN",
                          )}
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
                            onCheckedChange={() => toggleActive(audio.audio_id)}
                          />
                        </div>
                        <Button
                          variant="ghost"
                          size="icon"
                          onClick={() => deleteAudio(audio.audio_id)}
                        >
                          <Trash2 className="w-4 h-4 text-destructive" />
                        </Button>
                      </div>
                    </div>
                  ))}
                </div>
              )}
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
            <Button variant="outline" onClick={() => setDialogOpen(false)}>
              Hủy
            </Button>
            <Button onClick={handleUpload}>Tải lên</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
