using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
#nullable enable

namespace TSKT
{
    public class SoundManager : SoundPlayer
    {
        public static SoundManager? Instance { get; protected set; }

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
