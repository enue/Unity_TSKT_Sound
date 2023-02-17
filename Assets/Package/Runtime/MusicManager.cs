using UnityEngine;
using System.Collections;
using UnityEngine.Audio;
using Cysharp.Threading.Tasks;
using System;
#nullable enable

namespace TSKT
{
    [RequireComponent(typeof(AudioSource))]
    public class MusicManager : MonoBehaviour
    {
        [SerializeField]
        AudioMixerSnapshot? defaultSnapshot = default;

        [SerializeField]
        AudioMixerSnapshot? muteSnapshot = default;

        AudioSource? audioSource;
        AudioSource AudioSource => audioSource ? audioSource! : (audioSource = GetComponent<AudioSource>());

        public MusicManagers.State State => new MusicManagers.State(this, AudioSource.time);
        public bool IsPlaying => AudioSource.isPlaying;

        public Music? CurrentMusic { get; private set; }
        public IMusicStore? MusicStore { get; set; }

        static public MusicManager? Instance { get; private set; }
        System.Threading.CancellationTokenSource? cancellationTokenSource;

        void Awake()
        {
            Instance = this;
        }
        void OnDestroy()
        {
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;
        }

        public void Play(string musicName, float fadeOutDuration = 1f, float position = 0f, float fadeInDuration = 0f)
        {
            Play(MusicStore?.Get(musicName), fadeOutDuration: fadeOutDuration, position: position, fadeInDuration: fadeInDuration);
        }

        public void Play(MusicSymbol symbol, float fadeOutDuration = 1f, float position = 0f, float fadeInDuration = 0f)
        {
            if (symbol)
            {
                Play(MusicStore?.Get(symbol.name), fadeOutDuration: fadeOutDuration, position: position, fadeInDuration: fadeInDuration);
            }
            else
            {
                Play((Music?)null, fadeOutDuration: fadeOutDuration);
            }
        }

        public void Play(Music? music, float fadeOutDuration = 1f, float position = 0f, float fadeInDuration = 0f)
        {
            PlayInternal(music, fadeOutDuration, position, fadeInDuration).Forget();
        }
        async UniTask PlayInternal(Music? music, float fadeOutDuration = 1f, float position = 0f, float fadeInDuration = 0f)
        {
            if (CurrentMusic == music)
            {
                return;
            }
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = new();
            if (music && music!.Asset)
            {
                music.Asset!.LoadAudioData();
            }
            CurrentMusic = music;

            await FadeOutInternal(fadeOutDuration, cancellationTokenSource.Token);

            var previousClip = AudioSource.clip;
            if (previousClip)
            {
                var currentMusicAsset = CurrentMusic ? CurrentMusic!.Asset : null;
                if (previousClip != currentMusicAsset)
                {
                    previousClip.UnloadAudioData();
                }
            }

            if (CurrentMusic)
            {
                AudioSource.clip = CurrentMusic!.Asset;
                AudioSource.time = position;
                AudioSource.loop = CurrentMusic.Loop;
                AudioSource.Play();

                if (muteSnapshot)
                {
                    AudioSource.volume = CurrentMusic.Volume;
                    Debug.Assert(defaultSnapshot, "require defaultSnapshot");
                    if (defaultSnapshot)
                    {
                        defaultSnapshot!.TransitionTo(fadeInDuration);
                    }
                }
                else
                {
                    await TweenVolume(0f, CurrentMusic.Volume, fadeInDuration, cancellationTokenSource.Token);
                }
            }
            else
            {
                AudioSource.clip = null;
            }
        }

        public void Stop()
        {
            FadeOut(0f).Forget();
        }

        public async UniTask FadeOut(float fadeOutDutation)
        {
            try
            {
                await PlayInternal(null, fadeOutDutation);
            }
            catch (OperationCanceledException)
            {
                // nop
            }
        }

        async UniTask FadeOutInternal(float duration, System.Threading.CancellationToken cancellationToken)
        {
            if (AudioSource.isPlaying)
            {
                if (muteSnapshot)
                {
                    Debug.Assert(defaultSnapshot, "require defaultSnapshot");
                    muteSnapshot!.TransitionTo(duration);

                    switch (muteSnapshot.audioMixer.updateMode)
                    {
                        case AudioMixerUpdateMode.Normal:
                            await UniTask.Delay((int)(1000f * duration), ignoreTimeScale: false, cancellationToken: cancellationToken);
                            break;
                        case AudioMixerUpdateMode.UnscaledTime:
                            await UniTask.Delay((int)(1000f * duration), ignoreTimeScale: true, cancellationToken: cancellationToken);
                            break;
                        default:
                            Debug.LogError("unknown updateMode : " + muteSnapshot.audioMixer.updateMode);
                            break;
                    }
                }
                else
                {
                    await TweenVolume(AudioSource.volume, 0f, duration, cancellationToken);
                }
                AudioSource.Stop();
            }
        }

        public AudioMixer AudioMixer => AudioSource.outputAudioMixerGroup.audioMixer;

        async UniTask TweenVolume(float from, float to, float duration, System.Threading.CancellationToken cancellationToken)
        {
            if (duration == 0f)
            {
                AudioSource.volume = to;
                return;
            }

            var startedTime = Time.realtimeSinceStartup;
            while (true)
            {
                await UniTask.Yield(cancellationToken);

                if (!AudioSource)
                {
                    break;
                }
                var elapsedTime = Time.realtimeSinceStartup - startedTime;
                var t = Mathf.Clamp01(elapsedTime / duration);
                AudioSource.volume = Mathf.Lerp(from, to, t);
                if (t >= 1f)
                {
                    break;
                }
            }
        }
    }
}
