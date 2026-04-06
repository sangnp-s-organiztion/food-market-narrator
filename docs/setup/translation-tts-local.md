# Translation + TTS Local Setup (Saler)

TÃ i liá»‡u nÃ y mÃ´ táº£ Ä‘áº§y Ä‘á»§ flow local:

1. Saler gá»­i text lÃªn backend.
2. Backend gá»i LibreTranslate (`localhost:5000/translate`) Ä‘á»ƒ dá»‹ch.
3. Backend gá»i Edge TTS service (`localhost:6000/synthesize`) Ä‘á»ƒ táº¡o mp3.
4. Backend lÆ°u file vÃ o `/uploads/audios` vÃ  táº¡o báº£n ghi `Audio` trong MSSQL.
5. Backend lÆ°u lá»‹ch sá»­ billing vÃ o MongoDB collections:
   - `TranslationJobs`
   - `TranslationUsageLedger`
   - `AudioTranslationVersions`
   - `TranslationBillingMonthly`

## 1) YÃªu cáº§u trÆ°á»›c khi cháº¡y

- LibreTranslate container Ä‘Ã£ cháº¡y táº¡i cá»•ng `5000`.
- SQL Server + MongoDB Ä‘Ã£ sáºµn sÃ ng.
- Backend API cháº¡y táº¡i `http://localhost:5044`.

LÆ°u Ã½ báº¯t buá»™c:

- LibreTranslate pháº£i cÃ³ Ä‘á»§ ngÃ´n ngá»¯ `vi`, `en`, `ja`, `ko`, `zh` trong endpoint `/languages`.
- Náº¿u `/languages` khÃ´ng cÃ³ cÃ¡c mÃ£ nÃ y, chá»©c nÄƒng dá»‹ch sáº½ tráº£ lá»—i Ä‘Ãºng nguyÃªn nhÃ¢n tá»« backend.

VÃ­ dá»¥ cháº¡y láº¡i LibreTranslate vá»›i bá»™ ngÃ´n ngá»¯ yÃªu cáº§u:

```bash
docker rm -f libretranslate
docker run -d --name libretranslate -p 5000:5000 \
  -e LT_LOAD_ONLY=en,vi,ja,ko,zh \
  libretranslate/libretranslate:latest
```

Kiá»ƒm tra nhanh:

```bash
curl http://localhost:5000/languages
```

## 2) Cháº¡y Edge TTS service báº±ng Docker

Tá»« thÆ° má»¥c root project:

```bash
docker compose --env-file docker-compose.tts.env -f docker-compose.tts.yml up -d --build
```

Máº·c Ä‘á»‹nh service báº­t fallback `gTTS` qua biáº¿n mÃ´i trÆ°á»ng `EDGE_TTS_ENABLE_GTTS_FALLBACK=true`.
Khi Edge TTS bá»‹ cháº·n websocket (403), service váº«n tráº£ mp3 báº±ng fallback Ä‘á»ƒ luá»“ng táº¡o audio khÃ´ng bá»‹ giÃ¡n Ä‘oáº¡n.

Kiá»ƒm tra health:

```bash
curl http://localhost:6000/health
```

Ká»³ vá»ng:

```json
{ "status": "ok" }
```

Kiá»ƒm tra engine Ä‘ang dÃ¹ng:

```bash
curl -i -X POST "http://localhost:6000/synthesize" \
  -H "Content-Type: application/json" \
  -d '{"text":"Xin chao","language_code":"vi"}'
```

Header pháº£n há»“i:

- `x-tts-engine: edge` náº¿u dÃ¹ng Edge thÃ nh cÃ´ng.
- `x-tts-engine: gtts` náº¿u Ä‘ang fallback.

## 3) Cáº¥u hÃ¬nh backend

File `FoodMarketNarrator.Api/appsettings.json` Ä‘Ã£ cÃ³ sáºµn:

