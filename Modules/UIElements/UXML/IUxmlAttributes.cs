// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEngine.Experimental.UIElements
{
    public interface IUxmlAttributes
    {
        string GetPropertyString(string propertyName);
        long GetPropertyLong(string propertyName, long defaultValue);
        float GetPropertyFloat(string propertyName, float def);
        int GetPropertyInt(string propertyName, int def);
        bool GetPropertyBool(string propertyName, bool def);
        Color GetPropertyColor(string propertyName, Color def);
        T GetPropertyEnum<T>(string propertyName, T def);
    }
}
