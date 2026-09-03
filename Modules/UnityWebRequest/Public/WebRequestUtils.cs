// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine.Scripting;
using UnityEngine.Internal;
using UnityEngine.Bindings;


namespace UnityEngine
{
    ///<summary>Helper class to generate form data to post to web servers using the <see cref="UnityEngine.Networking.UnityWebRequest" /> or <see cref="T:UnityEngine.WWW" /> classes.</summary>
    ///<remarks>
    ///  <para>Here is a sample script that retrieves the high scores stored
    ///in a table in an SQL database.</para>
    ///  <para>Here is a sample Perl script that processes the high scores stored
    ///in a table in an SQL database.</para>
    ///</remarks>
    ///<example>
    ///  <code><![CDATA[
    ///using UnityEngine;
    ///using UnityEngine.Networking;
    ///using System.Collections;
    ///
    ///public class WWWFormImage : MonoBehaviour
    ///{
    ///
    ///    public string screenShotURL= "https://www.my-server.com/cgi-bin/screenshot.pl";
    ///
    ///    // Use this for initialization
    ///    void Start()
    ///    {
    ///        StartCoroutine(UploadPNG());
    ///    }
    ///
    ///    IEnumerator UploadPNG()
    ///    {
    ///        // We should only read the screen after all rendering is complete
    ///        yield return new WaitForEndOfFrame();
    ///
    ///        // Create a texture the size of the screen, RGB24 format
    ///        int width = Screen.width;
    ///        int height = Screen.height;
    ///        var tex = new Texture2D( width, height, TextureFormat.RGB24, false );
    ///
    ///        // Read screen contents into the texture
    ///        tex.ReadPixels( new Rect(0, 0, width, height), 0, 0 );
    ///        tex.Apply();
    ///
    ///        // Encode texture into PNG
    ///        byte[] bytes = tex.EncodeToPNG();
    ///        Destroy( tex );
    ///
    ///        // Create a Web Form
    ///        WWWForm form = new WWWForm();
    ///        form.AddField("frameCount", Time.frameCount.ToString());
    ///        form.AddBinaryData("fileUpload", bytes, "screenShot.png", "image/png");
    ///
    ///        // Upload to a cgi script
    ///        using (var w = UnityWebRequest.Post(screenShotURL, form))
    ///        {
    ///            yield return w.SendWebRequest();
    ///            if (w.result != UnityWebRequest.Result.Success) {
    ///                print(w.error);
    ///            }
    ///            else {
    ///                print("Finished Uploading Screenshot");
    ///            }
    ///        }
    ///    }
    ///}
    ///]]></code>
    ///</example>
    ///<example>
    ///  <code><![CDATA[
    ///using UnityEngine;
    ///using UnityEngine.Networking;
    ///using System.Collections;
    ///
    ///public class WWWFormScore : MonoBehaviour
    ///{
    ///    string highscore_url = "https://www.my-site.com/highscores.pl";
    ///    string playName = "Player 1";
    ///    int score = -1;
    ///
    ///    // Use this for initialization
    ///    IEnumerator Start()
    ///    {
    ///        // Create a form object for sending high score data to the server
    ///        WWWForm form = new WWWForm();
    ///
    ///        // Assuming the perl script manages high scores for different games
    ///        form.AddField( "game", "MyGameName" );
    ///
    ///        // The name of the player submitting the scores
    ///        form.AddField( "playerName", playName );
    ///
    ///        // The score
    ///        form.AddField( "score", score );
    ///
    ///        // Create a download object
    ///        var download = UnityWebRequest.Post(highscore_url, form);
    ///
    ///        // Wait until the download is done
    ///        yield return download.SendWebRequest();
    ///
    ///        if (download.result != UnityWebRequest.Result.Success)
    ///        {
    ///            print( "Error downloading: " + download.error );
    ///        }
    ///        else
    ///        {
    ///            // show the highscores
    ///            Debug.Log(download.downloadHandler.text);
    ///        }
    ///    }
    ///}
    ///]]></code>
    ///</example>
    ///<example nocheck="true">
    ///  <code><![CDATA[#!/usr/bin/perl
    ///# The SQL database needs to have a table called highscores
    ///# that looks something like this:
    ///#
    ///#   CREATE TABLE highscores (
    ///#     game varchar(255) NOT NULL,
    ///#     player varchar(255) NOT NULL,
    ///#     score integer NOT NULL
    ///#   );
    ///#
    ///use strict;
    ///use CGI;
    ///use DBI;
    ///
    ///# Read form data etc.
    ///my $cgi = new CGI;
    ///
    ///# The results from the high score script will be in plain text
    ///print $cgi->header("text/plain");
    ///
    ///my $game = $cgi->param('game');
    ///my $playerName = $cgi->param('playerName');
    ///my $score = $cgi->param('score');
    ///
    ///exit 0 unless $game; # This parameter is required
    ///
    ///# Connect to a database
    ///my $dbh = DBI->connect( 'DBI:mysql:databasename', 'username', 'password' )
    ///    || die "Could not connect to database: $DBI::errstr";
    ///
    ///# Insert the player score if there are any
    ///if( $playerName && $score) {
    ///    $dbh->do( "insert into highscores (game, player, score) values(?,?,?)",
    ///        undef, $game, $playerName, $score );
    ///}
    ///
    ///# Fetch the high scores
    ///my $sth = $dbh->prepare(
    ///    'SELECT player, score FROM highscores WHERE game=? ORDER BY score desc LIMIT 10' );
    ///$sth->execute($game);
    ///while (my $r = $sth->fetchrow_arrayref) {
    ///    print join(':',@$r),"\n"
    ///}]]></code>
    ///</example>
    public class WWWForm
    {
        private List<byte[]> formData; // <byte[]>
        private List<string> fieldNames; // <string>
        private List<string> fileNames; // <string>
        private List<string> types; // <string>
        private byte[] boundary;
        private bool containsFiles = false;

