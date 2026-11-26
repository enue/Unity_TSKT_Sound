using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
#nullable enable

namespace TSKT.Sounds
{
    /// <summary>
    /// SoundObjectとMusicManagerが使っているAudioClipを数えて、誰も使わなくなったらオーディオデータをアンロードする。
    /// preloadAudioDataが有効なクリップと、最後に手放した側がアンロードしないと指定したクリップはアンロードしない。
    /// </summary>
    static class AudioDataUsage
    {
        static readonly Dictionary<AudioClip, int> counts = new();
#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Init()
        {
            counts.Clear();
        }
#endif

        public static void Acquire(AudioResource? resource)
        {
            if (resource is AudioClip clip)
            {
                counts.TryGetValue(clip, out var count);
                counts[clip] = count + 1;
            }
        }

        /// <param name="unloadIfUnused">falseなら、誰も使わなくなってもアンロードしない</param>
        public static void Release(AudioResource? resource, bool unloadIfUnused = true)
        {
            if (resource is not AudioClip clip)
            {
                return;
            }
            if (!counts.TryGetValue(clip, out var count))
            {
                return;
            }
            if (count > 1)
            {
                counts[clip] = count - 1;
                return;
            }
            counts.Remove(clip);

            // 破棄済みのクリップは触らない
            if (unloadIfUnused
                && clip
                && !clip.preloadAudioData
                && clip.loadState == AudioDataLoadState.Loaded)
            {
                clip.UnloadAudioData();
            }
        }
    }
}
