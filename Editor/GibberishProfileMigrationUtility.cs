using UnityEditor;
using UnityEngine;

namespace CozyGibberish.Editor
{
    internal static class GibberishProfileMigrationUtility
    {
        private const string MenuPath =
            "Tools/Cozy Gibberish/Validate and Migrate Profiles";

        [MenuItem(MenuPath, priority = 130)]
        private static void ValidateAndMigrate()
        {
            var changed = 0;
            changed += MigrateVoices();
            changed += MigrateStyles();
            changed += MigratePhonotactics();
            AssetDatabase.SaveAssets();
            Debug.Log($"Cozy Gibberish profile validation complete. Updated assets: {changed}");
        }

        private static int MigrateVoices()
        {
            var changed = 0;
            var guids = AssetDatabase.FindAssets("t:GibberishVoiceProfile");
            for (var index = 0; index < guids.Length; index++)
            {
                var asset = AssetDatabase.LoadAssetAtPath<GibberishVoiceProfile>(
                    AssetDatabase.GUIDToAssetPath(guids[index]));
                if (asset != null && asset.ValidateAndMigrate())
                {
                    EditorUtility.SetDirty(asset);
                    changed++;
                }
            }

            return changed;
        }

        private static int MigrateStyles()
        {
            var changed = 0;
            var guids = AssetDatabase.FindAssets("t:GibberishStylePreset");
            for (var index = 0; index < guids.Length; index++)
            {
                var asset = AssetDatabase.LoadAssetAtPath<GibberishStylePreset>(
                    AssetDatabase.GUIDToAssetPath(guids[index]));
                if (asset != null && asset.ValidateAndMigrate())
                {
                    EditorUtility.SetDirty(asset);
                    changed++;
                }
            }

            return changed;
        }

        private static int MigratePhonotactics()
        {
            var changed = 0;
            var guids = AssetDatabase.FindAssets("t:GibberishPhonotacticProfile");
            for (var index = 0; index < guids.Length; index++)
            {
                var asset = AssetDatabase.LoadAssetAtPath<GibberishPhonotacticProfile>(
                    AssetDatabase.GUIDToAssetPath(guids[index]));
                if (asset != null && asset.ValidateAndMigrate())
                {
                    EditorUtility.SetDirty(asset);
                    changed++;
                }
            }

            return changed;
        }
    }
}
