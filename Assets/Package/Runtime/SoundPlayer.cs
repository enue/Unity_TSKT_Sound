using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
namespace TSKT
{
    public class SoundPlayer : MonoBehaviour
    {
        [SerializeField]
        GameObject soundObjectPrefab = default;

        [SerializeField]
        float interval = 0.1f;

        readonly List<SoundObject> soundObjects = new List<SoundObject>();

        public void Play(AudioClip audio, bool loop = false, string channel = null, float volume = 1f)
        {
            Debug.Assert(audio, "audio cannot null");
            if (!audio)
            {
                return;
            }

            // 同じチャンネルの音を止める
            if (channel != null)
            {
                Stop(channel);
            }

            // 同じ音を同時に再生しない
            if (soundObjects.Count > 0
                && soundObjects.Any(_ => _.Clip == audio && _.ElapsedTime < interval))
            {
                return;
            }

            var soundObject = soundObjects.FirstOrDefault(_ => !_.Clip);
            if (soundObject == null)
            {
                var obj = Instantiate(soundObjectPrefab, transform, false);
                soundObject = obj.GetComponent<SoundObject>();
                soundObjects.Add(soundObject);
            }
            soundObject.Play(audio, loop: loop, volume: volume);
            soundObject.Channel = channel;
        }

        public void Stop(string channel)
        {
            foreach (var it in soundObjects)
            {
                if (it.Channel == channel)
                {
                    it.Stop();
                }
            }
        }

        public void StopAll()
        {
            foreach (var it in soundObjects)
            {
                it.Stop();
            }
        }

        public bool IsPlaying()
        {
            foreach (var it in soundObjects)
            {
                if (it.IsPlaying)
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsPlaying(AudioClip clip)
        {
            foreach (var it in soundObjects)
            {
                if (it.Clip == clip && it.IsPlaying)
                {
                    return true;
                }
            }
            return false;
        }

        public float GetPlayingSoundDuration(bool includeLoop = true)
        {
            var result = 0f;
            foreach (var it in soundObjects)
            {
                if (it.IsPlaying)
                {
                    if (includeLoop || !it.Loop)
                    {
                        result = Mathf.Max(result, it.Clip.length);
                    }
                }
            }
            return result;
        }
    }
}
