using UnityEngine;
using System.Collections;
using UnityEngine.Audio;
using Cysharp.Threading.Tasks;
#nullable enable

namespace TSKT.MusicManagers
{
    public readonly struct State
    {
        readonly MusicManager player;
        readonly Music? currentMusic;
        readonly float position;

        public State(MusicManager player, float position)
        {
            this.player = player;
            currentMusic = player.CurrentMusic;
            this.position = position;
        }

        readonly public void Resume(float fadeOutDuration = 1f, float fadeInDuration = 0f)
        {
            player.Play(currentMusic, position: position,
                fadeInDuration: fadeInDuration,
                fadeOutDuration: fadeOutDuration);
        }
    }
}
