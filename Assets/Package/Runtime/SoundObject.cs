using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace TSKT
{
    [RequireComponent(typeof(AudioSource))]
    public class SoundObject : MonoBehaviour
    {
        readonly static HashSet<SoundObject> enabledInstances = new HashSet<SoundObject>();

        AudioSource audioSource;
        AudioSource AudioSource => audioSource ? audioSource : (audioSource = GetComponent<AudioSource>());

        float startedTime;
        public float ElapsedTime => Time.realtimeSinceStartup - startedTime;

        public string Channel { get; set; }

        void OnEnable()
        {
            enabledInstances.Add(this);
        }
        void OnDisable()
        {
            enabledInstances.Remove(this);

            var clip = AudioSource.clip;
            AudioSource.clip = null;
            if (clip
                && !clip.preloadAudioData
                && clip.loadState == AudioDataLoadState.Loaded)
            {
                if (enabledInstances.Count == 0
                    || !enabledInstances.Any(_ => _.Clip == clip))
                {
                    clip.UnloadAudioData();
                }
            }
        }

        public void Play(AudioClip clip, bool loop, float volume)
        {
            AudioSource.clip = clip;
            AudioSource.loop = loop;
            AudioSource.volume = volume;
            AudioSource.Play();
            startedTime = Time.realtimeSinceStartup;

            enabled = true;
        }

        public AudioClip Clip => AudioSource.clip;
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
