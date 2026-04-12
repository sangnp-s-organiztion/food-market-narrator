#if ANDROID
using Android.Content;
using Android.Media;
using Microsoft.Maui.ApplicationModel;

namespace food_market_narrator.Services;

public partial class AudioService
{
    private AudioManager? _platformAudioManager;
    private PlatformAudioFocusListener? _platformAudioFocusListener;
    private AudioFocusRequestClass? _platformAudioFocusRequest;


    // hàm này được dùng để khởi tạo các thành phần liên quan đến quản lý audio trên nền tảng Android, bao gồm AudioManager để quản lý các thiết bị âm thanh và AudioFocusListener để lắng nghe các sự kiện thay đổi về quyền truy cập audio. Hàm này sẽ được gọi khi khởi tạo AudioService để đảm bảo rằng app có thể xử lý các tình huống như cuộc gọi đến hoặc người dùng mở một app khác đang phát audio.
    partial void InitializePlatformInterruptionHandling()
    {
        var context = Android.App.Application.Context;
        _platformAudioManager = context.GetSystemService(Context.AudioService) as AudioManager;
        _platformAudioFocusListener = new PlatformAudioFocusListener(this);
    }

    // hàm này được dùng để yêu cầu quyền truy cập audio khi app cần phát audio. Nó sẽ sử dụng AudioManager để yêu cầu quyền truy cập và sẽ xử lý các trường hợp khác nhau dựa trên phiên bản Android. Nếu yêu cầu thành công, app sẽ có quyền phát audio. Nếu không, app có thể cần phải xử lý tình huống khi không có quyền truy cập audio.
    partial void RequestPlatformAudioFocus()
    {
        if (_platformAudioManager == null || _platformAudioFocusListener == null)
        {
            return;
        }

        if (OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            if (_platformAudioFocusRequest == null)
            {
                var attributesBuilder = new AudioAttributes.Builder();
                if (attributesBuilder == null)
                {
                    return;
                }

                var usageBuilder = attributesBuilder.SetUsage(AudioUsageKind.Media);
                if (usageBuilder == null)
                {
                    return;
                }

                var contentBuilder = usageBuilder.SetContentType(AudioContentType.Speech);
                if (contentBuilder == null)
                {
                    return;
                }

                var attributes = contentBuilder.Build();

                if (attributes == null)
                {
                    return;
                }

                var focusRequestBuilder = new AudioFocusRequestClass.Builder(AudioFocus.Gain);
                if (focusRequestBuilder == null)
                {
                    return;
                }

                var audioAttributesBuilder = focusRequestBuilder.SetAudioAttributes(attributes);
                if (audioAttributesBuilder == null)
                {
                    return;
                }

                var duckBuilder = audioAttributesBuilder.SetWillPauseWhenDucked(true);
                if (duckBuilder == null)
                {
                    return;
                }

                var listenerBuilder = duckBuilder.SetOnAudioFocusChangeListener(_platformAudioFocusListener);
                if (listenerBuilder == null)
                {
                    return;
                }

                _platformAudioFocusRequest = listenerBuilder.Build();
            }

            if (_platformAudioFocusRequest == null)
            {
                return;
            }

            _platformAudioManager.RequestAudioFocus(_platformAudioFocusRequest);
            return;
        }

#pragma warning disable CA1422
        _platformAudioManager.RequestAudioFocus(
            _platformAudioFocusListener,
            Android.Media.Stream.Music,
            AudioFocus.Gain);
#pragma warning restore CA1422
    }

    // Nhả audio focus khi dừng phát để ứng dụng khác có thể lấy quyền phát audio.
    partial void ReleasePlatformAudioFocus()
    {
        if (_platformAudioManager == null || _platformAudioFocusListener == null)
        {
            return;
        }

        if (OperatingSystem.IsAndroidVersionAtLeast(26) && _platformAudioFocusRequest != null)
        {
            _platformAudioManager.AbandonAudioFocusRequest(_platformAudioFocusRequest);
            return;
        }

#pragma warning disable CA1422
        _platformAudioManager.AbandonAudioFocus(_platformAudioFocusListener);
#pragma warning restore CA1422
    }

    // Xử lý callback mất audio focus: dừng phát để tránh chồng âm thanh với app/hệ thống khác.
    private void HandlePlatformAudioFocusChanged(AudioFocus focusChange)
    {
        if (focusChange is AudioFocus.Loss or AudioFocus.LossTransient or AudioFocus.LossTransientCanDuck)
        {
            MainThread.BeginInvokeOnMainThread(StopForPlatformInterruption);
        }
    }

    private sealed class PlatformAudioFocusListener : Java.Lang.Object, AudioManager.IOnAudioFocusChangeListener
    {
        private readonly AudioService _owner;

        // Listener bridge từ Android AudioManager về AudioService.
        public PlatformAudioFocusListener(AudioService owner)
        {
            _owner = owner;
        }

        // Nhận event audio focus thay đổi từ hệ điều hành và chuyển sang owner xử lý.
        public void OnAudioFocusChange(AudioFocus focusChange)
        {
            _owner.HandlePlatformAudioFocusChanged(focusChange);
        }
    }
}
#endif
