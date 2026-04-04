#!/usr/bin/env python3
"""
Generate a QR code for Food Market Narrator deep-link with time limit.

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


def ask_unit() -> str:
    while True:
        print("\nChon don vi thoi gian:")
        print("1) Giay")
        print("2) Phut")
        raw = input("Nhap lua chon (1/2): ").strip()

        if raw == "1":
            return "seconds"
        if raw == "2":
            return "minutes"

        print("Lua chon khong hop le. Vui long nhap 1 hoac 2.")


def ask_positive_number(label: str) -> str:
    while True:
        raw = input(f"Nhap so {label}: ").strip()
        try:
            value = float(raw)
            if value <= 0:
                print("Gia tri phai lon hon 0.")
                continue
            return raw
        except ValueError:
            print("Gia tri khong hop le. Vui long nhap so.")


def build_deep_link(unit: str, value_text: str) -> str:
    if unit == "seconds":
        return f"{BASE_DEEP_LINK}?durationSeconds={value_text}"
    return f"{BASE_DEEP_LINK}?durationMinutes={value_text}"


def suggest_filename(unit: str, value_text: str) -> str:
    safe_value = value_text.replace(".", "_")
    suffix = "s" if unit == "seconds" else "m"
    return f"qr_{safe_value}{suffix}.png"


def main() -> None:
    print("=== QR Generator - Food Market Narrator ===")

    unit = ask_unit()
    value_text = ask_positive_number("thoi gian")
    deep_link = build_deep_link(unit, value_text)

    default_name = suggest_filename(unit, value_text)
    out_name = input(f"Ten file output (Enter de dung '{default_name}'): ").strip() or default_name

    out_path = Path.cwd() / out_name

    img = qrcode.make(deep_link)
    img.save(out_path)

    print("\nDa tao QR thanh cong")
    print(f"Deep link: {deep_link}")
    print(f"File: {out_path}")


if __name__ == "__main__":
    main()
