using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CozyGibberish.Editor
{
    internal static class GibberishBatchBaker
    {
        private const string MenuPath = "Tools/Cozy Gibberish/Batch Bake Selected Manifest";
        private const string DefaultFolderName = "GibberishBakes";
        private const string WaveExtension = ".wav";

        [MenuItem(MenuPath, true)]
        private static bool ValidateBake()
        {
            return Selection.activeObject is GibberishBakeManifest;
        }

        [MenuItem(MenuPath, priority = 120)]
        private static void Bake()
        {
            var manifest = Selection.activeObject as GibberishBakeManifest;
            if (manifest == null)
            {
                return;
            }

            var folder = EditorUtility.OpenFolderPanel(
                "Choose Gibberish Bake Folder",
                string.Empty,
                DefaultFolderName);
            if (string.IsNullOrWhiteSpace(folder))
            {
                return;
            }

            var planner = new GibberishUtterancePlanner();
            var synthesizer = new ProceduralGibberishSynthesizer();

            for (var index = 0; index < manifest.Entries.Length; index++)
            {
                var entry = manifest.Entries[index];
                if (entry.Voice == null || string.IsNullOrWhiteSpace(entry.Text))
                {
                    continue;
                }

                var prosody = entry.Prosody == null
                    ? GibberishProsodySnapshot.Neutral
                    : entry.Prosody.CreateSnapshot();
                var voice = entry.Voice.CreateSnapshot(
                    GibberishStyleInfluence.From(entry.Style),
                    GibberishExpression.Neutral,
                    prosody);
                var phonotactics = entry.Phonotactics == null
                    ? GibberishPhonotacticSnapshot.Default
                    : entry.Phonotactics.CreateSnapshot();
                var utterance = planner.CreatePlan(
                    entry.Text,
                    voice,
                    phonotactics,
                    entry.Seed);
                var audio = synthesizer.Render(utterance, voice);
                var fileName = SanitizeFileName(
                    string.IsNullOrWhiteSpace(entry.FileName)
                        ? $"Gibberish_{index + 1}"
                        : entry.FileName);
                var path = Path.Combine(folder, fileName + WaveExtension);
                GibberishWavEncoder.Write(path, audio);
            }

            EditorUtility.RevealInFinder(folder);
        }

        private static string SanitizeFileName(string value)
        {
            var result = value;
            var invalid = Path.GetInvalidFileNameChars();
            for (var index = 0; index < invalid.Length; index++)
            {
                result = result.Replace(invalid[index], '_');
            }

            return result;
        }
    }
}
