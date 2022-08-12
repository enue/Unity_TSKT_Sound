using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
#nullable enable

namespace TSKT
{
    public class SoundManager : SoundPlayer
    {
        readonly static List<SoundManager> instances = new();
        public static SoundManager? Instance => instances.LastOrDefault();

        void OnEnable()
        {
            instances.Add(this);
        }

        void OnDisable()
        {
            instances.Remove(this);
        }
    }
}
