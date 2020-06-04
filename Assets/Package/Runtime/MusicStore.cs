using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Cysharp.Threading.Tasks;

namespace TSKT
{
    [CreateAssetMenu(fileName = "MusicStore", menuName = "TSKT/Music Store", order = 1023)]
    public class MusicStore : ScriptableObject, IMusicStore
    {
        [SerializeField]
        Music[] musics = default;

        public Music Get(string musicName)
        {
            if (musics == null)
            {
                Debug.Assert(musics != null, "music store is null");
                return null;
            }
            return musics.FirstOrDefault(_ => _.name == musicName);
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            var musics = new List<Music>();
            var guids = UnityEditor.AssetDatabase.FindAssets("t:Music");
            foreach (var it in guids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(it);
                var music = UnityEditor.AssetDatabase.LoadAssetAtPath<Music>(path);
                musics.Add(music);
            }
            this.musics = musics.ToArray();
        }
#endif
    }
}