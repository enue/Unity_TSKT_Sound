using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Cysharp.Threading.Tasks;
#nullable enable

#if TSKT_SOUND_ASSETBUNDLE_SUPPORT && TSKT_SOUND_FILE_SUPPORT
namespace TSKT
{
    public class MusicAssetBundleStore : IMusicStore
    {
        Music[] musics = System.Array.Empty<Music>();

        public async void Add(string filename, string filepath, int priority)
        {
            var loader = new LocalAssetBundleLoader();
            var loadeds = await loader.LoadAllAsync<Music>(filename, filepath, priority: priority);
            if (loadeds.Succeeded)
            {
                musics = musics.Concat(loadeds.value).ToArray();
            }
            UnityEngine.Assertions.Assert.IsTrue(loadeds.Succeeded, "failed loading music assetbundle. " + loadeds.exception?.ToString());
        }

        public Music? Get(string musicName)
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
#endif
