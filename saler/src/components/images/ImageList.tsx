import { ImageIcon } from "lucide-react";
import type { RestaurantImage } from "@/types";
import { ImageCard } from "./ImageCard";

interface ImageListProps {
  images: RestaurantImage[];
  onSetPrimary: (id: number) => void;
  onMoveUp: (id: number) => void;
  onMoveDown: (id: number) => void;
  onDelete: (id: number) => void;
  onReplace: (id: number) => void;
}

/**
 * ImageList renders all restaurant images in a grid layout.
 *
 * Display rules:
 * - Images are sorted by sort_order (ascending)
 * - Primary image (is_primary = 1) always appears first via sorting
 * - Non-primary images follow as gallery items
 *
 * POIs:
 * - page load → renders image grid
 * - item click → handled by ImageCard actions
 */
export function ImageList({
  images,
  onSetPrimary,
  onMoveUp,
  onMoveDown,
  onDelete,
  onReplace,
}: ImageListProps) {
  if (images.length === 0) {
    return (
      <div className="form-section text-center py-12">
        <div className="flex flex-col items-center gap-3 text-muted-foreground">
          <ImageIcon className="w-12 h-12 opacity-40" />
          <p>Chưa có hình ảnh nào. Tải lên hình ảnh đầu tiên.</p>
        </div>
      </div>
    );
  }

  return (
    <div className="grid gap-4 sm:grid-cols-2">
      {images.map((img, i) => (
        <ImageCard
          key={img.image_id}
          image={img}
          index={i}
          total={images.length}
          onSetPrimary={onSetPrimary}
          onMoveUp={onMoveUp}
          onMoveDown={onMoveDown}
          onDelete={onDelete}
          onReplace={onReplace}
        />
      ))}
    </div>
  );
}
