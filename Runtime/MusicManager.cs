using UnityEngine;
using System.Collections;
using UniRx.Async;
using UnityEngine.Audio;

namespace TSKT
{
    [RequireComponent(typeof(AudioSource))]
    public class MusicManager : MonoBehaviour
    {
        [SerializeField]
        AudioMixerSnapshot defaultSnapshot = default;

        [SerializeField]
        AudioMixerSnapshot muteSnapshot = default;

        AudioSource audioSource;
        AudioSource AudioSource => audioSource ? audioSource : (audioSource = GetComponent<AudioSource>());

        public Music CurrentMusic { get; private set; }
        public IMusicStore MusicStore { get; set; }
        bool fadingOut;
        int generation;

        static public MusicManager Instance { get; private set; }

        void Awake()
        {
            Instance = this;
        }

        public void Play(string musicName, float fadeOutDuration = 1f)
        {
            Play(MusicStore?.Get(musicName), fadeOutDuration);
        }

        public void Play(MusicSymbol symbol, float fadeOutDuration = 1f)
        {
            if (symbol)
            {
                Play(MusicStore?.Get(symbol.name), fadeOutDuration);
            }
            else
            {
                Play(default(Music));
            }
        }

        public async void Play(Music music, float fadeOutDuration = 1f)
        {
            if (CurrentMusic == music)
            {
                return;
            }

            if (!fadingOut)
            {
                FadeOut(fadeOutDuration).Forget();
            }

            ++generation;
            var thisTaskGeneration = generation;

            CurrentMusic = music;

            music.Asset.LoadAudioData();
            await UniTask.WaitWhile(() => music.Asset.loadState != AudioDataLoadState.Loaded);
            await UniTask.WaitWhile(() => AudioSource.isPlaying);

            if (generation == thisTaskGeneration)
            {
                if (AudioSource.clip
                    && (AudioSource.clip != music.Asset))
                {
                    var previousClip = AudioSource.clip;
                    previousClip.UnloadAudioData();
                }

                AudioSource.clip = music.Asset;
                AudioSource.volume = music.Volume;
                AudioSource.time = 0f;
                AudioSource.loop = music.Loop;
                AudioSource.Play();
            }
        }

        public void Stop()
        {
            FadeOut(0f).Forget();
        }

        public async UniTask FadeOut(float duration)
        {
            ++generation;

            CurrentMusic = null;
            if (AudioSource.isPlaying)
            {
                fadingOut = true;

                if (muteSnapshot)
                {
                    Debug.Assert(defaultSnapshot, "require defaultSnapshot");
                    muteSnapshot.TransitionTo(duration);

                    switch (muteSnapshot.audioMixer.updateMode)
                    {
                        case AudioMixerUpdateMode.Normal:
                            await UniTask.Delay((int)(1000f * duration), ignoreTimeScale: false);
                            break;
                        case AudioMixerUpdateMode.UnscaledTime:
                            await UniTask.Delay((int)(1000f * duration), ignoreTimeScale: true);
                            break;
                        default:
                            Debug.LogError("unknown updateMode : " + muteSnapshot.audioMixer.updateMode);
                            break;
                    }
                }
                else
                {
                    await Tween.SoundVolume(AudioSource, duration).To(0f).UniTask;
                }
                AudioSource.Stop();

                if (defaultSnapshot)
                {
                    Debug.Assert(muteSnapshot, "require muteSnapshot");
                    defaultSnapshot.TransitionTo(0f);
                }

                fadingOut = false;
            }
        }

        public UnityEngine.Audio.AudioMixer AudioMixer => AudioSource.outputAudioMixerGroup.audioMixer;
    }
}
