using System;
using System.Collections.Generic;

namespace CozyGibberish
{
    public sealed class GibberishPreparedSpeechCache
    {
        private readonly object _gate = new object();
        private readonly Dictionary<GibberishRequestFingerprint, CacheEntry> _entries =
            new Dictionary<GibberishRequestFingerprint, CacheEntry>();
        private readonly LinkedList<GibberishRequestFingerprint> _recency =
            new LinkedList<GibberishRequestFingerprint>();

        private int _entryCapacity;
        private long _memoryCapacity;
        private long _memoryUsage;

        public int Count
        {
            get
            {
                lock (_gate)
                {
                    return _entries.Count;
                }
            }
        }

        public long MemoryUsage
        {
            get
            {
                lock (_gate)
                {
                    return _memoryUsage;
                }
            }
        }

        public GibberishPreparedSpeechCache(int entryCapacity, long memoryCapacity)
        {
            SetLimits(entryCapacity, memoryCapacity);
        }

        public void SetLimits(int entryCapacity, long memoryCapacity)
        {
            lock (_gate)
            {
                _entryCapacity = Math.Max(0, entryCapacity);
                _memoryCapacity = Math.Max(0L, memoryCapacity);
                TrimToBudget();
            }
        }

        public bool TryGet(
            GibberishRequestFingerprint fingerprint,
            out GibberishAudioData audio)
        {
            lock (_gate)
            {
                if (!_entries.TryGetValue(fingerprint, out var entry))
                {
                    audio = null;
                    return false;
                }

                _recency.Remove(entry.Node);
                _recency.AddFirst(entry.Node);
                audio = entry.Audio;
                return true;
            }
        }

        public void Store(
            GibberishRequestFingerprint fingerprint,
            GibberishAudioData audio)
        {
            if (audio == null || _entryCapacity == 0 || _memoryCapacity == 0L)
            {
                return;
            }

            var size = audio.Samples.LongLength * sizeof(float);
            if (size > _memoryCapacity)
            {
                return;
            }

            lock (_gate)
            {
                if (_entries.TryGetValue(fingerprint, out var existing))
                {
                    _memoryUsage -= existing.Size;
                    _recency.Remove(existing.Node);
                    _entries.Remove(fingerprint);
                }

                var node = new LinkedListNode<GibberishRequestFingerprint>(fingerprint);
                _recency.AddFirst(node);
                _entries.Add(fingerprint, new CacheEntry(audio, node, size));
                _memoryUsage += size;
                TrimToBudget();
            }
        }

        public void Clear()
        {
            lock (_gate)
            {
                _entries.Clear();
                _recency.Clear();
                _memoryUsage = 0L;
            }
        }

        private void TrimToBudget()
        {
            for (var guard = _entries.Count;
                 guard > 0 &&
                 (_entries.Count > _entryCapacity || _memoryUsage > _memoryCapacity);
                 guard--)
            {
                var last = _recency.Last;
                if (last == null || !_entries.TryGetValue(last.Value, out var entry))
                {
                    break;
                }

                _memoryUsage -= entry.Size;
                _entries.Remove(last.Value);
                _recency.RemoveLast();
            }
        }

        private sealed class CacheEntry
        {
            public GibberishAudioData Audio { get; }
            public LinkedListNode<GibberishRequestFingerprint> Node { get; }
            public long Size { get; }

            public CacheEntry(
                GibberishAudioData audio,
                LinkedListNode<GibberishRequestFingerprint> node,
                long size)
            {
                Audio = audio;
                Node = node;
                Size = size;
            }
        }
    }
}
