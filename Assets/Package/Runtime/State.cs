using UnityEngine;
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

        public readonly Awaitable Resume(float fadeOutDuration = 1f, float fadeInDuration = 0f)
        {
            return player.Play(currentMusic, position: position,
                fadeInDuration: fadeInDuration,
                fadeOutDuration: fadeOutDuration);
        }

        public readonly Awaitable Replay(float fadeOutDuration = 1f, float fadeInDuration = 0f)
        {
            return player.Play(currentMusic,
                fadeInDuration: fadeInDuration,
                fadeOutDuration: fadeOutDuration,
                forceRestart: true);
        }
    }
}
