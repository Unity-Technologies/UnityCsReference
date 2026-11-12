// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.Marketplace;

static class MarketplaceJson
{
    public static Dictionary<string, object> Parse(string json)
    {
        if (string.IsNullOrEmpty(json))
            return null;

        try
        {
            return Json.Deserialize(json) as Dictionary<string, object>;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static Dictionary<string, object> ReadObject(Dictionary<string, object> fields, string key)
    {
        if (fields == null)
            return null;

        return fields.TryGetValue(key, out var value) ? value as Dictionary<string, object> : null;
    }

    public static string ReadString(Dictionary<string, object> fields, string key)
    {
        if (fields == null)
            return null;

        return fields.TryGetValue(key, out var value) ? value as string : null;
    }

    public static int ReadInt(Dictionary<string, object> fields, string key, int fallback)
    {
        if (fields == null || !fields.TryGetValue(key, out var value) || value == null)
            return fallback;

        // MiniJson yields long for integral values and double otherwise.
        try
        {
            return Convert.ToInt32(value);
        }
        catch (Exception)
        {
            return fallback;
        }
    }

    public static bool? ReadNullableBool(Dictionary<string, object> fields, string key)
    {
        if (fields == null || !fields.TryGetValue(key, out var value))
            return null;

        return value as bool?;
    }
}
