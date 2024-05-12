using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Audio;
#nullable enable

namespace TSKT
{
    [RequireComponent(typeof(AudioSource))]
    public class SoundObject : MonoBehaviour
    {
        readonly static HashSet<SoundObject> enabledInstances = new HashSet<SoundObject>();

        AudioSource? audioSource;
        AudioSource AudioSource => audioSource ? audioSource! : (audioSource = GetComponent<AudioSource>());

        float startedTime;
        public float ElapsedTime => Time.realtimeSinceStartup - startedTime;

        public string? Channel { get; set; }
        public int Priority { get; set; }

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

        void OnEnable()
        {
            enabledInstances.Add(this);
        }
        void OnDisable()
        {
            enabledInstances.Remove(this);

            var clip = AudioSource.resource as AudioClip;
            AudioSource.resource = null;
            if (clip
                && !clip!.preloadAudioData
                && clip.loadState == AudioDataLoadState.Loaded)
            {
                if (enabledInstances.Count == 0
                    || !enabledInstances.Any(_ => _.Resource == clip))
                {
                    clip.UnloadAudioData();
                }
            }
        }

        public void Play(AudioResource source, bool loop, float volume)
        {
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
