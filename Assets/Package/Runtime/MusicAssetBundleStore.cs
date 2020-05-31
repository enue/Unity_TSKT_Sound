using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UniRx.Async;

namespace TSKT
{
    public class MusicAssetBundleStore : IMusicStore
    {
        List<UniTask<Music[]>> loaders;
        Music[] musics = System.Array.Empty<Music>();

        public void Add(string filename, int priorityOffset)
        {
            if (loaders == null)
            {
                loaders = new List<UniTask<Music[]>>();
            }
            loaders.Add(AssetBundleUtil.LoadAllAsync<Music>(filename, priorityOffset: priorityOffset));
        }

        void Refresh()
        {
            if (loaders != null)
            {
                for (int i = 0; i < loaders.Count;)
                {
                    if (loaders[i].IsCompleted)
                    {
                        musics = musics.Concat(loaders[i].Result).ToArray();
                        loaders.RemoveAt(i);
                    }
                    else
                    {
                        ++i;
                    }
                }
                if (loaders.Count == 0)
                {
                    loaders = null;
                }
            }
        }

        public Music Get(string musicName)
        {
            Refresh();
            foreach (var it in musics)
            {
                if (it.name == musicName)
                {
                    return it;
                }
            }
            Debug.Assert(musics != null, "music store is not loaded yet");
            return null;
        }
    }
}
