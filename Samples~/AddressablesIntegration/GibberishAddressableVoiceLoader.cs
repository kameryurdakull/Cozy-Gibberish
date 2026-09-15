#if COZY_GIBBERISH_ADDRESSABLES
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace CozyGibberish.Integrations.Addressables
{
    public sealed class GibberishAddressableVoiceLoader : MonoBehaviour
    {
        [SerializeField] private AssetReferenceT<GibberishVoiceProfile> _voiceReference;
        [SerializeField] private AssetReferenceT<GibberishStylePreset> _styleReference;

        private GibberishVoiceProfile _voice;
        private GibberishStylePreset _style;

        public void Load(Action<GibberishVoiceProfile, GibberishStylePreset> completed)
        {
            var voiceOperation = _voiceReference.LoadAssetAsync();
            voiceOperation.Completed += operation =>
            {
                if (operation.Status != AsyncOperationStatus.Succeeded)
                {
                    completed?.Invoke(null, null);
                    return;
                }

                _voice = operation.Result;
                var styleOperation = _styleReference.LoadAssetAsync();
                styleOperation.Completed += styleResult =>
                {
                    _style = styleResult.Status == AsyncOperationStatus.Succeeded
                        ? styleResult.Result
                        : null;
                    completed?.Invoke(_voice, _style);
                };
            };
        }

        private void OnDestroy()
        {
            if (_voiceReference.IsValid())
            {
                _voiceReference.ReleaseAsset();
            }

            if (_styleReference.IsValid())
            {
                _styleReference.ReleaseAsset();
            }
        }
    }
}
#endif
