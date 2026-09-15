#if COZY_GIBBERISH_LOCALIZATION
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace CozyGibberish.Integrations.Dialogue
{
    public sealed class GibberishLocalizedStringBridge : MonoBehaviour
    {
        [SerializeField] private GibberishDialogueBridge _dialogue;

        public void Speak(LocalizedString localizedString)
        {
            if (localizedString == null)
            {
                return;
            }

            var operation = localizedString.GetLocalizedStringAsync();
            operation.Completed += OnLocalizedStringLoaded;
        }

        private void OnLocalizedStringLoaded(AsyncOperationHandle<string> operation)
        {
            if (operation.Status == AsyncOperationStatus.Succeeded)
            {
                _dialogue.SpeakLine(operation.Result);
            }
        }
    }
}
#endif
