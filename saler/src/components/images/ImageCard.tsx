import { Star, StarOff, ArrowUp, ArrowDown, Trash2, Replace } from "lucide-react";
import { Button } from "@/components/ui/button";
import type { RestaurantImage } from "@/types";

interface ImageCardProps {
  image: RestaurantImage;
  index: number;
  total: number;
  onSetPrimary: (id: number) => void;
  onMoveUp: (id: number) => void;
  onMoveDown: (id: number) => void;
  onDelete: (id: number) => void;
  onReplace: (id: number) => void;
}

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
      <div className="aspect-video relative">
        <img
          src={image.image_url}
          alt={`Hình ảnh nhà hàng ${index + 1}`}
          className="w-full h-full object-cover"
        />
        {image.is_primary && (
          <span className="absolute top-2 left-2 bg-primary text-primary-foreground text-xs font-medium px-2 py-1 rounded-md select-none">
            Ảnh chính
          </span>
        )}
      </div>

      <div className="p-3 flex items-center gap-1">
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

        <Button
          variant="ghost"
          size="icon"
          onClick={() => onMoveUp(image.image_id)}
          disabled={index === 0}
          title="Di chuyển lên"
        >
          <ArrowUp className="w-4 h-4" />
        </Button>

        <Button
          variant="ghost"
          size="icon"
          onClick={() => onMoveDown(image.image_id)}
          disabled={index === total - 1}
          title="Di chuyển xuống"
        >
          <ArrowDown className="w-4 h-4" />
        </Button>

        <Button
          variant="ghost"
          size="icon"
          onClick={() => onReplace(image.image_id)}
          title="Thay ảnh"
        >
          <Replace className="w-4 h-4" />
        </Button>

        <div className="flex-1" />

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
