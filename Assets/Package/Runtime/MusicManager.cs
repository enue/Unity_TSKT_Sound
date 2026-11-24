using UnityEngine;
using System.Collections;
using UnityEngine.Audio;
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

        // 曲の切り替え中はAudioSourceがまだ前の曲を鳴らしているので、AudioSource.timeではなく予定の再生位置を返す
        bool transitioning;
        float pendingPosition;
        public MusicManagers.State State => new(this, transitioning ? pendingPosition : AudioSource.time);
        public bool IsPlaying => AudioSource.isPlaying;

        public Music? CurrentMusic { get; private set; }

        public static MusicManager? Instance { get; private set; }

        System.Threading.CancellationTokenSource? cancellationTokenSource;

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Init()
        {
            Instance = null;
        }
#endif

        void Awake()
        {
            Instance = this;
        }
        void OnDestroy()
        {
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;

            if (ReferenceEquals(Instance, this))
            {
                Instance = null;
            }
        }

        /// <param name="forceRestart">trueなら、同じ曲を再生中でも再生し直す</param>
        public void Play(Music? music, float fadeOutDuration = 1f, float position = 0f, float fadeInDuration = 0f, bool forceRestart = false)
        {
            _ = PlayAndLogException(music, fadeOutDuration, position, fadeInDuration, forceRestart);
        }
        async Awaitable PlayAndLogException(Music? music, float fadeOutDuration, float position, float fadeInDuration, bool forceRestart)
        {
            try
            {
                await PlayInternal(music, fadeOutDuration, position, fadeInDuration, forceRestart);
            }
            catch (OperationCanceledException)
            {
                // nop
            }
            catch (Exception ex)
            {
                // 待つ人がいないAwaitableの例外はどこにも出ないので、ここでログに出す
                Debug.LogException(ex);
            }
        }
        async Awaitable PlayInternal(Music? music, float fadeOutDuration = 1f, float position = 0f, float fadeInDuration = 0f, bool forceRestart = false)
        {
            if (CurrentMusic == music && !forceRestart)
            {
                // ループしない曲が最後まで再生し終わっている場合は、同じ曲でも再生し直す
                var finished = music && !music!.Loop && !AudioSource.isPlaying;
                if (!finished)
                {
                    return;
                }
            }
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = new();
            if (music && music!.Asset && music.Asset is AudioClip clip)
            {
                clip.LoadAudioData();
            }
            CurrentMusic = music;
            pendingPosition = position;
            transitioning = true;

            await FadeOut(AudioSource, fadeOutDuration, muteSnapshot, cancellationTokenSource.Token);

            if (AudioSource.resource is AudioClip previousClip && previousClip)
            {
                var currentMusicAsset = CurrentMusic ? CurrentMusic!.Asset : null;
                if (previousClip != currentMusicAsset)
                {
                    previousClip.UnloadAudioData();
                }
            }

            if (CurrentMusic)
            {
                AudioSource.resource = CurrentMusic!.Asset;
                AudioSource.time = position;
                AudioSource.loop = CurrentMusic.Loop;
                AudioSource.Play();
                transitioning = false;

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
                AudioSource.resource = null;
                transitioning = false;
            }
        }

        public void Stop()
        {
            _ = PlayAndLogException(null, 0f, 0f, 0f, false);
        }

        public async Awaitable FadeOut(float fadeOutDuration)
        {
            try
            {
                await PlayInternal(null, fadeOutDuration);
            }
            catch (OperationCanceledException)
            {
                // nop
            }
        }

        public AudioMixer AudioMixer => AudioSource.outputAudioMixerGroup.audioMixer;

        static async Awaitable FadeOut(AudioSource target, float duration, AudioMixerSnapshot? muteSnapshot, System.Threading.CancellationToken cancellationToken)
        {
            if (target.isPlaying)
            {
                if (muteSnapshot)
                {
                    muteSnapshot!.TransitionTo(duration);

                    switch (muteSnapshot.audioMixer.updateMode)
                    {
                        case AudioMixerUpdateMode.Normal:
                            await Awaitable.WaitForSecondsAsync(duration, cancellationToken);
                            break;
                        case AudioMixerUpdateMode.UnscaledTime:
                            await WaitForSecondsRealtimeAsync(duration, cancellationToken);
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

        static async Awaitable TweenVolume(AudioSource target, float from, float to, float duration, System.Threading.CancellationToken cancellationToken)
        {
            if (duration == 0f)
            {
                target.volume = to;
                return;
            }
            var startedTime = Time.realtimeSinceStartup;

            await Awaitable.NextFrameAsync(cancellationToken);
            while (target)
            {
                var elapsedTime = Time.realtimeSinceStartup - startedTime;
                var t = Mathf.Clamp01(elapsedTime / duration);
                target.volume = Mathf.Lerp(from, to, t);
                if (t >= 1f)
                {
                    break;
                }
                await Awaitable.NextFrameAsync(cancellationToken);
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
        static async Awaitable WaitForSecondsRealtimeAsync(float seconds, System.Threading.CancellationToken cancellationToken)
        {
            var endTime = Time.realtimeSinceStartupAsDouble + seconds;
            while (Time.realtimeSinceStartupAsDouble < endTime)
            {
                await Awaitable.NextFrameAsync(cancellationToken);
            }
        }
    }
}
