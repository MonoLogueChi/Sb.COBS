using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.HighPerformance;
#if NET5_0_OR_GREATER
using System.Runtime.InteropServices;
#endif

#pragma warning disable IDE0130
namespace Sb;
#pragma warning restore IDE0130

/// <summary>
/// </summary>
public static class COBS
{
  #region Encode

  /// <summary>
  ///   Encodes the given data using COBS.
  /// </summary>
  /// <param name="data">The data to encode.</param>
  /// <param name="writer"></param>
  /// <param name="addZeroByte">If true, appends a zero byte at the end of the encoded data.</param>
  /// <returns></returns>
  public static void Encode(ReadOnlySpan<byte> data, ArrayBufferWriter<byte> writer, bool addZeroByte = true)
  {
    if (data.Length == 0)
      throw new ArgumentException("Data to encode cannot be null or empty.", nameof(data));

    var blockStartIndex = 0;

    for (var i = 0; i < data.Length; i++)
      if (data[i] == 0)
      {
        while (i - blockStartIndex >= 254)
        {
          writer.Write<byte>(0xFF);
          var block = data.Slice(blockStartIndex, 254);
          writer.Write(block);
          blockStartIndex += 254;
        }

        writer.Write((byte)(i - blockStartIndex + 1));
        var subBlock = data.Slice(blockStartIndex, i - blockStartIndex);
        writer.Write(subBlock);
        blockStartIndex = i + 1;
      }

    while (data.Length - blockStartIndex >= 254)
    {
      writer.Write<byte>(0xFF);
      var block = data.Slice(blockStartIndex, 254);
      writer.Write(block);
      blockStartIndex += 254;
    }

    writer.Write((byte)(data.Length - blockStartIndex + 1));
    var finalBlock = data.Slice(blockStartIndex, data.Length - blockStartIndex);
    writer.Write(finalBlock);

    if (addZeroByte) writer.Write<byte>(0);
  }

  /// <summary>
  ///   Encodes the given data using COBS.
  /// </summary>
  /// <param name="data">The data to encode.</param>
  /// <param name="addZeroByte">If true, appends a zero byte at the end of the encoded data.</param>
  /// <returns>The COBS-encoded byte array.</returns>
  public static byte[] Encode(ReadOnlySpan<byte> data, bool addZeroByte = true)
  {
    var writer = new ArrayBufferWriter<byte>(data.Length + 1);
    Encode(data, writer, addZeroByte);
    return writer.WrittenSpan.ToArray();
  }

  /// <summary>
  ///   Encodes the given data using COBS.
  /// </summary>
  /// <param name="data">The data to encode.</param>
  /// <param name="addZeroByte">If true, appends a zero byte at the end of the encoded data.</param>
  /// <returns>The COBS-encoded byte array.</returns>
  public static byte[] Encode(byte[] data, bool addZeroByte = true)
  {
    return Encode(new ReadOnlySpan<byte>(data), addZeroByte);
  }

  /// <summary>
  ///   Encodes the given data using COBS.
  /// </summary>
  /// <param name="data"></param>
  /// <param name="addZeroByte"></param>
  /// <returns></returns>
  public static byte[] Encode(List<byte> data, bool addZeroByte = true)
  {
#if NET5_0_OR_GREATER
    return Encode(CollectionsMarshal.AsSpan(data), addZeroByte);
#else
    return Encode(data.ToArray(), addZeroByte);
#endif
  }

  /// <summary>
  ///   Encodes the given data using COBS.
  /// </summary>
  /// <param name="data">The data to encode, as a List of bytes.</param>
  /// <param name="addZeroByte">If true, appends a zero byte at the end of the encoded data.</param>
  /// <returns>The COBS-encoded byte array.</returns>
  public static byte[] Encode(IEnumerable<byte> data, bool addZeroByte = true)
  {
    return Encode(data.ToArray(), addZeroByte);
  }

