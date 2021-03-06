﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Cysharp.Threading.Tasks;
#nullable enable

namespace TSKT
{
    [CreateAssetMenu(fileName = "MusicStore", menuName = "TSKT/Music Store", order = 1023)]
    public class MusicStore : ScriptableObject, IMusicStore
    {
        [SerializeField]
        Music?[] musics = default!;

        public Music? Get(string musicName)
        {
            if (musics == null)
            {
                Debug.Assert(musics != null, "music store is null");
                return null;
            }
            foreach (var it in musics)
            {
                if (it && it!.name == musicName)
                {
                    return it;
                }
            }
            return null;
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