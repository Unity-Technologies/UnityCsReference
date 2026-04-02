// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.Multiplayer.Editor
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    internal class ProjectSettingsSectionAttribute : Attribute
    {
        public string Label = null;
        public string SettingsPath = ProjectSettingsProvider.k_SettingsGroupPath;

        public ProjectSettingsSectionAttribute() { }
    }
}
