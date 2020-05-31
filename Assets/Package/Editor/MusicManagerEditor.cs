using UnityEditor;
using UnityEngine;


namespace TSKT
{
    [CustomEditor(typeof(MusicManager))]
    public class MusicManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (Application.isPlaying)
            {
                var obj = (MusicManager)target;
                EditorGUILayout.LabelField("position: " + obj.GetComponent<AudioSource>().time.ToString());
            }
        }
    }
}
