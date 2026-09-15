using UnityEditor;
using UnityEngine;

namespace CozyGibberish.Editor
{
    [CustomEditor(typeof(GibberishVoiceProfile))]
    public sealed class GibberishVoiceProfileEditor : UnityEditor.Editor
    {
        private const string DefaultPreviewText = "Welcome home, little traveler. Tea is almost ready!";
        private const string PreviewClipName = "CozyGibberish_Preview";

        private string _previewText = DefaultPreviewText;
        private GibberishStylePreset _style;
        private float _energy;
        private float _warmth;
        private float _pace;
        private float _pitch;
        private int _seed;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("Live Preview", EditorStyles.boldLabel);
            _previewText = EditorGUILayout.TextField("Text", _previewText);
            _style = (GibberishStylePreset)EditorGUILayout.ObjectField(
                "Style",
                _style,
                typeof(GibberishStylePreset),
                false);
            _seed = EditorGUILayout.IntField("Seed", _seed);
            _energy = EditorGUILayout.Slider("Energy", _energy, -1f, 1f);
            _warmth = EditorGUILayout.Slider("Warmth", _warmth, -1f, 1f);
            _pace = EditorGUILayout.Slider("Pace", _pace, -1f, 1f);
            _pitch = EditorGUILayout.Slider("Pitch", _pitch, -1f, 1f);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Preview"))
                {
                    Preview();
                }

                using (new EditorGUI.DisabledScope(!GibberishEditorPreview.IsPlaying))
                {
                    if (GUILayout.Button("Stop"))
                    {
                        GibberishEditorPreview.Stop();
                    }
                }
            }

            EditorGUILayout.HelpBox(
                "Preview uses the exact runtime planner and synthesizer. A fixed seed always produces the same delivery.",
                MessageType.Info);
        }

        private void OnDisable()
        {
            GibberishEditorPreview.Stop();
        }

        private void Preview()
        {
            var profile = (GibberishVoiceProfile)target;
            var expression = new GibberishExpression(_energy, _warmth, _pace, _pitch);
            var snapshot = profile.CreateSnapshot(_style, expression);
            var planner = new GibberishUtterancePlanner();
            var utterance = planner.CreatePlan(_previewText, snapshot, _seed);
            var synthesizer = new ProceduralGibberishSynthesizer();
            var audio = synthesizer.Render(utterance, snapshot);
            var clip = GibberishAudioClipFactory.Create(PreviewClipName, audio);
            GibberishEditorPreview.Play(clip);
        }
    }
}