        internal static System.Text.Encoding DefaultEncoding
        {
            get
            {
                return System.Text.Encoding.ASCII;
            }
        }

        ///<summary>Creates an empty WWWForm object.</summary>
        ///<remarks>Use the <see cref="AddField" /> and <see cref="AddBinaryData" /> methods to insert data into the form.</remarks>
        ///<seealso cref="UnityEngine.Networking.UnityWebRequest" />
        ///<seealso cref="T:UnityEngine.WWW" />
        public WWWForm()
        {
            formData = new List<byte[]>();
            fieldNames = new List<string>();
            fileNames = new List<string>();
            types = new List<string>();

            // Generate a random boundary
            boundary = new byte[40];
            for (int i = 0; i < 40; i++)
            {
                int randomChar = Random.Range(48, 110);
                if (randomChar > 57) // skip unprintable chars between 57 and 64 (inclusive)
                    randomChar += 7;
                if (randomChar > 90) // and 91 and 96 (inclusive)
                    randomChar += 6;
                boundary[i] = (byte)randomChar;
            }
        }

        ///<summary>Add a simple field to the form.</summary>
        ///<remarks>Adds field <c>fieldName</c> with a given string value.</remarks>
        public void AddField(string fieldName, string value)
        {
            AddField(fieldName, value, Encoding.UTF8);
        }

        ///<summary>Add a simple field to the form.</summary>
        ///<remarks>Adds field <c>fieldName</c> with a given string value.</remarks>
        public void AddField(string fieldName, string value, Encoding e)
        {
            fieldNames.Add(fieldName);
            fileNames.Add(null);
            formData.Add(e.GetBytes(value));
            types.Add("text/plain; charset=\"" + e.WebName + "\"");
        }

        ///<summary>Adds a simple field to the form.</summary>
        ///<remarks>Adds field <c>fieldName</c> with a given integer value. A conveinience for calling
        ///AddField(fieldName, i.ToString).</remarks>
        public void AddField(string fieldName, int i)
        {
            AddField(fieldName, i.ToString());
        }

