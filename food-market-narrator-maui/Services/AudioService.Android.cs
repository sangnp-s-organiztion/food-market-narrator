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

    partial void InitializePlatformInterruptionHandling()
    {
        var context = Android.App.Application.Context;
        _platformAudioManager = context.GetSystemService(Context.AudioService) as AudioManager;
        _platformAudioFocusListener = new PlatformAudioFocusListener(this);
    }

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

        public PlatformAudioFocusListener(AudioService owner)
        {
            _owner = owner;
        }

        public void OnAudioFocusChange(AudioFocus focusChange)
        {
            _owner.HandlePlatformAudioFocusChanged(focusChange);
        }
    }
}
#endif
