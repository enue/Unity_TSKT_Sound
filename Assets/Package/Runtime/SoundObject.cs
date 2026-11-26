using UnityEngine;
using UnityEngine.Audio;
#nullable enable

namespace TSKT
{
    [RequireComponent(typeof(AudioSource))]
    public class SoundObject : MonoBehaviour
    {
        AudioSource? audioSource;
        AudioSource AudioSource => audioSource ? audioSource! : (audioSource = GetComponent<AudioSource>());

        float startedTime;
        public float ElapsedTime => Time.realtimeSinceStartup - startedTime;

        public string? Channel { get; private set; }
        public int Priority { get; private set; }

        /// <summary>
        /// Playのたびに増える。SoundPlayer.Handleが、自分の再生した音がまだこのSoundObjectにあるかを判定するのに使う。
        /// </summary>
        public int Generation { get; private set; }

        float? spatialBlend;
        public void Set3D(bool is3D)
        {
            spatialBlend ??= AudioSource.spatialBlend;
            if (is3D)
            {
                AudioSource.spatialBlend = spatialBlend.Value;
            }
            else
            {
                AudioSource.spatialBlend = 0f;
            }
        }

        void OnDisable()
        {
            // SoundPlayerはResourceが空のSoundObjectを空きとして使い回す
            if (audioSource)
            {
                audioSource!.resource = null;
            }
            Sounds.AudioDataUsage.Release(usingResource, unloadIfUnused: unloadUsingResource);
            usingResource = null;
        }

        // AudioDataUsageに使用中として届けてあるリソース
        AudioResource? usingResource;
        // usingResourceを手放した時に、誰も使っていなければアンロードするか
        bool unloadUsingResource = true;

        /// <param name="unloadAudioDataWhenUnused">鳴らし終えた時に、ほかで使われていなければアンロードするか</param>
        public void Play(AudioResource source, bool loop, float volume, string? channel = null, int priority = 0, bool unloadAudioDataWhenUnused = true)
        {
            ++Generation;
            Channel = channel;
            Priority = priority;

            // 同じクリップを続けて鳴らす場合にアンロードされないよう、先に新しい方を届ける
            Sounds.AudioDataUsage.Acquire(source);
            Sounds.AudioDataUsage.Release(usingResource, unloadIfUnused: unloadUsingResource);
            usingResource = source;
            unloadUsingResource = unloadAudioDataWhenUnused;

            AudioSource.resource = source;
            AudioSource.loop = loop;
            AudioSource.volume = volume;
            AudioSource.Play();
            startedTime = Time.realtimeSinceStartup;

            enabled = true;
        }

        public AudioResource Resource => AudioSource.resource;
        public bool IsPlaying => AudioSource.isPlaying;
        public bool Loop => AudioSource.loop;
        public float Volume => AudioSource.volume;

        void Update()
        {
            if (!AudioSource.isPlaying)
            {
                Stop();
            }
        }

        public void Stop()
        {
            AudioSource.Stop();
            enabled = false;
        }
    }
}
