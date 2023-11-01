// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditorInternal;

namespace UnityEditor.Connect
{
    static class NetworkingUtils
    {
        internal static string GetStringFromRawJsonDictionaryString(string rawJson, string key)
        {
            var jsonParser = new JSONParser(rawJson);
            var json = jsonParser.Parse();

            return GetStringFromJsonDictionary(json.AsDict(), key);
        }

        internal static string GetStringFromJsonDictionary(Dictionary<string, JSONValue> container, string key)
        {
            var jsonValue = new JSONValue();
            container?.TryGetValue(key, out jsonValue);
            return jsonValue.AsString();
        }

        internal static Dictionary<string, JSONValue> GetJsonDictionaryWithinRawJsonDictionaryString(string rawJson, string key)
        {
            var jsonParser = new JSONParser(rawJson);
            var json = jsonParser.Parse();

            return GetJsonDictionaryWithinJsonDictionary(json.AsDict(), key);
        }


        static Dictionary<string, JSONValue> GetJsonDictionaryWithinJsonDictionary(Dictionary<string, JSONValue> container, string key)
        {
            var jsonValue = new JSONValue();
            container?.TryGetValue(key, out jsonValue);
            return jsonValue.AsDict();
        }
    }
}
