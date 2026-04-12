import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import {
  createAudioFromTextApi,
  deleteAudioApi,
  getLanguagesApi,
  getRestaurantAudiosApi,
  translateAudioTextApi,
  updateAudioActiveApi,
  uploadAudioApi,
} from "@/services/api";
import { useRestaurant } from "@/contexts/RestaurantContext";
import type { Audio, Language } from "@/types";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Switch } from "@/components/ui/switch";
import { Textarea } from "@/components/ui/textarea";
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
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import { toast } from "sonner";
import {
  Loader2,
  Pause,
  Play,
  Plus,
  Sparkles,
  Trash2,
  Volume2,
} from "lucide-react";

const API_BASE =
  (import.meta.env.VITE_API_BASE_URL as string | undefined) ??
  "http://localhost:5044";

const TARGET_LANGUAGE_OPTIONS = [
  { code: "vi", label: "Tiếng Việt" },
  { code: "en", label: "Tiếng Anh" },
  { code: "ja", label: "Tiếng Nhật" },
  { code: "zh", label: "Tiếng Trung" },
  { code: "ko", label: "Tiếng Hàn" },
] as const;

const SOURCE_LANGUAGE_OPTIONS = [
  { code: "auto", label: "Tự động" },
  ...TARGET_LANGUAGE_OPTIONS,
] as const;

