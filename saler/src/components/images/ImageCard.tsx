import { Star, StarOff, ArrowUp, ArrowDown, Trash2, Replace } from "lucide-react";
import { Button } from "@/components/ui/button";
import type { RestaurantImage } from "@/types";

interface ImageCardProps {
  image: RestaurantImage;
  /** Current sort position (0-indexed, sorted by sort_order) */
  index: number;
  /** Total images in the list */
  total: number;
  /** POI: set primary — toggle an image to be the restaurant avatar */
  onSetPrimary: (id: number) => void;
  /** POI: reorder — move image up in the gallery */
  onMoveUp: (id: number) => void;
  /** POI: reorder — move image down in the gallery */
  onMoveDown: (id: number) => void;
  /** POI: delete image */
  onDelete: (id: number) => void;
  /** POI: upload action — replace the existing image file */
  onReplace: (id: number) => void;
}

/**
 * ImageCard renders a single restaurant image with:
 * - Primary badge (when is_primary = 1)
 * - Action buttons: set primary, reorder up/down, replace, delete
 */
export function ImageCard({
  image,
  index,
  total,
  onSetPrimary,
  onMoveUp,
  onMoveDown,
  onDelete,
  onReplace,
}: ImageCardProps) {
  return (
    <div className="dashboard-card p-0 overflow-hidden group">
      {/* Image preview */}
      <div className="aspect-video relative">
        <img
          src={image.image_url}
          alt={`Hình ảnh nhà hàng ${index + 1}`}
          className="w-full h-full object-cover"
        />
        {/* Primary badge — POI: shows avatar label */}
        {image.is_primary && (
          <span className="absolute top-2 left-2 bg-primary text-primary-foreground text-xs font-medium px-2 py-1 rounded-md select-none">
            Ảnh chính
          </span>
        )}
      </div>

      {/* Action bar */}
      <div className="p-3 flex items-center gap-1">
        {/* Set as primary — POI: set primary */}
        <Button
          variant="ghost"
          size="icon"
          onClick={() => onSetPrimary(image.image_id)}
          title={image.is_primary ? "Ảnh chính" : "Đặt làm ảnh chính"}
        >
          {image.is_primary ? (
            <Star className="w-4 h-4 text-primary fill-primary" />
          ) : (
            <StarOff className="w-4 h-4" />
          )}
        </Button>

        {/* Reorder up — POI: reorder */}
        <Button
          variant="ghost"
          size="icon"
          onClick={() => onMoveUp(image.image_id)}
          disabled={index === 0}
          title="Di chuyển lên"
        >
          <ArrowUp className="w-4 h-4" />
        </Button>

        {/* Reorder down — POI: reorder */}
        <Button
          variant="ghost"
          size="icon"
          onClick={() => onMoveDown(image.image_id)}
          disabled={index === total - 1}
          title="Di chuyển xuống"
        >
          <ArrowDown className="w-4 h-4" />
        </Button>

        {/* Replace — POI: upload action — replaces the image file only */}
        <Button
          variant="ghost"
          size="icon"
          onClick={() => onReplace(image.image_id)}
          title="Thay ảnh"
        >
          <Replace className="w-4 h-4" />
        </Button>

        <div className="flex-1" />

        {/* Delete — POI: delete image */}
        <Button
          variant="ghost"
          size="icon"
          onClick={() => onDelete(image.image_id)}
          title="Xóa ảnh"
        >
          <Trash2 className="w-4 h-4 text-destructive" />
        </Button>
      </div>
    </div>
  );
}
