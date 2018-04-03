// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Experimental.UIElements
{
    public interface IUxmlAttributes
    {
        // TODO 2018.3 [Obsolete("Use GetPropertyString(string propertyName, string defaultValue)")]
        string GetPropertyString(string propertyName);
        string GetPropertyString(string propertyName, string defaultValue);
        float GetPropertyFloat(string propertyName, float defaultValue);
        double GetPropertyDouble(string propertyName, double defaultValue);
        int GetPropertyInt(string propertyName, int defaultValue);
        long GetPropertyLong(string propertyName, long defaultValue);
        bool GetPropertyBool(string propertyName, bool defaultValue);
        Color GetPropertyColor(string propertyName, Color defaultValue);
        T GetPropertyEnum<T>(string propertyName, T defaultValue);
    }
}