        // Add binary data to the form.
        [ExcludeFromDocs]
        public void AddBinaryData(string fieldName, byte[] contents)
        {
            AddBinaryData(fieldName, contents, null, null);
        }

        // Add binary data to the form.
        [ExcludeFromDocs]
        public void AddBinaryData(string fieldName, byte[] contents, string fileName)
        {
            AddBinaryData(fieldName, contents, fileName, null);
        }

        ///<summary>Add binary data to the form.</summary>
        ///<remarks>Use this function to upload files and images to a web server application.
        ///Note that the data is read from the contents of byte array and not from a file.
        ///The fileName parameter is for telling the server what filename to use when saving the uploaded file.
        ///
        ///If <c>mimeType</c> is not given and first 8 bytes of the data match PNG format header, then the
        ///data is sent with "<c>image/png</c>" mimetype. Otherwise it is sent with "<c>application/octet-stream</c>"
        ///mimetype.</remarks>
        public void AddBinaryData(string fieldName, byte[] contents, [DefaultValue("null")] string fileName, [DefaultValue("null")] string mimeType)
        {
            containsFiles = true;

            // We handle png files automatically as we suspect people will be uploading png files a lot due to the new
            // screen shot feature. If we want to add support for detecting other file types, we will need to do it in a more extensible way.
            bool isPng = contents.Length > 8 && contents[0] == 0x89 && contents[1] == 0x50 && contents[2] == 0x4e &&
                contents[3] == 0x47
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

        ///<summary>(RO) Returns the correct request headers for posting the form using the <see cref="T:UnityEngine.WWW" /> class.</summary>
        ///<remarks>This field only contains one header, /"Content-Type"/,
        ///which is set to the correct mime type for the form: "<c>application/x-www-form-urlencoded</c>" for normal
        ///forms and "<c>multipart/form-data</c>" for forms containing data added using <see cref="AddBinaryData" />.</remarks>
        ///<example>
        ///  <code><![CDATA[using System.Collections;
        ///using System.Collections.Generic;
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour {
        ///
        ///    IEnumerator Start () {
        ///        Dictionary<string, string> headers = new Dictionary<string,string>();
        ///        headers.Add("header-name", "header content");
        ///        WWW www = new WWW("https://example.com", null, headers);
        ///        yield return www;
        ///        Debug.Log (www.text);
        ///    }
        ///
        ///}
        ///]]></code>
        ///</example>
        public Dictionary<string, string> headers
        {
            get
            {
                Dictionary<string, string> retval = new Dictionary<string, string>();
                if (containsFiles)
                    retval["Content-Type"] = "multipart/form-data; boundary=\"" +
                        System.Text.Encoding.UTF8.GetString(boundary, 0, boundary.Length) + "\"";
                else
                    retval["Content-Type"] = "application/x-www-form-urlencoded";
                return retval;
            }
        }

        private static readonly byte[] dDash = DefaultEncoding.GetBytes("--");
        private static readonly byte[] crlf = DefaultEncoding.GetBytes("\r\n");
        private static readonly byte[] contentTypeHeader = DefaultEncoding.GetBytes("Content-Type: ");
        private static readonly byte[] dispositionHeader = DefaultEncoding.GetBytes("Content-disposition: form-data; name=\"");
        private static readonly byte[] endQuote = DefaultEncoding.GetBytes("\"");
        private static readonly byte[] fileNameField = DefaultEncoding.GetBytes("; filename=\"");
        private static readonly byte[] ampersand = DefaultEncoding.GetBytes("&");
        private static readonly byte[] equal = DefaultEncoding.GetBytes("=");

        ///<summary>(RO) The raw data to pass as the POST request body when sending the form.</summary>
        ///<remarks>Usually, you just pass the WWWForm object directly to the <see cref="T:UnityEngine.WWW" /> constructor, but you will
        ///need this variable if you want to change the request headers sent to the web server.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///using System.Collections.Generic;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    IEnumerator Start()
        ///    {
        ///        WWWForm form = new WWWForm();
        ///        form.AddField( "name", "value" );
        ///        Dictionary<string, string> headers = form.headers;
        ///        byte[] rawData = form.data;
        ///        string url = "www.myurl.com";
        ///
        ///        // Add a custom header to the request.
        ///        // In this case a basic authentication to access a password protected resource.
        ///        headers["Authorization"] = "Basic " + System.Convert.ToBase64String(
        ///            System.Text.Encoding.ASCII.GetBytes("username:password"));
        ///
        ///        // Post a request to an URL with our custom headers
        ///        using (WWW www = new WWW(url, rawData, headers))
        ///        {
        ///            yield return www;
        ///            //.. process results from WWW request here...
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="headers" />
        public byte[] data
        {
            get
            {
                using (MemoryStream memStream = new MemoryStream(1024))
                {
                    if (containsFiles)
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
                            // Headers must be 7 bit clean, so encode as per rfc1522 using quoted-printable if needed.
                            string encodedFieldName = (string)fieldNames[i];
                            if (!WWWTranscoder.SevenBitClean(encodedFieldName, System.Text.Encoding.UTF8) ||
                                encodedFieldName.IndexOf("=?") > -1)
                            {
                                encodedFieldName = "=?" + headerName + "?Q?" +
                                    WWWTranscoder.QPEncode(encodedFieldName, System.Text.Encoding.UTF8) + "?=";
                            }
                            byte[] name = System.Text.Encoding.UTF8.GetBytes(encodedFieldName);
                            memStream.Write(name, 0, (int)name.Length);
                            memStream.Write(endQuote, 0, (int)endQuote.Length);

                            if (fileNames[i] != null)
                            {
                                // Headers must be 7 bit clean, so encode as per rfc1522 using quoted-printable if needed.
                                string encodedFileName = (string)fileNames[i];
                                if (!WWWTranscoder.SevenBitClean(encodedFileName, System.Text.Encoding.UTF8) ||
                                    encodedFileName.IndexOf("=?") > -1)
                                {
                                    encodedFileName = "=?" + headerName + "?Q?" +
                                        WWWTranscoder.QPEncode(encodedFileName, System.Text.Encoding.UTF8) + "?=";
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
                    }
                    else
                    {
                        for (int i = 0; i < formData.Count; i++)
                        {
                            byte[] name = WWWTranscoder.DataEncode(System.Text.Encoding.UTF8.GetBytes((string)fieldNames[i]));
                            byte[] formBytes = (byte[])formData[i];
                            byte[] value = WWWTranscoder.DataEncode(formBytes);

                            if (i > 0) memStream.Write(ampersand, 0, (int)ampersand.Length);
                            memStream.Write(name, 0, (int)name.Length);
                            memStream.Write(equal, 0, (int)equal.Length);
                            memStream.Write(value, 0, (int)value.Length);
                        }
                    }

                    return memStream.ToArray();
                }
            }
        }
    }


    [VisibleToOtherModules("UnityEngine.UnityWebRequestWWWModule")]
    internal class WWWTranscoder
    {
        private static readonly byte[] ucHexChars = WWWForm.DefaultEncoding.GetBytes("0123456789ABCDEF");
        private static readonly byte[] lcHexChars = WWWForm.DefaultEncoding.GetBytes("0123456789abcdef");
        private static readonly byte urlEscapeChar = (byte)'%';
        private static readonly byte[] urlSpace = new byte[] { (byte)'+' };
        private static readonly byte[] dataSpace = WWWForm.DefaultEncoding.GetBytes("%20");
        private static readonly byte[] urlForbidden = WWWForm.DefaultEncoding.GetBytes("@&;:<>=?\"'/\\!#%+$,{}|^[]`");
        private static readonly byte[] wwwFormForbidden = WWWForm.DefaultEncoding.GetBytes("@;:<>?\"'/\\!#%+$,{}|^[]`");
        private static readonly byte qpEscapeChar = (byte)'=';
        private static readonly byte[] qpSpace = new byte[] {  (byte)'_' };
        private static readonly byte[] qpForbidden = WWWForm.DefaultEncoding.GetBytes("&;=?\"'%+_");

        private static byte Hex2Byte(byte[] b, int offset)
        {
            byte result = (byte)0;

            for (int i = offset; i < offset + 2; i++)
            {
                result *= 16;
                int d = b[i];

                if (d >= 48 && d <= 57) // 0 - 9
                    d -= 48;
                else if (d >= 65 && d <= 75) // A -F
                    d -= 55;
                else if (d >= 97 && d <= 102) // a - f
                    d -= 87;
                if (d > 15)
                {
                    return 63; // ?
                }

                result += (byte)d;
            }

            return result;
        }

        private static void Byte2Hex(byte b, byte[] hexChars, out byte byte0, out byte byte1)
        {
            byte0 = hexChars[b >> 4];
            byte1 = hexChars[b & 0xf];
        }

        public static string URLEncode(string toEncode)
        {
            return URLEncode(toEncode, Encoding.UTF8);
        }

        public static string URLEncode(string toEncode, Encoding e)
        {
            byte[] data = Encode(e.GetBytes(toEncode), urlEscapeChar, urlSpace, urlForbidden, false);
            return WWWForm.DefaultEncoding.GetString(data, 0, data.Length);
        }

        public static byte[] URLEncode(byte[] toEncode)
        {
            return Encode(toEncode, urlEscapeChar, urlSpace, urlForbidden, false);
        }

        public static string DataEncode(string toEncode)
        {
            return DataEncode(toEncode, Encoding.UTF8);
        }

        public static string DataEncode(string toEncode, Encoding e)
        {
            byte[] data = Encode(e.GetBytes(toEncode), urlEscapeChar, dataSpace, urlForbidden, false);
            return WWWForm.DefaultEncoding.GetString(data, 0, data.Length);
        }

        public static byte[] DataEncode(byte[] toEncode)
        {
            return Encode(toEncode, urlEscapeChar, dataSpace, urlForbidden, false);
        }

        public static string WwwFormEncode(string toEncode, Encoding e)
        {
            byte[] data = Encode(e.GetBytes(toEncode), urlEscapeChar, dataSpace, wwwFormForbidden, false);
            return WWWForm.DefaultEncoding.GetString(data, 0, data.Length);
        }

        public static string QPEncode(string toEncode)
        {
            return QPEncode(toEncode, Encoding.UTF8);
        }

        public static string QPEncode(string toEncode, Encoding e)
        {
            byte[] data = Encode(e.GetBytes(toEncode), qpEscapeChar, qpSpace, qpForbidden, true);
            return WWWForm.DefaultEncoding.GetString(data, 0, data.Length);
        }

        public static byte[] QPEncode(byte[] toEncode)
        {
            return Encode(toEncode, qpEscapeChar, qpSpace, qpForbidden, true);
        }

        public static byte[] Encode(byte[] input, byte escapeChar, byte[] space, byte[] forbidden, bool uppercase)
        {
            using (MemoryStream memStream = new MemoryStream(input.Length * 2))
            {
                // encode
                for (int i = 0; i < input.Length; i++)
                {
                    if (input[i] == 32)
                    {
                        memStream.Write(space, 0, space.Length);
                    }
                    else if (input[i] < 32 || input[i] > 126 || ByteArrayContains(forbidden, input[i]))
                    {
                        memStream.WriteByte(escapeChar);
                        byte byte0, byte1;
                        Byte2Hex(input[i], uppercase ? ucHexChars : lcHexChars, out byte0, out byte1);
                        memStream.WriteByte(byte0);
                        memStream.WriteByte(byte1);
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

        public static string URLDecode(string toEncode)
        {
            return URLDecode(toEncode, Encoding.UTF8);
        }

        public static string URLDecode(string toEncode, Encoding e)
        {
            byte[] data = Decode(WWWForm.DefaultEncoding.GetBytes(toEncode), urlEscapeChar, urlSpace);
            return e.GetString(data, 0, data.Length);
        }

        public static byte[] URLDecode(byte[] toEncode)
        {
            return Decode(toEncode, urlEscapeChar, urlSpace);
        }

        public static string DataDecode(string toDecode)
        {
            return DataDecode(toDecode, Encoding.UTF8);
        }

        public static string DataDecode(string toDecode, Encoding e)
        {
            byte[] data = Decode(WWWForm.DefaultEncoding.GetBytes(toDecode), urlEscapeChar, dataSpace);
            return e.GetString(data, 0, data.Length);
        }

        public static byte[] DataDecode(byte[] toDecode)
        {
            return Decode(toDecode, urlEscapeChar, dataSpace);
        }

        public static string QPDecode(string toEncode)
        {
            return QPDecode(toEncode, Encoding.UTF8);
        }

        public static string QPDecode(string toEncode, Encoding e)
        {
            byte[] data = Decode(WWWForm.DefaultEncoding.GetBytes(toEncode), qpEscapeChar, qpSpace);
            return e.GetString(data, 0, data.Length);
        }

        public static byte[] QPDecode(byte[] toEncode)
        {
            return Decode(toEncode, qpEscapeChar, qpSpace);
        }

        private static bool ByteSubArrayEquals(byte[] array, int index, byte[] comperand)
        {
            if (array.Length - index < comperand.Length)
                return false;
            for (int i = 0; i < comperand.Length; ++i)
                if (array[index + i] != comperand[i])
                    return false;
            return true;
        }

        public static byte[] Decode(byte[] input, byte escapeChar, byte[] space)
        {
            using (MemoryStream memStream = new MemoryStream(input.Length))
            {
                // decode
                for (int i = 0; i < input.Length; i++)
                {
                    if (ByteSubArrayEquals(input, i, space))
                    {
                        i += space.Length - 1;
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

        public static bool SevenBitClean(string s)
        {
            return SevenBitClean(s, Encoding.UTF8);
        }

        public static bool SevenBitClean(string s, Encoding e)
        {
            unsafe
            {
                if (string.IsNullOrEmpty(s))
                    return true;
                int capacity = s.Length * 2;
                byte* bytes = stackalloc byte[capacity];
                int length;
                fixed(char* chars = s)
                {
                    length = e.GetBytes(chars, s.Length, bytes, capacity);
                }
                return SevenBitClean(bytes, length);
            }
        }

        public static unsafe bool SevenBitClean(byte* input, int inputLength)
        {
            for (int i = 0; i < inputLength; i++)
            {
                if (input[i] < 32 || input[i] > 126)
                    return false;
            }

            return true;
        }
    }
}

namespace UnityEngineInternal
{
    static class WebRequestUtils
    {
        private static readonly Regex domainRegex = new Regex("^\\s*\\w+(?:\\.\\w+)+(\\/.*)?$");

        [RequiredByNativeCode]
        internal static string RedirectTo(string baseUri, string redirectUri)
        {
            Uri redirectURI;
            // On UNIX systems URI starting with / is misidentified as absolute path and is considered absolute
            // while it is actually a relative URI. Enforce that.
            if (redirectUri[0] == '/')
                redirectURI = new Uri(redirectUri, UriKind.Relative);
            else
                redirectURI = new Uri(redirectUri, UriKind.RelativeOrAbsolute);
            if (redirectURI.IsAbsoluteUri)
                return redirectURI.AbsoluteUri;

            var baseURI = new Uri(baseUri, UriKind.Absolute);
            var finalUri = new Uri(baseURI, redirectURI);
            return finalUri.AbsoluteUri;
        }

        internal static string MakeInitialUrl(string targetUrl, string localUrl)
        {
            if (string.IsNullOrEmpty(targetUrl))
                return "";

            bool prependProtocol = false;
            var localUri = new System.Uri(localUrl);
            Uri targetUri = null;

            if (targetUrl[0] == '/')
            {
                // Prepend scheme and (if needed) host
                targetUri = new Uri(localUri, targetUrl);
                prependProtocol = true;
            }

            if (targetUri == null && domainRegex.IsMatch(targetUrl))
            {
                targetUrl = localUri.Scheme + "://" + targetUrl;
                prependProtocol = true;
            }

            FormatException ex = null;
            try
            {
                // If URL starts with dot, it is relative and this would throw, skip to combining
                if (targetUri == null && targetUrl[0] != '.')
                    targetUri = new System.Uri(targetUrl);
            }
            catch (FormatException e1)
            {
                // Technically, this should be UriFormatException but MSDN says WSA/PCL doesn't support
                // UriFormatException, and recommends FormatException instead
                // See: https://msdn.microsoft.com/en-us/library/system.uriformatexception%28v=vs.110%29.aspx
                ex = e1;
            }

            if (targetUri == null)
                try
                {
                    targetUri = new System.Uri(localUri, targetUrl);
                    prependProtocol = true;
                }
                catch (FormatException)
                {
                    throw ex;
                }

            return MakeUriString(targetUri, targetUrl, prependProtocol);
        }

        internal static string MakeUriString(Uri targetUri, string targetUrl, bool prependProtocol)
        {
            // for file://protocol pass in unescaped string so we can pass it to VFS
            if (targetUri.IsFile)
            {
                if (!targetUri.IsLoopback)
                    return targetUri.OriginalString;
                string path = targetUri.AbsolutePath;
                var original = targetUri.OriginalString;
                if (path.Contains("%"))
                {
                    if (path.Contains('+'))
                    {
                        // if URI has both % and +, we don't know if + mean itself or is an escape for space
                        // what we want is for correct absolute path passed to Uri constructor to work
                        // otherwise it's users responsibility to ensure proper escaping                       
                        if (!original.StartsWith("file:"))
                        {
                            return "file://" + original;
                        }
                    }
                    path = URLDecode(path);
                }
                if (path.Length > 0 && path[0] != '/')
                    path = '/' + path;

                return "file://" + path;
            }

            // Special handling for URIs like jar:file (Android), blob:http (WebGL and similar
            // Uri.AbsoluteUri class in those cases results in jar:file/path, which is incorrect because of only one slash
            // Uri.Scheme also returns scheme part before the colon (jar, blob)
            // so if we didn't prepend the scheme and scheme has colon it it, construct the URI from it's parts
            var scheme = targetUri.Scheme;
            if (!prependProtocol && (targetUrl.Length >= scheme.Length + 2) && targetUrl[scheme.Length + 1] != '/')
            {
                StringBuilder sb = new StringBuilder(scheme, targetUrl.Length);
                sb.Append(':');
                // for these spec URIs path also has the part of URI to right of colon
                // jar:file URIs should be treated like file URIs (unescaped and stripped of query&fragment)
                if (scheme == "jar")
                {
                    string path = targetUri.AbsolutePath;
                    if (path.Contains("%"))
                        path = URLDecode(path);

                    // common error when using Uri class and converting to string: URI will be jar:file:/path instead of jar:file:///path
                    if (path.StartsWith("file:/") && path.Length > 6 && path[6] != '/')
                    {
                        sb.Append("file://");
                        sb.Append(path.Substring(5));
                    }
                    else
                        sb.Append(path);
                    return sb.ToString();
                }
                sb.Append(targetUri.PathAndQuery);
                sb.Append(targetUri.Fragment);
                return sb.ToString();
            }

            // if URL contains '%', assume it is properly escaped, otherwise '%2f' gets unescaped as '/' (which may not be correct)
            // otherwise escape it, i.e. replaces spaces by '%20'
            if (targetUrl.Contains("%"))
                return targetUri.OriginalString;

            return targetUri.AbsoluteUri;
        }

        static string URLDecode(string encoded)
        {
            var urlBytes = Encoding.UTF8.GetBytes(encoded);
            var decodedBytes = UnityEngine.WWWTranscoder.URLDecode(urlBytes);
            return Encoding.UTF8.GetString(decodedBytes);
        }
    }
}
