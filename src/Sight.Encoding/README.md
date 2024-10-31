# Sight.Encoding

[![](https://img.shields.io/nuget/v/Sight.Encoding.svg)](https://www.nuget.org/packages/Sight.Encoding/)

This library implement helpers methods to encode/decode binary data.

## Features

* Support of little/big endian
* Methods for:
  * Buffer (bytes array)
  * `Stream`
  * `BinaryWriter` and `BinaryWriter`
* Allow to encode/decode integers and floating point values

## How to use?

```csharp
public class Encoder
{
    private readonly Stream _stream;

    public Encoder(Stream stream)
    {
        _stream = stream;
    }

    public void Write(uint value)
    {
        BinaryUtility.WriteUInt32ToStream(value, _stream, littleEndian: true);
    }

    public uint Read()
    {
        return BinaryUtility.ReadUInt32FromStream(_stream, littleEndian: true);
    }
}
```
