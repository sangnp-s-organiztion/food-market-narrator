THƯ MỤC: narration/tts/

MỤC ĐÍCH:
- Chứa TTS (Text-to-Speech) service implementation
- Tích hợp với TTS API providers (Azure, Google, etc.)
- Chuyển đổi text script thành âm thanh

NỘI DUNG:
- TtsService.cs - Service TTS chính
- Providers/ - Các TTS providers
- Config/ - Cấu hình TTS
- Utilities/ - Utility cho TTS

VÍ DỤ:
- AzureTtsProvider.cs - Azure Speech Services
- GoogleTtsProvider.cs - Google Cloud TTS
- OpenAiTtsProvider.cs - OpenAI TTS
- LanguageSupport.cs - Hỗ trợ ngôn ngữ

CẤU HÌNH:
- API keys cho TTS provider
- Voice preferences (giọng nữ/nam, accent)
- Output format (mp3, wav, etc.)
- Speed, pitch settings

GHI CHÚ:
- Cần API key để sử dụng TTS service
- Kiểm tra hóa đơn sử dụng TTS thường xuyên
- Đơn giá TTS có thể cao, cần tối ưu
