# Geofence Cooldown

## 1) Muc tieu

Chan trigger lap lai lien tuc cho cung 1 POI sau khi vua trigger xong.

Cooldown giup audio/UI khong bi lap event qua day trong khoang ngan.

## 2) Tham so cau hinh

Them 1 tham so trong AppSettings:

- GeofenceCooldownSeconds = 45 (co the doi theo slide)

Y nghia:

- Sau khi POI A vua trigger, trong 45 giay tiep theo POI A se bi bo qua.

## 3) Thay doi trong POIService

Bo sung dictionary cooldown theo tung POI:

- \_poiCooldownUntilUtc: Dictionary<string, DateTimeOffset>

Them helper:

- IsInCooldown(string poiId, DateTimeOffset nowUtc)

Hanh vi:

1. Neu poiId khong co trong dictionary -> khong cooldown.
2. Neu moc cooldown da het han -> xoa key, cho trigger lai.
3. Neu con han -> return true, bo qua trigger.

## 4) Tich hop vao luong enter geofence

Trong UpdateNearestPOI, sau khi pass debounce:

1. Check IsInCooldown(poiId, nowUtc).
2. Neu dang cooldown -> return null.
3. Neu khong cooldown -> trigger event enter.
4. Sau khi trigger thanh cong, set lai cooldown:
   \_poiCooldownUntilUtc[poiId] = nowUtc + GeofenceCooldownSeconds.

## 5) Lien he voi Enter/Exit hysteresis

Logic hysteresis 30m/40m van duoc giu:

- EnterRadius: xet dieu kien vao.
- ExitRadius: xet dieu kien thoat.

Cooldown la lop bo sung sau debounce, khong thay the hysteresis.

## 6) Loi ich

- Tranh lap audio khi user di cham quanh cung mot diem.
- Giam spam su kien do location poll 2s/lan.
- Cac trigger gan nhau tro nen de du doan hon.

## 7) Checklist test nhanh

- Trigger vao POI A xong, o lai gan A: khong trigger lai trong cooldown.
- Sau khi qua cooldown, vao lai A: duoc trigger lai.
- Chuyen A -> B trong thoi gian cooldown cua A: B van trigger binh thuong neu dat dieu kien.
