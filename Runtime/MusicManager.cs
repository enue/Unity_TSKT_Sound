using UnityEngine;
using System.Collections;
using UniRx.Async;

namespace TSKT
{
    [RequireComponent(typeof(AudioSource), typeof(AudioListener))]
    public class MusicManager : MonoBehaviour
    {
        AudioSource audioSource;
        AudioSource AudioSource => audioSource ?? (audioSource = GetComponent<AudioSource>());

        public Music CurrentMusic { get; private set; }
        public IMusicStore MusicStore { get; set; }
        bool fadingOut;
        int generation;

        static public MusicManager Instance { get; private set; }

        void Awake()
        {
            Instance = this;
        }

        public void Play(string musicName)
        {
            Play(MusicStore?.Get(musicName));
        }

        public void Play(MusicSymbol symbol)
        {
            if (symbol)
            {
                Play(MusicStore?.Get(symbol.name));
            }
            else
            {
                Play(default(Music));
            }
        }

        public async void Play(Music music)
        {
            if (CurrentMusic == music)
            {
                return;
            }

            if (!fadingOut)
            {
                FadeOut(1f).Forget();
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

                await Tween.SoundVolume(gameObject, duration).To(0f).UniTask;
                AudioSource.Stop();

                fadingOut = false;
            }
        }

        public UnityEngine.Audio.AudioMixer AudioMixer => AudioSource.outputAudioMixerGroup.audioMixer;
    }
}
