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

        public (Music currentMusic, float position) State => (CurrentMusic, AudioSource.time);

        public Music CurrentMusic { get; private set; }
        public IMusicStore MusicStore { get; set; }
        bool fadingOut;

        static public MusicManager Instance { get; private set; }

        void Awake()
        {
            Instance = this;
        }

        public void Play(string musicName, float fadeOutDuration = 1f, float position = 0f)
        {
            Play(MusicStore?.Get(musicName), fadeOutDuration: fadeOutDuration, position: position);
        }

        public void Play(MusicSymbol symbol, float fadeOutDuration = 1f, float position = 0f)
        {
            if (symbol)
            {
                Play(MusicStore?.Get(symbol.name), fadeOutDuration: fadeOutDuration, position: position);
            }
            else
            {
                Play(default(Music), fadeOutDuration: fadeOutDuration);
            }
        }

        public async void Play(Music music, float fadeOutDuration = 1f, float position = 0f)
        {
            if (CurrentMusic == music)
            {
                return;
            }

            if (music && music.Asset)
            {
                music.Asset.LoadAudioData();
            }
            CurrentMusic = music;

            if (fadingOut)
            {
                return;
            }

            await FadeOutInternal(fadeOutDuration);

            var previousClip = AudioSource.clip;
            if (previousClip)
            {
                var currentMusicAsset = CurrentMusic ? CurrentMusic.Asset : null;
                if (previousClip != currentMusicAsset)
                {
                    previousClip.UnloadAudioData();
                }
            }

            if (CurrentMusic)
            {
                AudioSource.clip = CurrentMusic.Asset;
                AudioSource.volume = CurrentMusic.Volume;
                AudioSource.time = position;
                AudioSource.loop = CurrentMusic.Loop;
                AudioSource.Play();
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

        public UniTask FadeOut(float fadeOutDutation)
        {
            Play(default(Music), fadeOutDutation);
            return UniTask.WaitWhile(() => fadingOut);
        }

        async UniTask FadeOutInternal(float duration)
        {
            if (fadingOut)
            {
                await UniTask.WaitWhile(() => fadingOut);
                return;
            }

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

        public AudioMixer AudioMixer => AudioSource.outputAudioMixerGroup.audioMixer;
    }
}
