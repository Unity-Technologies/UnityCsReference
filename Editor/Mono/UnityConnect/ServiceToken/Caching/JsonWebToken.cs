// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text;

namespace UnityEditor.Connect
{
    readonly struct JsonWebToken
    {
        static readonly char[] k_JwtSeparator = { '.' };
        static readonly DateTime k_UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

        public DateTime exp { get; }

        public JsonWebToken(long exp)
        {
            this.exp = k_UnixEpoch.AddSeconds(exp);
        }

        public override string ToString()
        {
            return Json.Serialize(this);
        }

        public static JsonWebToken Decode(string token)
        {
            var parts = token.Split(k_JwtSeparator);
            if (parts.Length != 3)
            {
                throw new ArgumentException($"The authentication token is malformed or invalid. " +
                                              $"JWT has an invalid number of sections. Token: '{token}'");
            }

            var payload = parts[1];
            var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(payload));
            var deserialized = Json.Deserialize(payloadJson) as Dictionary<string,object>;
            return new JsonWebToken(Convert.ToInt64(deserialized.GetValueOrDefault(nameof(exp))));
        }

        static byte[] Base64UrlDecode(string input)
        {
            var output = input;
            output = output.Replace('-', '+'); // 62nd char of encoding
            output = output.Replace('_', '/'); // 63rd char of encoding

            var mod4 = input.Length % 4;
            if (mod4 > 0)
            {
                output += new string('=', 4 - mod4);
            }

            return Convert.FromBase64String(output);
        }
    }
}
