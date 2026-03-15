# Audio cache luu o dau

Tai lieu nay mo ta vi tri app luu file audio da tai ve de phat offline.

## Tom tat

- Audio duoc luu trong AppDataDirectory cua app, trong thu muc con audio_cache.
- Ten file la hash + extension (vi du: A1B2C3... .mp3), khong giu ten goc tu server.
- Neu audio ton tai trong goi app thi app uu tien doc trong package truoc, sau do moi tai tu network.

## Bang chung trong code

- Tao duong dan cache: food-market-narrator-maui/Services/AudioService.cs (ham GetAudioCachePath)
- Goc thu muc cache: Path.Combine(FileSystem.AppDataDirectory, "audio_cache")
- Ten file cache: Path.Combine(cacheRoot, $"{hash}{extension}")
- Doc file trong package app: FileSystem.OpenAppPackageFileAsync(packagePath)

## Duong dan thuc te theo nen tang

1. Android (emulator/device)

- /data/user/0/com.companyname.foodmarketnarrator/files/audio_cache

2. Windows (target Windows)

- Nam duoi LocalAppData cua user, trong vung du lieu ung dung cua app, co thu muc audio_cache

3. iOS

- Nam trong sandbox cua app (AppData/Library), co thu muc audio_cache

## Luu y van hanh

- App chi phat offline duoc voi nhung file audio da co san trong package hoac da tai thanh cong truoc do.
- Neu chua co file trong cache va cung khong co trong package, app can mang de tai file audio.

## Kiem tra nhanh

1. Mo app va phat thu 1 audio (de kich hoat luu cache)
2. Dung file explorer/phuong tien debug cua nen tang de vao audio_cache
3. Kiem tra co file moi duoc tao, kich thuoc > 0
4. Tat mang, phat lai cung audio do de xac nhan offline playback
