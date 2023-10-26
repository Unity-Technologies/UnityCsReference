// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine
{
    public partial class TextAsset : Object
    {
        // Used by MonoScript constructor to avoid creating native TextAsset object.
        internal enum CreateOptions
        {
            None = 0,
            CreateNativeObject = 1
        }

        // The text contents of the .txt file as a string. (RO)
        public string text
        {
            get
            {
                var localBytes = bytes;
                return localBytes.Length == 0 ? string.Empty : DecodeString(localBytes);
            }
        }

        public long dataSize => GetDataSize();

        public override string ToString() { return text; }

        public TextAsset() : this(CreateOptions.CreateNativeObject, (string)null)
        {
        }

        public TextAsset(string text) : this(CreateOptions.CreateNativeObject, text)
        {
        }

        public TextAsset(ReadOnlySpan<byte> bytes) : this(CreateOptions.CreateNativeObject, bytes)
        {

        }

        internal TextAsset(CreateOptions options, string text)
        {
            if (options == CreateOptions.CreateNativeObject)
            {
                Internal_CreateInstance(this, text);
            }
        }

        internal TextAsset(CreateOptions options, ReadOnlySpan<byte> bytes)
        {
            if (options == CreateOptions.CreateNativeObject)
            {
                Internal_CreateInstanceFromBytes(this, bytes);
            }
        }

        public unsafe NativeArray<T> GetData<T>() where T : struct
        {
            long size = GetDataSize();
            long stride = UnsafeUtility.SizeOf<T>();
            if (size % stride != 0)
                throw new ArgumentException($"Type passed to {nameof(GetData)} can't capture the asset data. Data size is {size} which is not a multiple of type size {stride}");
            var arrSize = size / stride;

            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>((void*)GetDataPtr(), (int)arrSize, Allocator.None);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, GetSafetyHandle(this));
            return array;
        }

        internal string GetPreview(int maxChars)
        {
            // Take 4 times as much bytes because our widest character could be 4 bytes long
            return DecodeString(GetPreviewBytes(maxChars * 4));
        }

        static class EncodingUtility
        {
            internal static readonly KeyValuePair<byte[], Encoding>[] encodingLookup;

            internal static readonly Encoding targetEncoding =
                Encoding.GetEncoding(Encoding.UTF8.CodePage,
                    new EncoderReplacementFallback("�"),
                    new DecoderReplacementFallback("�"));

            static EncodingUtility()
            {
                Encoding utf32BE = new UTF32Encoding(true, true, true);
                Encoding utf32LE = new UTF32Encoding(false, true, true);
                Encoding utf16BE = new UnicodeEncoding(true, true, true);
                Encoding utf16LE = new UnicodeEncoding(false, true, true);
                Encoding utf8BOM = new UTF8Encoding(true, true);

                encodingLookup = new KeyValuePair<byte[], Encoding>[]
                {
                    new KeyValuePair<byte[], Encoding>(utf32BE.GetPreamble(), utf32BE),
                    new KeyValuePair<byte[], Encoding>(utf32LE.GetPreamble(), utf32LE),
                    new KeyValuePair<byte[], Encoding>(utf16BE.GetPreamble(), utf16BE),
                    new KeyValuePair<byte[], Encoding>(utf16LE.GetPreamble(), utf16LE),
                    new KeyValuePair<byte[], Encoding>(utf8BOM.GetPreamble(), utf8BOM),
                };
            }
        }

        internal static string DecodeString(byte[] bytes)
        {
            int encodingLookupLength = EncodingUtility.encodingLookup.Length;

            int preambleLength;
            for (int i = 0; i < encodingLookupLength; i++)
            {
                byte[] preamble = EncodingUtility.encodingLookup[i].Key;
                preambleLength = preamble.Length;
                if (bytes.Length >= preambleLength)
                {
                    for (int j = 0; j < preambleLength; j++)
                    {
                        if (preamble[j] != bytes[j])
                        {
                            preambleLength = -1;
                        }
                    }

                    if (preambleLength < 0) continue;

                    try
                    {
                        Encoding tempEncoding = EncodingUtility.encodingLookup[i].Value;
                        return tempEncoding.GetString(bytes, preambleLength, bytes.Length - preambleLength);
                    }
                    catch {};
                }
            }

            preambleLength = 0;
            Encoding encoding = EncodingUtility.targetEncoding;
            return encoding.GetString(bytes, preambleLength, bytes.Length - preambleLength);
        }
    }
}