  #endregion

  #region Decode

  /// <summary>
  ///   Decodes the given COBS-encoded data.
  /// </summary>
  /// <param name="data">The COBS-encoded data to decode.</param>
  /// <param name="writer"></param>
  /// <param name="withZeroByte">If true, removes the trailing zero byte from the decoded data.</param>
  /// <returns></returns>
  /// <exception cref="ArgumentException">Thrown when the COBS encoded data is invalid.</exception>
  public static void Decode(ReadOnlySpan<byte> data, ArrayBufferWriter<byte> writer, bool withZeroByte = false)
  {
    var span = data;

    if (data.Length == 0)
      throw new ArgumentException("Data to decode cannot be null or empty.", nameof(data));

    // Check if the data is COBS encoded
    if (withZeroByte && data[^1] != 0x00) throw new ArgumentException("Invalid COBS encoded data.", nameof(data));

    // Remove the trailing zero byte if it exists
    if (data[^1] == 0x00) span = data[..^1];

    var blockStartIndex = 0;
    var spanLength = span.Length;

    while (blockStartIndex < spanLength)
    {
      var distance = span[blockStartIndex];
      var blockEnd = blockStartIndex + distance;


      if (distance < 1 || blockEnd > spanLength)
        throw new ArgumentException("Invalid COBS encoded data.", nameof(data));


      if (distance > 1) writer.Write(span.Slice(blockStartIndex + 1, distance - 1));


      if (distance < 0xFF && blockEnd < spanLength) writer.Write<byte>(0);

      blockStartIndex = blockEnd;
    }
  }

  /// <summary>
  ///   Decodes the given COBS-encoded data.
  /// </summary>
  /// <param name="data">The COBS-encoded data to decode.</param>
  /// <param name="withZeroByte">If true, removes the trailing zero byte from the decoded data.</param>
  /// <returns>The decoded byte array.</returns>
  /// <exception cref="ArgumentException">Thrown when the COBS encoded data is invalid.</exception>
  public static byte[] Decode(ReadOnlySpan<byte> data, bool withZeroByte = false)
  {
    var writer = new ArrayBufferWriter<byte>(data.Length);
    Decode(data, writer, withZeroByte);
    return writer.WrittenSpan.ToArray();
  }

  /// <summary>
  ///   Decodes the given COBS-encoded data.
  /// </summary>
  /// <param name="data">The COBS-encoded data to decode.</param>
  /// <param name="withZeroByte">If true, removes the trailing zero byte from the decoded data.</param>
  /// <returns>The decoded byte array.</returns>
  /// <exception cref="ArgumentException">Thrown when the COBS encoded data is invalid.</exception>
  public static byte[] Decode(byte[] data, bool withZeroByte = false)
  {
    return Decode(new ReadOnlySpan<byte>(data), withZeroByte);
  }

  /// <summary>
  ///   Decodes the given COBS-encoded data.
  /// </summary>
  /// <param name="data">The COBS-encoded data to decode.</param>
  /// <param name="withZeroByte">If true, removes the trailing zero byte from the decoded data.</param>
  /// <returns>The decoded byte array.</returns>
  public static byte[] Decode(List<byte> data, bool withZeroByte = true)
  {
#if NET5_0_OR_GREATER
    return Encode(CollectionsMarshal.AsSpan(data), withZeroByte);
#else
    return Decode(data.ToArray(), withZeroByte);
#endif
  }

  /// <summary>
  ///   Decodes the given COBS-encoded data.
  /// </summary>
  /// <param name="data">The COBS-encoded data to decode, as a List of bytes.</param>
  /// <param name="withZeroByte">If true, removes the trailing zero byte from the decoded data.</param>
  /// <returns>The decoded byte array.</returns>
  public static byte[] Decode(IEnumerable<byte> data, bool withZeroByte = false)
  {
    return Decode(data.ToArray(), withZeroByte);
  }

  #endregion
}