#if COZY_GIBBERISH_ADDRESSABLES
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

namespace CozyGibberish.Integrations.Addressables.Editor
{
    internal static class GibberishAddressablesBakeUtility
    {
        private const string MenuPath =
            "Assets/Cozy Gibberish/Mark Selected Bakes Addressable";

        [MenuItem(MenuPath, true)]
        private static bool Validate()
        {
            return Selection.assetGUIDs.Length > 0 &&
                   AddressableAssetSettingsDefaultObject.Settings != null;
        }

        [MenuItem(MenuPath)]
        private static void MarkSelected()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            for (var index = 0; index < Selection.assetGUIDs.Length; index++)
            {
                var guid = Selection.assetGUIDs[index];
                var entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
                entry.address = AssetDatabase.GUIDToAssetPath(guid);
            }

            AssetDatabase.SaveAssets();
        }
    }
}
#endif
