using System;
using System.Buffers.Binary;

namespace Sight.Serialization.Internal;

/// <summary>
/// XXH64 hash (seed 0), used for schema hashes
/// </summary>
internal static class XxHash64
{
    private const ulong Prime1 = 11400714785074694791UL;
    private const ulong Prime2 = 14029467366897019727UL;
    private const ulong Prime3 = 1609587929392839161UL;
    private const ulong Prime4 = 9650029242287828579UL;
    private const ulong Prime5 = 2870177450012600261UL;

    public static ulong Hash(ReadOnlySpan<byte> data, ulong seed = 0)
    {
        var length = data.Length;
        ulong hash;
        if (length >= 32)
        {
            var v1 = seed + Prime1 + Prime2;
            var v2 = seed + Prime2;
            var v3 = seed;
            var v4 = seed - Prime1;
            do
            {
                v1 = Round(v1, BinaryPrimitives.ReadUInt64LittleEndian(data));
                v2 = Round(v2, BinaryPrimitives.ReadUInt64LittleEndian(data.Slice(8)));
                v3 = Round(v3, BinaryPrimitives.ReadUInt64LittleEndian(data.Slice(16)));
                v4 = Round(v4, BinaryPrimitives.ReadUInt64LittleEndian(data.Slice(24)));
                data = data.Slice(32);
            } while (data.Length >= 32);

            hash = RotateLeft(v1, 1) + RotateLeft(v2, 7) + RotateLeft(v3, 12) + RotateLeft(v4, 18);
            hash = MergeRound(hash, v1);
            hash = MergeRound(hash, v2);
            hash = MergeRound(hash, v3);
            hash = MergeRound(hash, v4);
        }
        else
        {
            hash = seed + Prime5;
        }

        hash += (ulong)length;

        while (data.Length >= 8)
        {
            hash ^= Round(0, BinaryPrimitives.ReadUInt64LittleEndian(data));
            hash = RotateLeft(hash, 27) * Prime1 + Prime4;
            data = data.Slice(8);
        }

        if (data.Length >= 4)
        {
            hash ^= BinaryPrimitives.ReadUInt32LittleEndian(data) * Prime1;
            hash = RotateLeft(hash, 23) * Prime2 + Prime3;
            data = data.Slice(4);
        }

        foreach (var b in data)
        {
            hash ^= b * Prime5;
            hash = RotateLeft(hash, 11) * Prime1;
        }

        hash ^= hash >> 33;
        hash *= Prime2;
        hash ^= hash >> 29;
        hash *= Prime3;
        hash ^= hash >> 32;
        return hash;
    }

    private static ulong Round(ulong acc, ulong input)
    {
        acc += input * Prime2;
        acc = RotateLeft(acc, 31);
        return acc * Prime1;
    }

    private static ulong MergeRound(ulong acc, ulong value)
    {
        acc ^= Round(0, value);
        return acc * Prime1 + Prime4;
    }

    private static ulong RotateLeft(ulong value, int offset) => (value << offset) | (value >> (64 - offset));
}
