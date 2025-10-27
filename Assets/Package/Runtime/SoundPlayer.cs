using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
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
        public readonly struct Builder
        {
            readonly SoundPlayer owner;
            readonly AudioResource audio;
            readonly bool loop;
            readonly float volume;
            readonly Vector3? position;
            readonly int? priority;
            readonly string? channel;

            public Builder(SoundPlayer owner, AudioResource audio)
            {
                this.owner = owner;
                this.audio = audio;
                loop = false;
                volume = 1f;
                position = null;
                priority = null;
                channel = null;
            }
            Builder(SoundPlayer owner, AudioResource audio, bool loop, float volume, Vector3? position, int? priority, string? channel)
            {
                this.owner = owner;
                this.audio = audio;
                this.loop = loop;
                this.volume = volume;
                this.position = position;
                this.priority = priority;
                this.channel = channel;
            }
            public Builder With(bool? loop = null, float? volume = null, Vector3? position = null, int? priority = null, string? channel = null)
            {
                return new Builder(owner, audio,
                    loop: loop ?? this.loop,
                    volume: volume ?? this.volume,
                    position: position ?? this.position,
                    priority: priority ?? this.priority,
                    channel: channel ?? this.channel);
            }
            public readonly bool TryPlay()
            {
                if (priority.HasValue && channel != null)
                {
                    if (owner.GetPriority(channel) >= priority.Value)
                    {
                        return false;
                    }
                    owner.Play(audio, loop: loop, channel: channel, volume: volume, position: position)
                        .SetPriority(priority.Value);
                }
                else
                {
                    owner.Play(audio, loop: loop, channel: channel, volume: volume, position: position);
                }
                return true;
            }
        }

        [SerializeField]
        GameObject? soundObjectPrefab = default;

        [SerializeField]
        float interval = 0.1f;

        readonly List<SoundObject> soundObjects = new();

        public Task Play(AudioResource audio, bool loop = false, string? channel = null, float volume = 1f, Vector3? position = null)
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
                if (it.Resource == audio
                    && it.ElapsedTime < interval
                    && it.IsPlaying)
                {
                    return default;
                }
            }

            var soundObject = soundObjects.FirstOrDefault(_ => !_.Resource);
            if (soundObject == null)
            {
                var obj = Instantiate(soundObjectPrefab, transform, false)!;
                soundObject = obj.GetComponent<SoundObject>();
                soundObjects.Add(soundObject);
            }

            soundObject.Set3D(position.HasValue);
            if (position.HasValue)
            {
                soundObject.transform.position = position.Value;
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

        public bool IsPlaying(AudioResource resource)
        {
            foreach (var it in soundObjects)
            {
                if (it.Resource == resource && it.IsPlaying)
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
                        if (it.Resource is AudioClip clip)
                        {
                            result = Mathf.Max(result, clip.length);
                        }
                    }
                }
            }
            return result;
        }
        public virtual Builder From(AudioResource audio) => new(this, audio);
    }
}
