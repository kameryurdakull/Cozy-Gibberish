using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace CozyGibberish.Editor
{
    internal static class GibberishEditorPreview
    {
        private const string AudioUtilityTypeName = "UnityEditor.AudioUtil";
        private const string PlayPreviewMethodName = "PlayPreviewClip";
        private const string LegacyPlayMethodName = "PlayClip";
        private const string StopPreviewMethodName = "StopAllPreviewClips";
        private const string LegacyStopMethodName = "StopAllClips";

        private static AudioClip _activeClip;
        private static GameObject _fallbackObject;
        private static AudioSource _fallbackSource;
        private static double _stopAt;

        public static bool IsPlaying => _activeClip != null;

        public static void Play(AudioClip clip)
        {
            if (clip == null)
            {
                throw new ArgumentNullException(nameof(clip));
            }

            Stop();
            var audioUtility = typeof(AudioImporter).Assembly.GetType(AudioUtilityTypeName);
            var playMethod = FindMethod(audioUtility, PlayPreviewMethodName, LegacyPlayMethodName);
            if (playMethod == null)
            {
                PlayFallback(clip);
            }
            else
            {
                var parameters = BuildArguments(playMethod, clip);
                playMethod.Invoke(null, parameters);
            }

            _activeClip = clip;
            _stopAt = EditorApplication.timeSinceStartup + clip.length + 0.1d;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        public static void Stop()
        {
            EditorApplication.update -= Tick;
            var audioUtility = typeof(AudioImporter).Assembly.GetType(AudioUtilityTypeName);
            var stopMethod = FindMethod(audioUtility, StopPreviewMethodName, LegacyStopMethodName);
            stopMethod?.Invoke(null, Array.Empty<object>());
            _fallbackSource?.Stop();

            if (_fallbackObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_fallbackObject);
                _fallbackObject = null;
                _fallbackSource = null;
            }

            if (_activeClip != null)
            {
                UnityEngine.Object.DestroyImmediate(_activeClip);
                _activeClip = null;
            }
        }

        private static void Tick()
        {
            if (EditorApplication.timeSinceStartup >= _stopAt)
            {
                Stop();
            }
        }

        private static MethodInfo FindMethod(Type type, params string[] names)
        {
            if (type == null)
            {
                return null;
            }

            var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            for (var nameIndex = 0; nameIndex < names.Length; nameIndex++)
            {
                var name = names[nameIndex];
                for (var methodIndex = 0; methodIndex < methods.Length; methodIndex++)
                {
                    var method = methods[methodIndex];
                    if (method.Name != name)
                    {
                        continue;
                    }

                    var parameters = method.GetParameters();
                    var isPlayMethod = name == PlayPreviewMethodName ||
                                       name == LegacyPlayMethodName;
                    if (!isPlayMethod ||
                        (parameters.Length > 0 &&
                         parameters[0].ParameterType == typeof(AudioClip)))
                    {
                        return method;
                    }
                }
            }

            return null;
        }

        private static object[] BuildArguments(MethodInfo method, AudioClip clip)
        {
            var parameters = method.GetParameters();
            var arguments = new object[parameters.Length];

            for (var index = 0; index < parameters.Length; index++)
            {
                var parameter = parameters[index];
                if (index == 0 && parameter.ParameterType == typeof(AudioClip))
                {
                    arguments[index] = clip;
                }
                else if (parameter.HasDefaultValue)
                {
                    arguments[index] = parameter.DefaultValue;
                }
                else if (parameter.ParameterType.IsValueType)
                {
                    arguments[index] = Activator.CreateInstance(parameter.ParameterType);
                }
            }

            return arguments;
        }

        private static void PlayFallback(AudioClip clip)
        {
            _fallbackObject = EditorUtility.CreateGameObjectWithHideFlags(
                nameof(GibberishEditorPreview),
                HideFlags.HideAndDontSave,
                typeof(AudioSource));
            _fallbackSource = _fallbackObject.GetComponent<AudioSource>();
            _fallbackSource.playOnAwake = false;
            _fallbackSource.clip = clip;
            _fallbackSource.Play();
        }
    }
}
