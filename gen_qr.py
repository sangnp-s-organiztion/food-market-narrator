#!/usr/bin/env python3
"""
Generate a QR code for Food Market Narrator deep-link.

Usage:
  python gen_qr.py
"""

from pathlib import Path

try:
    import qrcode
except ImportError:
    print("Missing dependency: qrcode")
    print("Install with: pip install qrcode[pil]")
    raise SystemExit(1)

BASE_DEEP_LINK = "foodmarketnarrator://open"


def suggest_filename() -> str:
    return "qr_open_app.png"


def main() -> None:
    print("=== QR Generator - Food Market Narrator ===")

    deep_link = BASE_DEEP_LINK

    default_name = suggest_filename()
    out_name = input(f"Ten file output (Enter de dung '{default_name}'): ").strip() or default_name

    out_path = Path.cwd() / out_name

    img = qrcode.make(deep_link)
    img.save(out_path)

    print("\nDa tao QR thanh cong")
    print(f"Deep link: {deep_link}")
    print(f"File: {out_path}")


if __name__ == "__main__":
    main()
