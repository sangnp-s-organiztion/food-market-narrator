import { useRef, type ChangeEvent } from "react";
import { Upload } from "lucide-react";
import { Button } from "@/components/ui/button";

interface UploadButtonProps {
  onFileSelect: (file: File) => void;
  accept?: string;
  children?: React.ReactNode;
  disabled?: boolean;
}

/**
 * Generic upload button that triggers a hidden file input.
 * POI: upload action — fires when user selects a file.
 */
export function UploadButton({
  onFileSelect,
  accept = "image/*",
  children,
  disabled = false,
}: UploadButtonProps) {
  const inputRef = useRef<HTMLInputElement>(null);

  const handleClick = () => {
    inputRef.current?.click();
  };

  const handleChange = (e: ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      onFileSelect(file);
    }
    // Reset so same file can be re-selected
    e.target.value = "";
  };

  return (
    <>
      <Button
        type="button"
        onClick={handleClick}
        disabled={disabled}
      >
        <Upload className="w-4 h-4 mr-2" />
        {children ?? "Tải ảnh lên"}
      </Button>
      <input
        ref={inputRef}
        type="file"
        accept={accept}
        className="hidden"
        onChange={handleChange}
      />
    </>
  );
}
