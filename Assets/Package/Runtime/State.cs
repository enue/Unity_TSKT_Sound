using UnityEngine;
using System.Collections;
using UnityEngine.Audio;
using Cysharp.Threading.Tasks;
#nullable enable

namespace TSKT.MusicManagers
{
    public readonly struct State
    {
        public readonly MusicManager player;
        public readonly Music? currentMusic;
        public readonly float position;

        public State(MusicManager player, float position)
        {
            this.player = player;
            currentMusic = player.CurrentMusic;
            this.position = position;
        }

        public readonly void Resume(float fadeOutDuration = 1f, float fadeInDuration = 0f)
        {
            player.Play(currentMusic, position: position,
                fadeInDuration: fadeInDuration,
                fadeOutDuration: fadeOutDuration);
        }

        public readonly void Replay(float fadeOutDuration = 1f, float fadeInDuration = 0f)
        {
            player.Play(currentMusic,
                fadeInDuration: fadeInDuration,
                fadeOutDuration: fadeOutDuration);
        }
    }
}