- `LibreTranslate.BaseUrl = http://localhost:5000`
- `EdgeTts.BaseUrl = http://localhost:6000`
- `TranslationPricing.PricePer1KChars = 0.02`

Náº¿u báº¡n Ä‘á»•i cá»•ng/container thÃ¬ chá»‰nh láº¡i cÃ¡c giÃ¡ trá»‹ nÃ y.

## 4) API má»›i cho saler

### 4.1 Dá»‹ch vÄƒn báº£n

- Method: `POST`
- URL: `/Restaurant/{restaurantId}/translate`
- Auth: cookie saler (báº¯t buá»™c)

Body:

```json
{
  "text": "Ná»™i dung cáº§n dá»‹ch",
  "sourceLanguageCode": "vi",
  "targetLanguageCode": "en"
}
```

Response máº«u:

```json
{
  "requestId": "f0f8eaf7d9fd4e73b3f0fce5846f0af4",
  "sourceLanguageCode": "vi",
  "targetLanguageCode": "en",
  "translatedText": "Text translated to English",
  "inputChars": 20,
  "outputChars": 26,
  "estimatedCost": 0.0004,
  "currency": "USD"
}
```

### 4.2 Táº¡o audio tá»« text

- Method: `POST`
- URL: `/Restaurant/{restaurantId}/audios/from-text`
- Auth: cookie saler (báº¯t buá»™c)

Body:

```json
{
  "text": "Text to synthesize",
  "languageCode": "en",
  "sourceText": "Ná»™i dung gá»‘c tiáº¿ng Viá»‡t"
}
```

Response máº«u:

```json
{
  "requestId": "5eb296db7b2642bb96c58b691e8b95a0",
  "audioId": 123,
  "audioUrl": "/uploads/audios/tts_en_abc.mp3",
  "languageCode": "en",
  "voice": "en-US-JennyNeural",
  "createdAt": "2026-04-06T10:20:00.000Z"
}
```

## 5) CÃ¡ch test nhanh báº±ng curl

### Dá»‹ch

```bash
curl -X POST "http://localhost:5044/Restaurant/chilli-bbq-hotpot-restaurant/translate" \
  -H "Content-Type: application/json" \
  -b "fmn_saler_auth=<cookie>" \
  -d '{"text":"Xin chÃ o","sourceLanguageCode":"vi","targetLanguageCode":"en"}'
```

### Táº¡o audio

```bash
curl -X POST "http://localhost:5044/Restaurant/chilli-bbq-hotpot-restaurant/audios/from-text" \
  -H "Content-Type: application/json" \
  -b "fmn_saler_auth=<cookie>" \
  -d '{"text":"Hello from Food Market Narrator","languageCode":"en"}'
```

## 6) UI saler Ä‘Ã£ tÃ­ch há»£p

Trang `saler/src/pages/AudioPage.tsx` Ä‘Ã£ cÃ³:

- textarea nháº­p vÄƒn báº£n
- chá»n ngÃ´n ngá»¯ nguá»“n/Ä‘Ã­ch (Anh, Viá»‡t, Nháº­t, Trung, HÃ n)
- nÃºt `Dá»‹ch`
- nÃºt `Táº¡o audio`
- vÃ¹ng hiá»ƒn thá»‹ káº¿t quáº£ dá»‹ch
- nÃºt `Play audio` cho audio má»›i táº¡o

## 7) LÆ°u Ã½ váº­n hÃ nh

- Náº¿u LibreTranslate khÃ´ng pháº£n há»“i, API tráº£ `502`.
- Náº¿u Edge TTS service khÃ´ng pháº£n há»“i hoÃ n toÃ n, API tráº£ `502`.
- Náº¿u Edge TTS upstream tráº£ lá»—i 403, service tá»± fallback qua `gTTS` (náº¿u báº­t env fallback).
- Náº¿u seller khÃ´ng sá»Ÿ há»¯u restaurant, API tráº£ `403`.
- Náº¿u language chÆ°a cÃ³ trong báº£ng `Languages` MSSQL, API tráº£ `400`.
