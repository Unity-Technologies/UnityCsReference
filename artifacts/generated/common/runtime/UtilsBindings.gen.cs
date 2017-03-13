// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace UnityEngine
{

#pragma warning disable 414
[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct Hash128
{
    public Hash128(uint u32_0, uint u32_1, uint u32_2, uint u32_3)
        {
            m_u32_0 = u32_0;
            m_u32_1 = u32_1;
            m_u32_2 = u32_2;
            m_u32_3 = u32_3;
        }
    
    
    uint m_u32_0;
    uint m_u32_1;
    uint m_u32_2;
    uint m_u32_3;
    
    
    public bool isValid
        {
            get
            {
                return m_u32_0 != 0
                    || m_u32_1 != 0
                    || m_u32_2 != 0
                    || m_u32_3 != 0;
            }
        }
    
    
    public override string ToString()
        {
            return Internal_Hash128ToString(m_u32_0, m_u32_1, m_u32_2, m_u32_3);
        }
    
    
    public static Hash128 Parse (string hashString) {
        Hash128 result;
        INTERNAL_CALL_Parse ( hashString, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Parse (string hashString, out Hash128 value);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  string Internal_Hash128ToString (uint d0, uint d1, uint d2, uint d3) ;

    public override bool Equals(object obj)
        {
            return obj is Hash128 && this == (Hash128)obj;
        }
    
    
    public override int GetHashCode()
        {
            return m_u32_0.GetHashCode() ^ m_u32_1.GetHashCode() ^ m_u32_2.GetHashCode() ^ m_u32_3.GetHashCode();
        }
    
    
    public static bool operator==(Hash128 hash1, Hash128 hash2)
        {
            return (hash1.m_u32_0 == hash2.m_u32_0 && hash1.m_u32_1 == hash2.m_u32_1 && hash1.m_u32_2 == hash2.m_u32_2 && hash1.m_u32_3 == hash2.m_u32_3);
        }
    
    
    public static bool operator!=(Hash128 hash1, Hash128 hash2)
        {
            return !(hash1 == hash2);
        }
    
    
}

public enum AudioType
{
    
    UNKNOWN = 0,
    
    ACC = 1,
    
    AIFF = 2,
    
    
    
    
    
    
    
    
    
    IT = 10,
    
    
    MOD = 12,
    
    MPEG = 13,
    
    OGGVORBIS = 14,
    
    
    
    S3M = 17,
    
    
    
    WAV = 20,
    
    XM = 21,
    
    XMA = 22,
    VAG = 23,
    
    AUDIOQUEUE = 24,
    
    
    
    
    
    
}


#pragma warning restore 414



[UsedByNativeCode]
public sealed partial class WWW : IDisposable
{
    [RequiredByNativeCode] internal IntPtr m_Ptr;
    
    
    public void Dispose()
        {
            DestroyWWW(true);
        }
    
    
    ~WWW()
        {
            DestroyWWW(false);
        }
    
    
    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void DestroyWWW (bool cancel) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void InitWWW (string url , byte[] postData, string[] iHeaders) ;

    public WWW(string url)
        {
            InitWWW(url, null, null);
        }
    
    
    public WWW(string url, WWWForm form)
        {
            string[] flattenedHeaders = FlattenedHeadersFrom(form.headers);

            if (enforceWebSecurityRestrictions())
            {
                CheckSecurityOnHeaders(flattenedHeaders);
            }

            InitWWW(url, form.data, flattenedHeaders);
        }
    
    
    public WWW(string url, byte[] postData)
        {
            InitWWW(url, postData, null);
        }
    
    
    [System.Obsolete ("This overload is deprecated. Use UnityEngine.WWW.WWW(string, byte[], System.Collections.Generic.Dictionary<string, string>) instead.", true)]
    public WWW(string url, byte[] postData, Hashtable headers) { Debug.LogError("This overload is deprecated. Use UnityEngine.WWW.WWW(string, byte[], System.Collections.Generic.Dictionary<string, string>) instead");  }
    
    
    public WWW(string url, byte[] postData, Dictionary<string, string> headers)
        {
            string[] flattenedHeaders = FlattenedHeadersFrom(headers);

            if (enforceWebSecurityRestrictions())
            {
                CheckSecurityOnHeaders(flattenedHeaders);
            }

            InitWWW(url, postData, flattenedHeaders);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal bool enforceWebSecurityRestrictions () ;

    
    [uei.ExcludeFromDocs]
public static string EscapeURL (string s) {
    Encoding e = System.Text.Encoding.UTF8;
    return EscapeURL ( s, e );
}

public static string EscapeURL(string s, [uei.DefaultValue("System.Text.Encoding.UTF8")]  Encoding e )
        {
            if (s == null)
                return null;

            if (s == "")
                return "";

            if (e == null)
                return null;

            return WWWTranscoder.URLEncode(s, e);
        }

    
    
    
    
    [uei.ExcludeFromDocs]
public static string UnEscapeURL (string s) {
    Encoding e = System.Text.Encoding.UTF8;
    return UnEscapeURL ( s, e );
}

public static string UnEscapeURL(string s, [uei.DefaultValue("System.Text.Encoding.UTF8")]  Encoding e )
        {
            if (null == s)
                return null;

            if (s.IndexOf('%') == -1 && s.IndexOf('+') == -1)
                return s;

            return WWWTranscoder.URLDecode(s, e);

        }

    
    
    
    
            public System.Collections.Generic.Dictionary<string, string> responseHeaders
        {
            get
            {
                if (!isDone) throw new UnityException("WWW is not finished downloading yet");
                return ParseHTTPHeaderString(responseHeadersString);
            }
        }
    
    
    private extern  string responseHeadersString
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public string text
        {
            get
            {
                if (!isDone) throw new UnityException("WWW is not ready downloading yet");
                var myBytes = bytes;
                return GetTextEncoder().GetString(myBytes, 0, myBytes.Length);
            }
        }
    internal static System.Text.Encoding DefaultEncoding
        {
            get
            {
                return System.Text.Encoding.ASCII;
            }
        }
    
    
    private Encoding GetTextEncoder()
        {
            string contentType = null;
            if (responseHeaders.TryGetValue("CONTENT-TYPE", out contentType))
            {
                int charsetKeyIndex = contentType.IndexOf("charset", StringComparison.OrdinalIgnoreCase);
                if (charsetKeyIndex > -1)
                {
                    int charsetValueIndex = contentType.IndexOf('=', charsetKeyIndex);
                    if (charsetValueIndex > -1)
                    {
                        string encoding = contentType.Substring(charsetValueIndex + 1).Trim().Trim(new[] {'\'', '"'}).Trim();
                        int semicolonIndex = encoding.IndexOf(';');
                        if (semicolonIndex > -1)
                            encoding = encoding.Substring(0, semicolonIndex);
                        try
                        {
                            return System.Text.Encoding.GetEncoding(encoding);
                        }
                        catch (Exception)
                        {
                            Debug.Log("Unsupported encoding: '" + encoding + "'");
                        }
                    }
                }
            }
            return System.Text.Encoding.UTF8;
        }
    
    
    [System.Obsolete ("Please use WWW.text instead")]
    public string data { get { return text; } }
    
    
    public extern  byte[] bytes
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  int size
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    
    
    
    public extern  string error
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [Obsolete("Obsolete msg (UnityUpgradable) -> * UnityEngine.ImageConversion.GetTexture(UnityEngine.WWW)", true)]
    public Texture2D texture
        {
            get
            {
                throw new NotSupportedException("WWW.texture is obsolete. Use ImageConversion.GetTexture(UnityEngine.WWW) instead.");
            }
        }
    
    
    [Obsolete("Obsolete msg (UnityUpgradable) -> * UnityEngine.ImageConversion.GetTextureNonReadable(UnityEngine.WWW)", true)]
    public Texture2D textureNonReadable
        {
            get
            {
                throw new NotSupportedException("WWW.textureNonReadable is obsolete. Use ImageConversion.GetTextureNonReadable(UnityEngine.WWW) instead.");
            }
        }
    
    
    
    [Obsolete("Obsolete msg (UnityUpgradable) -> * UnityEngine.WWWAudioExtensions.GetAudioClip(UnityEngine.WWW)", true)]
            public Object audioClip
        {
            get { return null; }
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal Object GetAudioClipInternal (bool threeD, bool stream, bool compressed, AudioType audioType) ;

    [Obsolete("Obsolete msg (UnityUpgradable) -> * UnityEngine.WWWAudioExtensions.GetMovieTexture(UnityEngine.WWW)", true)]
            public Object movie
        {
            get { return null; }
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal Object GetMovieTextureInternal () ;

    public extern  bool isDone
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [System.Obsolete ("All blocking WWW functions have been deprecated, please use one of the asynchronous functions instead.", true)]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string GetURL (string url) ;

    public extern  float progress
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  float uploadProgress
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  int bytesDownloaded
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Obsolete msg (UnityUpgradable) -> * UnityEngine.WWWAudioExtensions.GetAudioClip(UnityEngine.WWW)", true)]
    public Object oggVorbis { get { return null; } }
    
    
    [System.Obsolete ("LoadUnityWeb is no longer supported. Please use javascript to reload the web player on a different url instead", true)]
public void LoadUnityWeb() {}
    
    
    
    public extern  string url
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  AssetBundle assetBundle
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  ThreadPriority threadPriority
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    internal  WWW (string url, Hash128 hash, uint crc) {
        INTERNAL_CALL_WWW ( this, url, ref hash, crc );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_WWW (WWW self, string url, ref Hash128 hash, uint crc);
    [uei.ExcludeFromDocs]
public static WWW LoadFromCacheOrDownload (string url, int version) {
    uint crc = 0;
    return LoadFromCacheOrDownload ( url, version, crc );
}

public static WWW LoadFromCacheOrDownload(string url, int version, [uei.DefaultValue("0")]  uint crc )
        {
            Hash128 tempHash = new Hash128(0, 0, 0, (uint)version);
            return LoadFromCacheOrDownload(url, tempHash, crc);
        }

    
    
    [uei.ExcludeFromDocs]
public static WWW LoadFromCacheOrDownload (string url, Hash128 hash) {
    uint crc = 0;
    return LoadFromCacheOrDownload ( url, hash, crc );
}

public static WWW LoadFromCacheOrDownload(string url, Hash128 hash, [uei.DefaultValue("0")]  uint crc )
        {
            return new WWW(url, hash, crc);
        }

    
    
}

public sealed partial class WWWForm
{
    private List<byte[]> formData; 
    private List<string> fieldNames; 
    private List<string> fileNames; 
    private List<string> types; 
    private byte[] boundary;
    private bool containsFiles = false;
    
    
    public WWWForm()
        {
            formData = new List<byte[]>();
            fieldNames = new List<string>();
            fileNames = new List<string>();
            types = new List<string>();

            boundary = new byte[40];
            for (int i = 0; i < 40; i++)
            {
                int randomChar = Random.Range(48, 110);
                if (randomChar > 57) 
                    randomChar += 7;
                if (randomChar > 90) 
                    randomChar += 6;
                boundary[i] = (byte)randomChar;
            }

        }
    
    
    [uei.ExcludeFromDocs]
public void AddField (string fieldName, string value) {
    Encoding e = System.Text.Encoding.UTF8;
    AddField ( fieldName, value, e );
}

public void AddField(string fieldName, string value, [uei.DefaultValue("System.Text.Encoding.UTF8")]  Encoding e )
        {
            fieldNames.Add(fieldName);
            fileNames.Add(null);
            formData.Add(e.GetBytes(value));
            types.Add("text/plain; charset=\"" + e.WebName + "\"");
        }

    
    
    public void AddField(string fieldName, int i)
        {
            AddField(fieldName, i.ToString());
        }
    
    
    [uei.ExcludeFromDocs]
public void AddBinaryData (string fieldName, byte[] contents, string fileName ) {
    string mimeType = null;
    AddBinaryData ( fieldName, contents, fileName, mimeType );
}

[uei.ExcludeFromDocs]
public void AddBinaryData (string fieldName, byte[] contents) {
    string mimeType = null;
    string fileName = null;
    AddBinaryData ( fieldName, contents, fileName, mimeType );
}

public void AddBinaryData(string fieldName, byte[] contents, [uei.DefaultValue("null")]  string fileName , [uei.DefaultValue("null")]  string mimeType )
        {
            containsFiles = true;

            bool isPng = contents.Length > 8 && contents[0] == 0x89 && contents[1] == 0x50 && contents[2] == 0x4e && contents[3] == 0x47
                && contents[4] == 0x0d && contents[5] == 0x0a && contents[6] == 0x1a && contents[7] == 0x0a;
            if (fileName == null)
            {
                fileName = fieldName + (isPng ? ".png" : ".dat");
            }
            if (mimeType == null)
            {
                if (isPng)
                    mimeType = "image/png";
                else
                    mimeType = "application/octet-stream";
            }

            fieldNames.Add(fieldName);
            fileNames.Add(fileName);
            formData.Add(contents);
            types.Add(mimeType);
        }

    
    
    public Dictionary<string, string> headers
        {
            get
            {
                Dictionary<string, string> retval = new Dictionary<string, string>();
                if (containsFiles)
                    retval["Content-Type"] = "multipart/form-data; boundary=\"" + System.Text.Encoding.UTF8.GetString(boundary, 0, boundary.Length) + "\"";
                else
                    retval["Content-Type"] = "application/x-www-form-urlencoded";
                return retval;
            }
        }
    
    
    public byte[] data
        {
            get
            {

                if (containsFiles)
                {
                    byte[] dDash = WWW.DefaultEncoding.GetBytes("--");
                    byte[] crlf = WWW.DefaultEncoding.GetBytes("\r\n");
                    byte[] contentTypeHeader = WWW.DefaultEncoding.GetBytes("Content-Type: ");
                    byte[] dispositionHeader = WWW.DefaultEncoding.GetBytes("Content-disposition: form-data; name=\"");
                    byte[] endQuote = WWW.DefaultEncoding.GetBytes("\"");
                    byte[] fileNameField = WWW.DefaultEncoding.GetBytes("; filename=\"");


                    using (MemoryStream memStream = new MemoryStream(1024))
                    {
                        for (int i = 0; i < formData.Count; i++)
                        {
                            memStream.Write(crlf, 0, (int)crlf.Length);
                            memStream.Write(dDash, 0, (int)dDash.Length);
                            memStream.Write(boundary, 0, (int)boundary.Length);
                            memStream.Write(crlf, 0, (int)crlf.Length);
                            memStream.Write(contentTypeHeader, 0, (int)contentTypeHeader.Length);

                            byte[] type = System.Text.Encoding.UTF8.GetBytes((string)types[i]);
                            memStream.Write(type, 0, (int)type.Length);
                            memStream.Write(crlf, 0, (int)crlf.Length);
                            memStream.Write(dispositionHeader, 0, (int)dispositionHeader.Length);

                            string headerName = System.Text.Encoding.UTF8.HeaderName;
                            string encodedFieldName = (string)fieldNames[i];
                            if (!WWWTranscoder.SevenBitClean(encodedFieldName, System.Text.Encoding.UTF8) || encodedFieldName.IndexOf("=?") > -1)
                            {
                                encodedFieldName = "=?" + headerName + "?Q?" + WWWTranscoder.QPEncode(encodedFieldName, System.Text.Encoding.UTF8) + "?=";
                            }
                            byte[] name = System.Text.Encoding.UTF8.GetBytes(encodedFieldName);
                            memStream.Write(name, 0, (int)name.Length);
                            memStream.Write(endQuote, 0, (int)endQuote.Length);

                            if (fileNames[i] != null)
                            {
                                string encodedFileName = (string)fileNames[i];
                                if (!WWWTranscoder.SevenBitClean(encodedFileName, System.Text.Encoding.UTF8) || encodedFileName.IndexOf("=?") > -1)
                                {
                                    encodedFileName = "=?" + headerName + "?Q?" + WWWTranscoder.QPEncode(encodedFileName, System.Text.Encoding.UTF8) + "?=";
                                }
                                byte[] fileName = System.Text.Encoding.UTF8.GetBytes(encodedFileName);

                                memStream.Write(fileNameField, 0, (int)fileNameField.Length);
                                memStream.Write(fileName, 0, (int)fileName.Length);
                                memStream.Write(endQuote, 0, (int)endQuote.Length);

                            }
                            memStream.Write(crlf, 0, (int)crlf.Length);
                            memStream.Write(crlf, 0, (int)crlf.Length);

                            byte[] formBytes = (byte[])formData[i];
                            memStream.Write(formBytes, 0, (int)formBytes.Length);
                        }
                        memStream.Write(crlf, 0, (int)crlf.Length);
                        memStream.Write(dDash, 0, (int)dDash.Length);
                        memStream.Write(boundary, 0, (int)boundary.Length);
                        memStream.Write(dDash, 0, (int)dDash.Length);
                        memStream.Write(crlf, 0, (int)crlf.Length);

                        return memStream.ToArray();
                    }
                }
                else
                {
                    byte[] ampersand = WWW.DefaultEncoding.GetBytes("&");
                    byte[] equal = WWW.DefaultEncoding.GetBytes("=");

                    using (MemoryStream memStream = new MemoryStream(1024))
                    {
                        for (int i = 0; i < formData.Count; i++)
                        {
                            byte[] name = WWWTranscoder.URLEncode(System.Text.Encoding.UTF8.GetBytes((string)fieldNames[i]));
                            byte[] formBytes = (byte[])formData[i];
                            byte[] value = WWWTranscoder.URLEncode(formBytes);

                            if (i > 0) memStream.Write(ampersand, 0, (int)ampersand.Length);
                            memStream.Write(name, 0, (int)name.Length);
                            memStream.Write(equal, 0, (int)equal.Length);
                            memStream.Write(value, 0, (int)value.Length);
                        }
                        return memStream.ToArray();
                    }
                }
            }
        }
    
    
    
}

internal sealed partial class WWWTranscoder
{
    private static byte[] ucHexChars = WWW.DefaultEncoding.GetBytes("0123456789ABCDEF");
    private static byte[] lcHexChars = WWW.DefaultEncoding.GetBytes("0123456789abcdef");
    private static byte urlEscapeChar = (byte)'%';
    private static byte urlSpace = (byte)'+';
    private static byte[] urlForbidden = WWW.DefaultEncoding.GetBytes("@&;:<>=?\"'/\\!#%+$,{}|^[]`");
    private static byte qpEscapeChar = (byte)'=';
    private static byte qpSpace = (byte)'_';
    private static byte[] qpForbidden = WWW.DefaultEncoding.GetBytes("&;=?\"'%+_");
    
    
    private static byte Hex2Byte(byte[] b, int offset)
        {
            byte result = (byte)0;

            for (int i = offset; i < offset + 2; i++)
            {
                result *= 16;
                int d = b[i];

                if (d >= 48 && d <= 57) 
                    d -= 48;
                else if (d >= 65 && d <= 75) 
                    d -= 55;
                else if (d >= 97 && d <= 102) 
                    d -= 87;
                if (d > 15)
                {
                    return 63; 
                }

                result += (byte)d;
            }

            return result;
        }
    
    
    private static byte[] Byte2Hex(byte b, byte[] hexChars)
        {
            byte[] dest = new byte[2];
            dest[0] = hexChars[b >> 4];
            dest[1] = hexChars[b & 0xf];
            return dest;
        }
    
    
    [uei.ExcludeFromDocs]
public static string URLEncode (string toEncode) {
    Encoding e = Encoding.UTF8;
    return URLEncode ( toEncode, e );
}

public static string URLEncode(string toEncode, [uei.DefaultValue("Encoding.UTF8")]  Encoding e )
        {
            byte[] data = Encode(e.GetBytes(toEncode), urlEscapeChar, urlSpace, urlForbidden, false);
            return WWW.DefaultEncoding.GetString(data, 0, data.Length);
        }

    
    
    public static byte[] URLEncode(byte[] toEncode)
        {
            return Encode(toEncode, urlEscapeChar, urlSpace, urlForbidden, false);
        }
    
    
    [uei.ExcludeFromDocs]
public static string QPEncode (string toEncode) {
    Encoding e = Encoding.UTF8;
    return QPEncode ( toEncode, e );
}

public static string QPEncode(string toEncode, [uei.DefaultValue("Encoding.UTF8")]  Encoding e )
        {
            byte[] data = Encode(e.GetBytes(toEncode), qpEscapeChar, qpSpace, qpForbidden, true);
            return WWW.DefaultEncoding.GetString(data, 0, data.Length);
        }

    
    
    public static byte[] QPEncode(byte[] toEncode)
        {
            return Encode(toEncode, qpEscapeChar, qpSpace, qpForbidden, true);
        }
    
    
    public static byte[] Encode(byte[] input, byte escapeChar, byte space, byte[] forbidden, bool uppercase)
        {
            using (MemoryStream memStream = new MemoryStream(input.Length * 2))
            {
                for (int i = 0; i < input.Length; i++)
                {
                    if (input[i] == 32)
                    {
                        memStream.WriteByte(space);
                    }
                    else if (input[i] < 32 || input[i] > 126 || ByteArrayContains(forbidden, input[i]))
                    {
                        memStream.WriteByte(escapeChar);
                        memStream.Write(Byte2Hex(input[i], uppercase ? ucHexChars : lcHexChars), 0, 2);
                    }
                    else
                    {
                        memStream.WriteByte(input[i]);
                    }
                }

                return memStream.ToArray();
            }

        }
    
    
    private static bool ByteArrayContains(byte[] array, byte b)
        {
            var arrayLength = array.Length;

            for (int i = 0; i < arrayLength; i++)
            {
                if (array[i] == b)
                    return true;
            }

            return false;
        }
    
    
    [uei.ExcludeFromDocs]
public static string URLDecode (string toEncode) {
    Encoding e = Encoding.UTF8;
    return URLDecode ( toEncode, e );
}

public static string URLDecode(string toEncode, [uei.DefaultValue("Encoding.UTF8")]  Encoding e )
        {
            byte[] data = Decode(WWW.DefaultEncoding.GetBytes(toEncode), urlEscapeChar, urlSpace);
            return e.GetString(data, 0, data.Length);
        }

    
    
    public static byte[] URLDecode(byte[] toEncode)
        {
            return Decode(toEncode, urlEscapeChar, urlSpace);
        }
    
    
    [uei.ExcludeFromDocs]
public static string QPDecode (string toEncode) {
    Encoding e = Encoding.UTF8;
    return QPDecode ( toEncode, e );
}

public static string QPDecode(string toEncode, [uei.DefaultValue("Encoding.UTF8")]  Encoding e )
        {
            byte[] data = Decode(WWW.DefaultEncoding.GetBytes(toEncode), qpEscapeChar, qpSpace);
            return e.GetString(data, 0, data.Length);
        }

    
    
    public static byte[] QPDecode(byte[] toEncode)
        {
            return Decode(toEncode, qpEscapeChar, qpSpace);
        }
    
    
    public static byte[] Decode(byte[] input, byte escapeChar, byte space)
        {
            using (MemoryStream memStream = new MemoryStream(input.Length))
            {
                for (int i = 0; i < input.Length; i++)
                {
                    if (input[i] == space)
                    {
                        memStream.WriteByte((byte)32);
                    }
                    else if (input[i] == escapeChar && i + 2 < input.Length)
                    {
                        i++;
                        memStream.WriteByte(Hex2Byte(input, i++));
                    }
                    else
                    {
                        memStream.WriteByte(input[i]);
                    }
                }

                return memStream.ToArray();
            }

        }
    
    
    [uei.ExcludeFromDocs]
public static bool SevenBitClean (string s) {
    Encoding e = Encoding.UTF8;
    return SevenBitClean ( s, e );
}

public static bool SevenBitClean(string s, [uei.DefaultValue("Encoding.UTF8")]  Encoding e )
        {
            return SevenBitClean(e.GetBytes(s));
        }

    
    
    public static bool SevenBitClean(byte[] input)
        {
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] < 32 || input[i] > 126)
                    return false;
            }

            return true;
        }
    
    
}

internal sealed partial class UnityLogWriter : System.IO.TextWriter
{
    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void WriteStringToUnityLog (string s) ;

    
    public static void Init()
        {
            System.Console.SetOut(new UnityLogWriter());
        }
    
            public override System.Text.Encoding Encoding
        {
            get { return System.Text.Encoding.UTF8; }
        }
    public override void Write(char value)
        {
            WriteStringToUnityLog(value.ToString());
        }
    
    public override void Write(string s)
        {
            WriteStringToUnityLog(s);
        }
    
    
}

public sealed partial class UnityEventQueueSystem
{
    
    public static string GenerateEventIdForPayload(string eventPayloadName)
        {
            byte[] bs = System.Guid.NewGuid().ToByteArray();
            return string.Format("REGISTER_EVENT_ID(0x{0:X2}{1:X2}{2:X2}{3:X2}{4:X2}{5:X2}{6:X2}{7:X2}ULL,0x{8:X2}{9:X2}{10:X2}{11:X2}{12:X2}{13:X2}{14:X2}{15:X2}ULL,{16})"
                , bs[0], bs[1], bs[2], bs[3], bs[4], bs[5], bs[6], bs[7]
                , bs[8], bs[9], bs[10], bs[11], bs[12], bs[13], bs[14], bs[15]
                , eventPayloadName);
        }
    
    
    public static IntPtr GetGlobalEventQueue () {
        IntPtr result;
        INTERNAL_CALL_GetGlobalEventQueue ( out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetGlobalEventQueue (out IntPtr value);
}


}
