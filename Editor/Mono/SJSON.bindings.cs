// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;
using System.Text;
using System;
using UnityEngine;
using UnityEngine.Bindings;

using JSONObject = System.Collections.IDictionary;

namespace UnityEditor
{
    [NativeHeader("Editor/Src/SJSON.h"),
     StaticAccessor("SJSON", StaticAccessorType.DoubleColon)]
    internal static class SJSON
    {
        public static extern string Encode([NotNull] JSONObject t);
        public static extern string EncodeObject(object o);
        [NativeThrows] public static extern JSONObject Decode([NotNull] byte[] sjson);
        [NativeThrows] public static extern object DecodeObject([NotNull] byte[] sjson);

        public static JSONObject Load(string path)
        {
            if (String.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));
            var readResult = File.ReadAllBytes(path);
            try
            {
                return Decode(readResult);
            }
            catch (UnityException ex)
            {
                throw new UnityException(ex.Message.Replace("(memory)", $"({path})"));
            }
        }

        public static JSONObject LoadString(string json)
        {
            if (String.IsNullOrEmpty(json))
                throw new ArgumentNullException(nameof(json));

            try
            {
                return Decode(Encoding.UTF8.GetBytes(json));
            }
            catch (UnityException ex)
            {
                throw new UnityException(ex.Message.Replace("(memory)", "(string)"), ex);
            }
        }

        public static byte[] GetBytes(JSONObject data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            return Encoding.UTF8.GetBytes(Encode(data));
        }

        public static bool Save(JSONObject h, string path)
        {
            var s = Encode(h);
            if (File.Exists(path))
            {
                var oldS = File.ReadAllText(path, Encoding.GetEncoding(0));
                if (s.Equals(oldS))
                    return false;
            }

            var bytes = Encoding.UTF8.GetBytes(s);
            File.WriteAllBytes(path, bytes);
            return true;
        }
    }
}
