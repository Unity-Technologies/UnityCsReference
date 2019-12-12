// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
                return DecodeString(bytes);
            }
        }

        public override string ToString() { return text; }

        public TextAsset() : this(CreateOptions.CreateNativeObject, null)
        {
        }

        public TextAsset(string text) : this(CreateOptions.CreateNativeObject, text)
        {
        }

        internal TextAsset(CreateOptions options, string text)
        {
            if (options == CreateOptions.CreateNativeObject)
            {
                Internal_CreateInstance(this, text);
            }
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
            Encoding encoding = null;
            int preambleLength = 0;
            int encodingLookupLength = EncodingUtility.encodingLookup.Length;

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
                        string text = tempEncoding.GetString(bytes, preambleLength, bytes.Length - preambleLength);
                        encoding = tempEncoding;
                        break;
                    }
                    catch {};
                }
            }

            if (encoding == null)
            {
                encoding = EncodingUtility.targetEncoding;
                preambleLength = 0;
            }

            return encoding.GetString(bytes, preambleLength, bytes.Length - preambleLength);
        }
    }
}
