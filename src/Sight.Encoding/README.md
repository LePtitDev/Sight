# Sight.Encoding

[![](https://img.shields.io/nuget/v/Sight.Encoding.svg)](https://www.nuget.org/packages/Sight.Encoding/)

This library implements helpers to encode and decode binary data with an explicit byte order.

## Features

* Little and big endian (`Endianness` enum), built on `BinaryPrimitives`
* Supported values:
  * Integers from 8 to 64 bits (128 bits on .NET 8+), `bool`, `char`
  * `float`, `double` (`Half` on .NET 8+), `decimal`, `Guid` (big endian follows RFC 4122)
  * Variable-length integers (LEB128, ZigZag for signed values)
  * UTF-8 strings prefixed by their length (same format as `BinaryWriter.Write(string)`)
* Several targets:
  * `BinaryEncoding`: static methods on `Span<byte>`/`ReadOnlySpan<byte>` (and `byte[]`), with `Try*` variants that never throw for a too small buffer
  * `SpanReader`/`SpanWriter`: cursors over a span, advancing after each value
  * `Stream` extension methods, synchronous and asynchronous (`ValueTask`, `CancellationToken`)
  * `BinaryReader`/`BinaryWriter` extension methods to read/write big endian values
* Failing writes never write partially, reading past the end of a stream throws `EndOfStreamException`

## How to use?

### Spans

```csharp
Span<byte> buffer = stackalloc byte[8];
BinaryEncoding.WriteUInt32(buffer, 0x01020304, Endianness.Big);
var value = BinaryEncoding.ReadUInt32(buffer, Endianness.Big);

if (!BinaryEncoding.TryReadUInt64(received, out var id))
    return; // Wait for more data
```

### Cursors

```csharp
var writer = new SpanWriter(buffer, Endianness.Big);
writer.WriteUInt16(header);
writer.WriteVarUInt32((uint)payload.Length);
writer.WriteBytes(payload);
var packet = writer.WrittenSpan;

var reader = new SpanReader(packet, Endianness.Big);
var header = reader.ReadUInt16();
var payload = reader.ReadBytes((int)reader.ReadVarUInt32());
```

### Streams

```csharp
stream.WriteInt32(42, Endianness.Big);
stream.WriteString("Hello");
await stream.WriteDoubleAsync(3.14, cancellationToken: token);

var number = stream.ReadInt32(Endianness.Big);
var text = stream.ReadString(maxByteCount: 1024);
var real = await stream.ReadDoubleAsync(cancellationToken: token);
```

### Binary reader/writer

```csharp
writer.WriteUInt32(value, Endianness.Big);
var value = reader.ReadUInt32(Endianness.Big);
```

## Migration from `BinaryUtility`

`BinaryUtility` is obsolete and forwards to the new API:

| Before | After |
| --- | --- |
| `BinaryUtility.WriteUInt32ToBuffer(value, buffer, offset, littleEndian: false)` | `BinaryEncoding.WriteUInt32(buffer.AsSpan(offset), value, Endianness.Big)` |
| `BinaryUtility.ReadUInt32FromStream(stream)` | `stream.ReadUInt32()` |
| `BinaryUtility.WriteUInt32ToBinaryWriter(value, writer, littleEndian: false)` | `writer.WriteUInt32(value, Endianness.Big)` |
| `BinaryUtility.ReverseEndianness(value)` | `BinaryPrimitives.ReverseEndianness(value)` |

Behavior changes:

* Reading past the end of a stream throws `EndOfStreamException` instead of returning garbage
* A too small buffer throws `ArgumentOutOfRangeException` before anything is written
