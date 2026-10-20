using System.Collections.Generic;
using Sight.Serialization.Schemas;

namespace Sight.Serialization.Binary;

/// <summary>
/// Schemas decoded from recent inline schema sections: a section with the same bytes is not decoded again
/// </summary>
/// <remarks>
/// Inline schemas form a closed list (they import nothing else), so their decoding only depends on the section bytes,
/// the deduplication length and the limits. Decoded schemas are validated, hence frozen and shareable.
/// </remarks>
internal static class InlineSchemaCache
{
    private const int MaxEntries = 16;

    private static readonly List<Entry> s_entries = new();

    /// <summary>
    /// Consume a section already decoded, and get its schemas
    /// </summary>
    public static IReadOnlyList<Schema>? TryRead(BinaryDecoder decoder, SerializationLimits limits)
    {
        if (decoder.DedupStringCount != 0)
            return null;

        lock (s_entries)
        {
            for (var i = s_entries.Count - 1; i >= 0; i--)
            {
                var entry = s_entries[i];
                if (entry.DedupMaxLength != decoder.DedupMaxLength || !entry.HasLimits(limits) || !decoder.TrySkipMatching(entry.Bytes))
                    continue;

                decoder.AddDedupStrings(entry.Strings);

                // Reused schemas get their hash once, so that read plans can be shared too
                if (!entry.IsHashed)
                {
                    foreach (var schema in entry.Schemas)
                        _ = schema.Hash;

                    entry.IsHashed = true;
                }

                return entry.Schemas;
            }
        }

        return null;
    }

    /// <summary>
    /// Remember a section just decoded, from <paramref name="start"/> to the current position of the decoder
    /// </summary>
    public static void Add(BinaryDecoder decoder, long start, SerializationLimits limits, IReadOnlyList<Schema> schemas)
    {
        var bytes = decoder.GetBytesReadSince(start);
        if (bytes == null)
            return;

        var entry = new Entry(bytes, decoder.GetDedupStrings(0), decoder.DedupMaxLength, limits, schemas);
        lock (s_entries)
        {
            if (s_entries.Count >= MaxEntries)
                s_entries.RemoveAt(0);

            s_entries.Add(entry);
        }
    }

    private sealed class Entry
    {
        private readonly int _maxDepth;
        private readonly int _maxCollectionCount;
        private readonly int _maxStringLength;

        public Entry(byte[] bytes, string[] strings, byte dedupMaxLength, SerializationLimits limits, IReadOnlyList<Schema> schemas)
        {
            Bytes = bytes;
            Strings = strings;
            DedupMaxLength = dedupMaxLength;
            Schemas = schemas;
            _maxDepth = limits.MaxDepth;
            _maxCollectionCount = limits.MaxCollectionCount;
            _maxStringLength = limits.MaxStringLength;
        }

        public byte[] Bytes { get; }

        public string[] Strings { get; }

        public byte DedupMaxLength { get; }

        public IReadOnlyList<Schema> Schemas { get; }

        public bool IsHashed { get; set; }

        /// <summary>
        /// Indicates whether the section was decoded with the same limits (a hit must not bypass stricter ones)
        /// </summary>
        public bool HasLimits(SerializationLimits limits)
        {
            return limits.MaxDepth == _maxDepth && limits.MaxCollectionCount == _maxCollectionCount && limits.MaxStringLength == _maxStringLength;
        }
    }
}
