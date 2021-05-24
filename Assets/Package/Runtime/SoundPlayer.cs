using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#nullable enable

namespace TSKT
{
    public class SoundPlayer : MonoBehaviour
    {
        public readonly struct Task
        {
            readonly SoundObject soundObject;

            public Task(SoundObject obj)
            {
                soundObject = obj;
            }

            public Task SetPriority(int priority)
            {
                if (soundObject)
                {
                    soundObject.Priority = priority;
                }
                return this;
            }
        }

        [SerializeField]
        GameObject? soundObjectPrefab = default;

        [SerializeField]
        float interval = 0.1f;

        readonly List<SoundObject> soundObjects = new List<SoundObject>();

        public Task Play(AudioClip audio, bool loop = false, string? channel = null, float volume = 1f)
        {
            if (!audio)
            {
                return default;
            }

            if (channel != null)
            {
                // 同じチャンネルの音を止める
                Stop(channel);
            }

            // 同じ音を同時に再生しない
            foreach (var it in soundObjects)
            {
                if (it.Clip == audio
                    && it.ElapsedTime < interval
                    && it.IsPlaying)
                {
                    return default;
                }
            }

            var soundObject = soundObjects.FirstOrDefault(_ => !_.Clip);
            if (soundObject == null)
            {
                var obj = Instantiate(soundObjectPrefab, transform, false)!;
                soundObject = obj.GetComponent<SoundObject>();
                soundObjects.Add(soundObject);
            }
            soundObject.Play(audio, loop: loop, volume: volume);
            soundObject.Channel = channel;

            return new Task(soundObject);
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

        public bool TryGetPlayingChannel(string channel, out SoundObject? soundObject)
        {
            foreach (var it in soundObjects)
            {
                if (it.Channel == channel && it.IsPlaying)
                {
                    soundObject = it;
                    return true;
                }
            }
            soundObject = null;
            return false;
        }

        public bool TryGetPriority(string channel, out int priority)
        {
            if (TryGetPlayingChannel(channel, out var current))
            {
                priority = current!.Priority;
                return true;
            }
            priority = 0;
            return false;
        }

        public int GetPriority(string channel, int defaultValue = int.MinValue)
        {
            if (TryGetPlayingChannel(channel, out var current))
            {
                return current!.Priority;
            }
            return defaultValue;
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
