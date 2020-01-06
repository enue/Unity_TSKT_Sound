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
        AudioSource AudioSource => audioSource ?? (audioSource = GetComponent<AudioSource>());

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
            if (clip)
            {
                if (!clip.preloadAudioData)
                {
                    if (enabledInstances.Count == 0
                        || !enabledInstances.Any(_ => _.Clip == clip))
                    {
                        clip.UnloadAudioData();
                    }
                }
            }
        }

        public void Play(AudioClip clip, bool loop = false)
        {
            AudioSource.clip = clip;
            AudioSource.loop = loop;
            AudioSource.Play();
            startedTime = Time.realtimeSinceStartup;

            enabled = true;
        }

        public AudioClip Clip => AudioSource.clip;
        public bool IsPlaying => AudioSource.isPlaying;
        public bool Loop => AudioSource.loop;

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
