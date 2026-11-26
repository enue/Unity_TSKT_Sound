using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
#nullable enable

namespace TSKT
{
    public class SoundPlayer : MonoBehaviour
    {
        /// <summary>
        /// Playが返す、再生した音への参照。
        /// SoundObjectは使い回されるので、別の音の再生に使われたあとは無効になる。
        /// </summary>
        public readonly struct Handle
        {
            readonly SoundObject? soundObject;
            readonly int generation;

            public Handle(SoundObject soundObject)
            {
                this.soundObject = soundObject;
                generation = soundObject.Generation;
            }

            /// <summary>
            /// 再生されなかった場合と、SoundObjectが別の音に使い回されたあとはfalse。
            /// </summary>
            public bool IsValid => soundObject && soundObject!.Generation == generation;
            public bool IsPlaying => IsValid && soundObject!.IsPlaying;

            public void Stop()
            {
                if (IsValid)
                {
                    soundObject!.Stop();
                }
            }
        }

        [SerializeField]
        GameObject? soundObjectPrefab = default;

        [SerializeField]
        float interval = 0.1f;

        readonly List<SoundObject> soundObjects = new();

        [SerializeField]
        [Tooltip("鳴らし終えたクリップを、ほかで使われていなければアンロードする。ボイスのように大きくて数の多い音向け。短い音を頻繁に鳴らすプレイヤーでは、アンロードと再デコードを繰り返す方が重いのでオフにする。preloadAudioDataが有効なクリップは、この設定にかかわらずアンロードしない。")]
        bool unloadAudioDataWhenUnused = false;

        /// <param name="channel">指定すると、同じチャンネルで再生中の音を止めてから再生する。</param>
        /// <param name="priority">channelとともに指定すると、同じチャンネルで再生中の音の優先度がこの値以上の場合は再生しない。省略すると必ず再生し、再生した音の優先度は0になる。</param>
        /// <returns>再生しなかった場合はIsValidがfalseのHandle</returns>
        public Handle Play(AudioResource audio, bool loop = false, string? channel = null, float volume = 1f, Vector3? position = null, int? priority = null)
        {
            if (!audio)
            {
                return default;
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

            if (channel != null)
            {
                if (priority.HasValue && GetPriority(channel) >= priority.Value)
                {
                    return default;
                }
                // 同じチャンネルの音を止める
                Stop(channel);
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

            soundObject.Play(audio, loop: loop, volume: volume, channel: channel, priority: priority ?? 0, unloadAudioDataWhenUnused: unloadAudioDataWhenUnused);
            return new Handle(soundObject);
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
    }
}
