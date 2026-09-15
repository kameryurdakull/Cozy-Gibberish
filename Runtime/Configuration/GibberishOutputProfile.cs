using UnityEngine;
using UnityEngine.Audio;

namespace CozyGibberish
{
    [CreateAssetMenu(
        fileName = "GibberishOutput",
        menuName = "Cozy Gibberish/Output Profile",
        order = 15)]
    public sealed class GibberishOutputProfile : ScriptableObject
    {
        [Header("Routing")]
        [SerializeField] private AudioMixerGroup _mixerGroup;
        [SerializeField, Range(0f, 1f)] private float _spatialBlend;
        [SerializeField, Range(0f, 1.1f)] private float _reverbZoneMix = 1f;
        [SerializeField, Range(0f, 360f)] private float _spread;
        [SerializeField, Range(0f, 5f)] private float _dopplerLevel = 1f;
        [SerializeField, Min(0.01f)] private float _minimumDistance = 1f;
        [SerializeField, Min(0.1f)] private float _maximumDistance = 25f;
        [SerializeField] private AudioRolloffMode _rolloffMode = AudioRolloffMode.Logarithmic;

        [Header("Mixer Ducking")]
        [SerializeField] private bool _enableDucking;
        [SerializeField] private string _duckingParameter;
        [SerializeField, Range(-80f, 20f)] private float _normalDuckingDb;
        [SerializeField, Range(-80f, 20f)] private float _speakingDuckingDb = -8f;

        [Header("Playback")]
        [SerializeField] private GibberishPlaybackBackend _backend = GibberishPlaybackBackend.AudioClip;
        [SerializeField, Range(0f, 0.5f)] private float _crossfadeDuration = 0.08f;
        [SerializeField, Range(0.01f, 0.25f)] private float _scheduleLeadTime = 0.04f;
        [SerializeField, Range(0f, 0.1f)] private float _eventLookahead = 0.025f;
        [SerializeField, Min(1)] private int _queueCapacity = 32;
        [SerializeField] private GibberishQueueOverflowPolicy _overflowPolicy =
            GibberishQueueOverflowPolicy.DropLowestPriority;

        [Header("Memory")]
        [SerializeField, Min(0)] private int _cacheEntryCapacity = 32;
        [SerializeField, Min(1)] private int _cacheMemoryMegabytes = 32;
        [SerializeField] private GibberishQualityTier _qualityTier = GibberishQualityTier.Balanced;

        public AudioMixerGroup MixerGroup => _mixerGroup;
        public float SpatialBlend => _spatialBlend;
        public float ReverbZoneMix => _reverbZoneMix;
        public float Spread => _spread;
        public float DopplerLevel => _dopplerLevel;
        public float MinimumDistance => _minimumDistance;
        public float MaximumDistance => Mathf.Max(_minimumDistance, _maximumDistance);
        public AudioRolloffMode RolloffMode => _rolloffMode;
        public bool EnableDucking => _enableDucking &&
                                      !string.IsNullOrWhiteSpace(_duckingParameter) &&
                                      _mixerGroup != null;
        public string DuckingParameter => _duckingParameter;
        public float NormalDuckingDb => _normalDuckingDb;
        public float SpeakingDuckingDb => _speakingDuckingDb;
        public GibberishPlaybackBackend Backend => _backend;
        public float CrossfadeDuration => _crossfadeDuration;
        public float ScheduleLeadTime => _scheduleLeadTime;
        public float EventLookahead => _eventLookahead;
        public int QueueCapacity => Mathf.Max(1, _queueCapacity);
        public GibberishQueueOverflowPolicy OverflowPolicy => _overflowPolicy;
        public int CacheEntryCapacity => Mathf.Max(0, _cacheEntryCapacity);
        public long CacheMemoryBytes => Mathf.Max(1, _cacheMemoryMegabytes) * 1024L * 1024L;
        public GibberishQualityTier QualityTier => _qualityTier;
    }
}