export default function AudioPage() {
  const { selectedRestaurant } = useRestaurant();
  const [audios, setAudios] = useState<Audio[]>([]);
  const [languages, setLanguages] = useState<Language[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [pendingDeleteAudio, setPendingDeleteAudio] = useState<Audio | null>(
    null,
  );
  const [selectedLang, setSelectedLang] = useState<string>("");
  const fileInputRef = useRef<HTMLInputElement | null>(null);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const playerRef = useRef<HTMLAudioElement | null>(null);
  const [playingKey, setPlayingKey] = useState<string | null>(null);

  const [sourceText, setSourceText] = useState("");
  const [translatedText, setTranslatedText] = useState("");
  const [sourceLangCode, setSourceLangCode] = useState<string>("vi");
  const [targetLangCode, setTargetLangCode] = useState<string>("en");
  const [isTranslating, setIsTranslating] = useState(false);
  const [isGenerating, setIsGenerating] = useState(false);
  const [generatedAudioUrl, setGeneratedAudioUrl] = useState("");
  const [lastTranslatedKey, setLastTranslatedKey] = useState<string | null>(
    null,
  );
  const [, setGeneratedAudioId] = useState<number | null>(null);
  const [, setTranslateMeta] = useState<{
    inputChars: number;
    outputChars: number;
    estimatedCost: number;
    currency: string;
  } | null>(null);

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

  const sourceTextTrimmed = useMemo(() => sourceText.trim(), [sourceText]);
  const isSameSourceTargetLanguage = sourceLangCode === targetLangCode;
  const currentTranslationKey = useMemo(
    () => `${sourceLangCode}|${targetLangCode}|${sourceTextTrimmed}`,
    [sourceLangCode, targetLangCode, sourceTextTrimmed],
  );
  const hasFreshTranslatedText =
    !isSameSourceTargetLanguage &&
    translatedText.trim().length > 0 &&
    lastTranslatedKey === currentTranslationKey;
  const canCreateAudio =
    sourceTextTrimmed.length > 0 &&
    (isSameSourceTargetLanguage || hasFreshTranslatedText);

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

  const toggleActive = async (id: number, nextChecked: boolean) => {
    const current = audios.find((a) => a.audio_id === id);
    if (!current) return;

    if (current.is_active && !nextChecked) {
      const activeCountInLanguage = audios.filter(
        (a) => a.language_id === current.language_id && a.is_active,
      ).length;

      if (activeCountInLanguage <= 1) {
        toast.error("Mỗi ngôn ngữ phải có ít nhất 1 bản thuyết minh hoạt động");
        return;
      }
    }

    try {
      await updateAudioActiveApi(id, nextChecked);
      await fetchAudioData();
      toast.success("Đã cập nhật trạng thái âm thanh");
    } catch {
      toast.error("Không thể cập nhật trạng thái âm thanh");
    }
  };

  const deleteAudio = async (id: number) => {
    const current = audios.find((a) => a.audio_id === id);
    if (!current) return;

    const totalInLanguage = audios.filter(
      (a) => a.language_id === current.language_id,
    ).length;

    if (totalInLanguage <= 1) {
      toast.error("Mỗi ngôn ngữ phải có ít nhất 1 bản thuyết minh");
      return;
    }

    try {
      await deleteAudioApi(id);
      await fetchAudioData();
      toast.success("Đã xóa âm thanh");
    } catch {
      toast.error("Không thể xóa âm thanh");
    }
  };

  const confirmDeleteAudio = async () => {
    if (!pendingDeleteAudio) return;
    await deleteAudio(pendingDeleteAudio.audio_id);
    setPendingDeleteAudio(null);
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

    const langId = parseInt(selectedLang, 10);

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

  const togglePlayByUrl = (audioUrl: string, key: string) => {
    if (!audioUrl) {
      toast.error("Không tìm thấy đường dẫn tệp âm thanh");
      return;
    }

    if (playingKey === key && playerRef.current) {
      playerRef.current.pause();
      setPlayingKey(null);
      return;
    }

    if (playerRef.current) {
      playerRef.current.pause();
      playerRef.current = null;
    }

    const player = new Audio(audioUrl);
    playerRef.current = player;
    setPlayingKey(key);

    player.onended = () => {
      setPlayingKey(null);
      playerRef.current = null;
    };

    player.onerror = () => {
      setPlayingKey(null);
      playerRef.current = null;
      toast.error("Không thể phát tệp âm thanh");
    };

    void player.play().catch(() => {
      setPlayingKey(null);
      playerRef.current = null;
      toast.error("Không thể phát tệp âm thanh");
    });
  };

  const togglePlayAudioItem = (audio: Audio) => {
    const audioUrl = resolveAudioUrl(audio);
    togglePlayByUrl(audioUrl, `audio-${audio.audio_id}`);
  };

  const handleTranslateText = async () => {
    if (!selectedRestaurant) return;

    const text = sourceTextTrimmed;
    if (!text) {
      toast.error("Vui lòng nhập nội dung cần dịch");
      return;
    }

    try {
      setIsTranslating(true);
      const result = await translateAudioTextApi(
        selectedRestaurant.restaurant_id,
        {
          text,
          source_language_code: sourceLangCode,
          target_language_code: targetLangCode,
        },
      );

      setTranslatedText(result.translated_text);
      setTranslateMeta({
        inputChars: result.input_chars,
        outputChars: result.output_chars,
        estimatedCost: result.estimated_cost,
        currency: result.currency,
      });
      setLastTranslatedKey(currentTranslationKey);
      setGeneratedAudioUrl("");
      setGeneratedAudioId(null);
      toast.success("Dịch văn bản thành công");
    } catch (error) {
      toast.error(extractErrorMessage(error, "Không thể dịch văn bản"));
    } finally {
      setIsTranslating(false);
    }
  };

  const handleCreateAudioFromText = async () => {
    if (!selectedRestaurant) return;

    if (!sourceTextTrimmed) {
      toast.error("Vui lòng nhập nội dung trước khi tạo âm thanh");
      return;
    }

    if (!isSameSourceTargetLanguage && !hasFreshTranslatedText) {
      toast.error(
        "Ngôn ngữ đích khác ngôn ngữ nguồn. Vui lòng bấm Dịch trước khi tạo âm thanh.",
      );
      return;
    }

    const textForAudio = isSameSourceTargetLanguage
      ? sourceTextTrimmed
      : translatedText.trim();

    try {
      setIsGenerating(true);
      const result = await createAudioFromTextApi(
        selectedRestaurant.restaurant_id,
        {
          text: textForAudio,
          language_code: targetLangCode,
          source_text: sourceTextTrimmed || undefined,
        },
      );

      const normalizedUrl = /^https?:\/\//i.test(result.audio_url)
        ? result.audio_url
        : new URL(result.audio_url, API_BASE).toString();

      setGeneratedAudioUrl(normalizedUrl);
      setGeneratedAudioId(result.audio_id);
      await fetchAudioData();
      toast.success("Đã tạo âm thanh thành công");
    } catch (error) {
      toast.error(
        extractErrorMessage(error, "Không thể tạo âm thanh từ văn bản"),
      );
    } finally {
      setIsGenerating(false);
    }
  };

  const extractErrorMessage = (error: unknown, fallback: string): string => {
    if (!(error instanceof Error) || !error.message) {
      return fallback;
    }

    const message = error.message.trim();
    if (!message) {
      return fallback;
    }

    try {
      const parsed = JSON.parse(message) as {
        message?: string;
        detail?: string;
        error?: string;
      };

      return parsed.message ?? parsed.detail ?? parsed.error ?? fallback;
    } catch {
      return message;
    }
  };

  const handlePlayGeneratedAudio = () => {
    if (!generatedAudioUrl) {
      toast.error("Chưa có âm thanh được tạo");
      return;
    }

    togglePlayByUrl(generatedAudioUrl, "generated");
  };

  return (
    <div className="max-w-5xl mx-auto animate-fade-in space-y-6">
      <div className="page-header flex items-start justify-between">
        <div>
          <h1 className="page-title">Mô tả âm thanh</h1>
          <p className="page-description">
            Nhập nội dung, dịch sang ngôn ngữ mong muốn, tạo âm thanh bằng Edge
            TTS và quản lý phiên bản âm thanh của nhà hàng.
          </p>
        </div>
        <Button onClick={() => openUploadDialog()}>
          <Plus className="w-4 h-4 mr-2" /> Tải lên âm thanh
        </Button>
      </div>

      <section className="dashboard-card space-y-4">
        <div className="flex items-center gap-2">
          <Sparkles className="w-4 h-4 text-primary" />
          <h2 className="font-semibold text-base">
            Dịch văn bản và tạo âm thanh
          </h2>
        </div>

        <div className="space-y-2">
          <Label htmlFor="source-text">Nội dung văn bản</Label>
          <Textarea
            id="source-text"
            value={sourceText}
            onChange={(e) => {
              setSourceText(e.target.value);
              setLastTranslatedKey(null);
            }}
            placeholder="Nhập nội dung mô tả nhà hàng để dịch và tạo âm thanh..."
            className="min-h-36"
          />
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
          <div className="space-y-2">
            <Label>Ngôn ngữ nguồn</Label>
            <Select
              value={sourceLangCode}
              onValueChange={(value) => {
                setSourceLangCode(value);
                setLastTranslatedKey(null);
              }}
            >
              <SelectTrigger>
                <SelectValue placeholder="Chọn ngôn ngữ nguồn" />
              </SelectTrigger>
              <SelectContent>
                {SOURCE_LANGUAGE_OPTIONS.map((lang) => (
                  <SelectItem key={lang.code} value={lang.code}>
                    {lang.label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <div className="space-y-2">
            <Label>Ngôn ngữ đích</Label>
            <Select
              value={targetLangCode}
              onValueChange={(value) => {
                setTargetLangCode(value);
                setLastTranslatedKey(null);
              }}
            >
              <SelectTrigger>
                <SelectValue placeholder="Chọn ngôn ngữ đích" />
              </SelectTrigger>
              <SelectContent>
                {TARGET_LANGUAGE_OPTIONS.map((lang) => (
                  <SelectItem key={lang.code} value={lang.code}>
                    {lang.label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
        </div>

        <div className="flex flex-wrap gap-3">
          <Button
            type="button"
            onClick={handleTranslateText}
            disabled={isTranslating}
          >
            {isTranslating && <Loader2 className="w-4 h-4 mr-2 animate-spin" />}
            Dịch
          </Button>

          <Button
            type="button"
            variant="secondary"
            onClick={handleCreateAudioFromText}
            disabled={isGenerating || !canCreateAudio}
          >
            {isGenerating && <Loader2 className="w-4 h-4 mr-2 animate-spin" />}
            Tạo âm thanh
          </Button>

          <Button
            type="button"
            variant="outline"
            onClick={handlePlayGeneratedAudio}
            disabled={!generatedAudioUrl}
          >
            {playingKey === "generated" ? (
              <Pause className="w-4 h-4 mr-2" />
            ) : (
              <Play className="w-4 h-4 mr-2" />
            )}
            Phát âm thanh
          </Button>
        </div>

        <div className="space-y-2">
          <Label>Kết quả dịch</Label>
          <Textarea
            value={translatedText}
            onChange={(e) => setTranslatedText(e.target.value)}
            placeholder="Kết quả dịch sẽ hiển thị ở đây"
            className="min-h-32"
          />
        </div>
      </section>

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
                        onClick={() => togglePlayAudioItem(audio)}
                        title={
                          playingKey === `audio-${audio.audio_id}`
                            ? "Tạm dừng"
                            : "Phát âm thanh"
                        }
                      >
                        {playingKey === `audio-${audio.audio_id}` ? (
                          <Pause className="w-5 h-5 text-accent-foreground" />
                        ) : (
                          <Play className="w-5 h-5 text-accent-foreground" />
                        )}
                      </Button>

                      <div className="flex-1 min-w-0">
                        <h4 className="font-medium text-foreground">
                          Phiên bản {audio.version}
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
                            disabled={audio.is_active && group.activeCount <= 1}
                            onCheckedChange={(checked) =>
                              toggleActive(audio.audio_id, checked)
                            }
                          />
                        </div>
                        <Button
                          variant="ghost"
                          size="icon"
                          disabled={group.items.length <= 1}
                          onClick={() => setPendingDeleteAudio(audio)}
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

      <AlertDialog
        open={pendingDeleteAudio !== null}
        onOpenChange={(open) => {
          if (!open) setPendingDeleteAudio(null);
        }}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Xác nhận xóa thuyết minh</AlertDialogTitle>
            <AlertDialogDescription>
              Bạn có chắc muốn xóa{" "}
              {pendingDeleteAudio
                ? `Phiên bản ${pendingDeleteAudio.version}`
                : "thuyết minh này"}
              ? Hành động này không thể hoàn tác.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Hủy</AlertDialogCancel>
            <AlertDialogAction
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
              onClick={() => {
                void confirmDeleteAudio();
              }}
            >
              Xóa
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
