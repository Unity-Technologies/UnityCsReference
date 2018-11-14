// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class SettingsProviderAttribute : Attribute
    {
        [RequiredSignature]
        private static SettingsProvider signature()
        {
            return null;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class SettingsProviderGroupAttribute : Attribute
    {
        [RequiredSignature]
        private static SettingsProvider[] signature()
        {
            return null;
        }
    }
}
