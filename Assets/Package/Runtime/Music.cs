using UnityEngine;
using System.Collections;
using UnityEngine.Audio;
#nullable enable

namespace TSKT
{
    [CreateAssetMenu(fileName = "Music", menuName = "TSKT/Music", order = 1023)]
    public class Music : ScriptableObject
    {
        [SerializeField]
        AudioResource? asset = default;
        public AudioResource? Asset => asset;

        [SerializeField]
        bool loop = true;
        public bool Loop => loop;

        [SerializeField]
        [Range(0.01f, 1f)]
        float volume = 1f;
        public float Volume => volume;
    }
}
