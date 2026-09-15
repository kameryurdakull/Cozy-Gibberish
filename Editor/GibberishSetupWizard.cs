using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CozyGibberish.Editor
{
    public sealed class GibberishSetupWizard : EditorWindow
    {
        private const string MenuPath = "Tools/Cozy Gibberish/Setup Wizard";
        private const string DefaultOutputFolder = "Assets/CozyGibberish";
        private const string VoiceAssetSuffix = "Voice.asset";
        private const string StyleAssetSuffix = "Style.asset";
        private const string PhonotacticsAssetSuffix = "Phonotactics.asset";
        private const string ProsodyAssetName = "DefaultProsody.asset";
        private const string OutputAssetName = "DefaultOutput.asset";
        private const string StyleStackAssetName = "DefaultStyleStack.asset";
        private const string PlayerPrefabName = "GibberishVoicePlayer.prefab";
        private const string PlayerObjectName = "Gibberish Voice Player";

        private string _outputFolder = DefaultOutputFolder;
        private GibberishVoiceArchetype _voiceArchetype = GibberishVoiceArchetype.Cozy;
        private bool _createAllStyles = true;
        private bool _createPlayerPrefab = true;

        [MenuItem(MenuPath, priority = 100)]
        public static void Open()
        {
            var window = GetWindow<GibberishSetupWizard>("Cozy Gibberish");
            window.minSize = new Vector2(420f, 270f);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Cozy Gibberish Setup", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Creates a production-ready voice profile, style presets and an optional player prefab. Existing assets are never overwritten.",
                MessageType.Info);
            EditorGUILayout.Space(8f);

            _outputFolder = EditorGUILayout.TextField("Output Folder", _outputFolder);
            _voiceArchetype = (GibberishVoiceArchetype)EditorGUILayout.EnumPopup(
                "Voice Archetype",
                _voiceArchetype);
            _createAllStyles = EditorGUILayout.Toggle("Create All Styles", _createAllStyles);
            _createPlayerPrefab = EditorGUILayout.Toggle("Create Player Prefab", _createPlayerPrefab);

            EditorGUILayout.Space(16f);
            using (new EditorGUI.DisabledScope(!IsValidAssetFolder(_outputFolder)))
            {
                if (GUILayout.Button("Create Starter Kit", GUILayout.Height(34f)))
                {
                    CreateStarterKit();
                }
            }

            if (!IsValidAssetFolder(_outputFolder))
            {
                EditorGUILayout.HelpBox("Output folder must be inside Assets.", MessageType.Warning);
            }
        }

        private void CreateStarterKit()
        {
            EnsureFolder(_outputFolder);
            var voice = ScriptableObject.CreateInstance<GibberishVoiceProfile>();
            voice.ApplyArchetype(_voiceArchetype);
            var voicePath = AssetDatabase.GenerateUniqueAssetPath(
                CombineAssetPath(_outputFolder, _voiceArchetype + VoiceAssetSuffix));
            AssetDatabase.CreateAsset(voice, voicePath);

            var styles = CreateStyles(_outputFolder, _createAllStyles);
            var phonotactics = ScriptableObject.CreateInstance<GibberishPhonotacticProfile>();
            phonotactics.ApplyArchetype(_voiceArchetype);
            CreateAsset(
                phonotactics,
                _outputFolder,
                _voiceArchetype + PhonotacticsAssetSuffix);
            var prosody = ScriptableObject.CreateInstance<GibberishProsodyProfile>();
            CreateAsset(prosody, _outputFolder, ProsodyAssetName);
            var output = ScriptableObject.CreateInstance<GibberishOutputProfile>();
            CreateAsset(output, _outputFolder, OutputAssetName);
            var styleStack = ScriptableObject.CreateInstance<GibberishStyleStack>();
            styleStack.SetLayers(styles.ToArray());
            CreateAsset(styleStack, _outputFolder, StyleStackAssetName);

            if (_createPlayerPrefab)
            {
                CreatePlayerPrefab(
                    _outputFolder,
                    voice,
                    styles,
                    styleStack,
                    phonotactics,
                    prosody,
                    output);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = voice;
            EditorGUIUtility.PingObject(voice);
        }

        private static List<GibberishStylePreset> CreateStyles(
            string outputFolder,
            bool createAll)
        {
            var styles = new List<GibberishStylePreset>();
            var archetypes = createAll
                ? (GibberishStyleArchetype[])Enum.GetValues(typeof(GibberishStyleArchetype))
                : new[] { GibberishStyleArchetype.Cozy };

            for (var index = 0; index < archetypes.Length; index++)
            {
                var archetype = archetypes[index];
                var style = ScriptableObject.CreateInstance<GibberishStylePreset>();
                style.ApplyArchetype(archetype);
                var path = AssetDatabase.GenerateUniqueAssetPath(
                    CombineAssetPath(outputFolder, archetype + StyleAssetSuffix));
                AssetDatabase.CreateAsset(style, path);
                styles.Add(style);
            }

            return styles;
        }

        private static void CreatePlayerPrefab(
            string outputFolder,
            GibberishVoiceProfile voice,
            IReadOnlyList<GibberishStylePreset> styles,
            GibberishStyleStack styleStack,
            GibberishPhonotacticProfile phonotactics,
            GibberishProsodyProfile prosody,
            GibberishOutputProfile output)
        {
            var playerObject = new GameObject(PlayerObjectName);
            try
            {
                var audioSource = playerObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.loop = false;
                playerObject.AddComponent<AudioSource>();
                var player = playerObject.AddComponent<GibberishVoicePlayer>();
                var defaultStyle = styles.Count == 0 ? null : styles[0];
                player.SetDefaults(voice, defaultStyle);
                player.SetConfiguration(
                    styleStack,
                    phonotactics,
                    prosody,
                    null,
                    output);

                var prefabPath = AssetDatabase.GenerateUniqueAssetPath(
                    CombineAssetPath(outputFolder, PlayerPrefabName));
                PrefabUtility.SaveAsPrefabAsset(playerObject, prefabPath);
            }
            finally
            {
                DestroyImmediate(playerObject);
            }
        }

        private static void CreateAsset(
            ScriptableObject asset,
            string outputFolder,
            string fileName)
        {
            var path = AssetDatabase.GenerateUniqueAssetPath(
                CombineAssetPath(outputFolder, fileName));
            AssetDatabase.CreateAsset(asset, path);
        }

        private static void EnsureFolder(string assetPath)
        {
            var normalized = assetPath.Replace('\\', '/').TrimEnd('/');
            var segments = normalized.Split('/');
            var current = segments[0];

            for (var index = 1; index < segments.Length; index++)
            {
                var next = current + "/" + segments[index];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[index]);
                }

                current = next;
            }
        }

        private static bool IsValidAssetFolder(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            var normalized = path.Replace('\\', '/');
            return normalized == "Assets" || normalized.StartsWith("Assets/", StringComparison.Ordinal);
        }

        private static string CombineAssetPath(string folder, string fileName)
        {
            return Path.Combine(folder, fileName).Replace('\\', '/');
        }
    }
}
