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

        public MusicManagers.State State => new(this, AudioSource.time);
        public bool IsPlaying => AudioSource.isPlaying;

        public Music? CurrentMusic { get; private set; }
        public IMusicStore? MusicStore { get; set; }

        public static MusicManager? Instance { get; private set; }
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

            await FadeOut(AudioSource, fadeOutDuration, muteSnapshot, cancellationTokenSource.Token);

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
                    UnityEngine.Assertions.Assert.IsTrue(defaultSnapshot, "require defaultSnapshot");
                    defaultSnapshot!.TransitionTo(fadeInDuration);
                }
                else
                {
                    await TweenVolume(AudioSource, 0f, CurrentMusic.Volume, fadeInDuration, cancellationTokenSource.Token);
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

        public AudioMixer AudioMixer => AudioSource.outputAudioMixerGroup.audioMixer;

        static async UniTask FadeOut(AudioSource target, float duration, AudioMixerSnapshot? muteSnapshot , System.Threading.CancellationToken cancellationToken)
        {
            if (target.isPlaying)
            {
                if (muteSnapshot)
                {
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
                    await TweenVolume(target, target.volume, 0f, duration, cancellationToken);
                }
                target.Stop();
            }
        }

        static async UniTask TweenVolume(AudioSource target, float from, float to, float duration, System.Threading.CancellationToken cancellationToken)
        {
            if (duration == 0f)
            {
                target.volume = to;
                return;
            }

            var startedTime = Time.realtimeSinceStartup;
            while (true)
            {
                await UniTask.Yield(cancellationToken);

                if (!target)
                {
                    break;
                }
                var elapsedTime = Time.realtimeSinceStartup - startedTime;
                var t = Mathf.Clamp01(elapsedTime / duration);
                target.volume = Mathf.Lerp(from, to, t);
                if (t >= 1f)
                {
                    break;
                }
            }
        }

        public async Awaitable WaitEndOfCurrentMusic()
        {
            var current = CurrentMusic;
            if (!current)
            {
                return;
            }
            while (true)
            {
                if (CurrentMusic != current)
                {
                    return;
                }
                if (!AudioSource.isPlaying)
                {
                    return;
                }
                await Awaitable.NextFrameAsync();
            }
        }
    }
}
