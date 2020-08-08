using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Cysharp.Threading.Tasks;

namespace TSKT
{
    public class MusicAssetBundleStore : IMusicStore
    {
        Music[] musics = System.Array.Empty<Music>();

        public async void Add(string filename, int priorityOffset)
        {
            var loadeds = await AssetBundleUtil.LoadAllAsync<Music>(filename, priorityOffset: priorityOffset);
            if (loadeds.Succeeded)
            {
                musics = musics.Concat(loadeds.value).ToArray();
            }
            UnityEngine.Assertions.Assert.IsTrue(loadeds.Succeeded, "failed loading music assetbundle. " + loadeds.exception?.ToString());
        }

        public Music Get(string musicName)
        {
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
