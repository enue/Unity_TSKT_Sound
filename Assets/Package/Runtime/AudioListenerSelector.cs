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
#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Init()
        {
            instances.Clear();
        }
#endif

        [SerializeField]
        AudioListener target = default!;

        [SerializeField]
        int ascendingPriority;

        void OnEnable()
        {
            // priorityが等しいSelectorが複数ある場合、新しい方を選択する
            instances.Insert(0, this);
            EnableHighestPriorityListener();
        }
        void OnDisable()
        {
            instances.Remove(this);
            var top = EnableHighestPriorityListener();
            // 他に有効にしたListenerがあるなら、自分のListenerは止める。なければ無音にならないようそのままにする
            if (top && target && target != top!.target)
            {
                target.enabled = false;
            }
        }

        static AudioListenerSelector? EnableHighestPriorityListener()
        {
            AudioListenerSelector? top = null;
            foreach (var it in instances)
            {
                // シーン破棄中などでListenerが先に破棄されていることがある
                if (!it.target)
                {
                    continue;
                }
                if (!top || it.ascendingPriority < top!.ascendingPriority)
                {
                    top = it;
                }
            }
            if (!top)
            {
                return null;
            }
            foreach (var it in instances)
            {
                if (it.target)
                {
                    it.target.enabled = it.target == top!.target;
                }
            }
            return top;
        }
    }
}
