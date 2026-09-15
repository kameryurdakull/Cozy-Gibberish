using System;
using UnityEngine;

namespace CozyGibberish
{
    [Serializable]
    public struct GibberishBakeEntry
    {
        [SerializeField] private string _fileName;
        [SerializeField, TextArea] private string _text;
        [SerializeField] private GibberishVoiceProfile _voice;
        [SerializeField] private GibberishStylePreset _style;
        [SerializeField] private GibberishPhonotacticProfile _phonotactics;
        [SerializeField] private GibberishProsodyProfile _prosody;
        [SerializeField] private int _seed;

        public string FileName => _fileName ?? string.Empty;
        public string Text => _text ?? string.Empty;
        public GibberishVoiceProfile Voice => _voice;
        public GibberishStylePreset Style => _style;
        public GibberishPhonotacticProfile Phonotactics => _phonotactics;
        public GibberishProsodyProfile Prosody => _prosody;
        public int Seed => _seed;
    }

    [CreateAssetMenu(
        fileName = "GibberishBakeManifest",
        menuName = "Cozy Gibberish/Bake Manifest",
        order = 19)]
    public sealed class GibberishBakeManifest : ScriptableObject
    {
        [SerializeField] private GibberishBakeEntry[] _entries =
            Array.Empty<GibberishBakeEntry>();

        public GibberishBakeEntry[] Entries => _entries ?? Array.Empty<GibberishBakeEntry>();
    }
}
