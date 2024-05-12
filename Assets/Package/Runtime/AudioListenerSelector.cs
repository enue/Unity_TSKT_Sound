#nullable enable
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace TSKT.Sounds
{
    public class AudioListenerSelector : MonoBehaviour
    {
        readonly static List<AudioListenerSelector> instances = new();

        [SerializeField]
        AudioListener target = default!;

        [SerializeField]
        int ascendingPriority;

        void OnEnable()
        {
            instances.Add(this);
            EnableHighestPriorityListener();
        }
        void OnDisable()
        {
            instances.Remove(this);
            EnableHighestPriorityListener();
        }

        static void EnableHighestPriorityListener()
        {
            if (instances.Count == 0)
            {
                return;
            }
            var top = instances.Min(_ => _.ascendingPriority);
            foreach (var it in instances)
            {
                it.target.enabled = top == it.ascendingPriority;
            }
        }
    }
}
