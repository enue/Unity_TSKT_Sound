using UnityEngine;
using UnityEngine.Audio;
using System;
#nullable enable

namespace TSKT
{
    [RequireComponent(typeof(AudioSource))]
    public class MusicManager : MonoBehaviour
    {
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

        // AudioSource.volume = baseVolume * fade
        // baseVolumeはAudioSourceが今鳴らしている曲の音量、fadeはフェードの進み具合(0～1)
        float baseVolume = 1f;
        float fade = 1f;

        // AudioDataUsageに使用中として届けてある、AudioSourceが今鳴らしているリソース
        AudioResource? usingResource;

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
            baseVolume = AudioSource.volume;
        }
        void OnDestroy()
        {
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;

            Sounds.AudioDataUsage.Release(usingResource);
            usingResource = null;

            if (ReferenceEquals(Instance, this))
            {
                Instance = null;
            }
        }

        /// <summary>
        /// 再生中の曲をフェードアウトしてから曲を再生する。
        /// 返すAwaitableはフェードインまで終わるか、後続のPlayやStopに割り込まれた時点で完了する。待たなくてもよい。
        /// </summary>
        /// <param name="forceRestart">trueなら、同じ曲を再生中でも再生し直す</param>
        public async Awaitable Play(Music? music, float fadeOutDuration = 1f, float position = 0f, float fadeInDuration = 0f, bool forceRestart = false)
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

        public Awaitable Stop(float fadeOutDuration = 0f)
        {
            return Play(null, fadeOutDuration);
        }

        async Awaitable PlayInternal(Music? music, float fadeOutDuration, float position, float fadeInDuration, bool forceRestart)
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
            var cancellationToken = cancellationTokenSource.Token;

            CurrentMusic = music;
            pendingPosition = position;
            transitioning = true;

            // フェードアウトの間に次の曲を読み込んでおく。
            // フェードアウト中に割り込まれたら、finallyで使用中の届けを取り消す
            var asset = music ? music!.Asset : null;
            Sounds.AudioDataUsage.Acquire(asset);
            var handedOver = false;
            try
            {
                if (asset is AudioClip clip && clip)
                {
                    clip.LoadAudioData();
                }

                if (AudioSource.isPlaying)
                {
                    await TweenFade(0f, fadeOutDuration, cancellationToken);
                    AudioSource.Stop();
                }

                AudioSource.resource = asset;
                Sounds.AudioDataUsage.Release(usingResource);
                usingResource = asset;
                handedOver = true;
            }
            finally
            {
                if (!handedOver)
                {
                    Sounds.AudioDataUsage.Release(asset);
                }
            }

            if (music)
            {
                AudioSource.time = position;
                AudioSource.loop = music.Loop;
                baseVolume = music.Volume;
                SetFade(fadeInDuration > 0f ? 0f : 1f);
                AudioSource.Play();
                transitioning = false;

                await TweenFade(1f, fadeInDuration, cancellationToken);
            }
            else
            {
                transitioning = false;
            }
        }

        public AudioMixer AudioMixer => AudioSource.outputAudioMixerGroup.audioMixer;

        void SetFade(float value)
        {
            fade = value;
            AudioSource.volume = baseVolume * fade;
        }

        /// <summary>
        /// 現在のフェード値からtoまで、実時間でdurationかけて変化させる。
        /// </summary>
        async Awaitable TweenFade(float to, float duration, System.Threading.CancellationToken cancellationToken)
        {
            var from = fade;
            if (duration > 0f && from != to)
            {
                var startedTime = Time.realtimeSinceStartup;
                while (true)
                {
                    await Awaitable.NextFrameAsync(cancellationToken);

                    var elapsedTime = Time.realtimeSinceStartup - startedTime;
                    var t = Mathf.Clamp01(elapsedTime / duration);
                    SetFade(Mathf.Lerp(from, to, t));
                    if (t >= 1f)
                    {
                        break;
                    }
                }
            }
            SetFade(to);
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
