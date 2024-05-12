#nullable enable
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace TSKT
{
    public class SoundManager : SoundPlayer
    {
        readonly static List<SoundManager> instances = new();
        public static SoundManager? Instance { get; private set; }

        [SerializeField]
        int ascendingPriority;

        void OnEnable()
        {
            // priorityが等しいManagerが複数ある場合、新しい方を選択する
            instances.Insert(0, this);
            SelectHighestPriorityInstance();
        }

        void OnDisable()
        {
            instances.Remove(this);
            SelectHighestPriorityInstance();
        }

        void SelectHighestPriorityInstance()
        {
            if (instances.Count == 0)
            {
                Instance = null;
                return;
            }
            Instance = instances.OrderBy(_ => _.ascendingPriority).First();
        }
    }
}
