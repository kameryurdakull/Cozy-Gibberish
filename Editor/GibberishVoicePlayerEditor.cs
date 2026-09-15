using UnityEditor;
using UnityEngine;

namespace CozyGibberish.Editor
{
    [CustomEditor(typeof(GibberishVoicePlayer))]
    public sealed class GibberishVoicePlayerEditor : UnityEditor.Editor
    {
        private const string DefaultRuntimeText = "A warm hello from Cozy Gibberish!";

        private string _runtimeText = DefaultRuntimeText;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("Runtime Debug", EditorStyles.boldLabel);
            _runtimeText = EditorGUILayout.TextField("Text", _runtimeText);

            var player = (GibberishVoicePlayer)target;
            using (new EditorGUI.DisabledScope(!Application.isPlaying || player.DefaultVoice == null))
            {
                if (GUILayout.Button("Speak"))
                {
                    player.Speak(_runtimeText);
                }
            }

            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField("Speaking", player.IsSpeaking ? "Yes" : "No");
                EditorGUILayout.LabelField("Queued", player.QueueCount.ToString());
                Repaint();
            }
        }
    }
}
