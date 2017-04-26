// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

using UnityEngine;
using UnityEditor;

namespace UnityEditor.Modules
{
    internal abstract class DefaultPlayerSettingsEditorExtension : ISettingEditorExtension
    {
        public virtual void OnEnable(PlayerSettingsEditor settingsEditor) {}

        public virtual bool HasPublishSection()
        {
            return true;
        }

        public virtual void PublishSectionGUI(float h, float midWidth, float maxWidth) {}

        public virtual bool HasIdentificationGUI()
        {
            return false;
        }

        public virtual void IdentificationSectionGUI() {}

        public virtual void ConfigurationSectionGUI() {}

        public virtual bool SupportsOrientation()
        {
            return false;
        }

        public virtual bool CanShowUnitySplashScreen()
        {
            return false;
        }

        public virtual void SplashSectionGUI() {}

        public virtual bool UsesStandardIcons()
        {
            return true;
        }

        public virtual void IconSectionGUI() {}

        public virtual bool HasResolutionSection()
        {
            return false;
        }

        public virtual bool SupportsStaticBatching()
        {
            return true;
        }

        public virtual bool SupportsDynamicBatching()
        {
            return true;
        }

        public virtual void ResolutionSectionGUI(float h, float midWidth, float maxWidth) {}

        public virtual bool HasBundleIdentifier()
        {
            return true;
        }

        public virtual bool SupportsHighDynamicRangeDisplays() { return false; }
        public virtual bool SupportsGfxJobModes() { return false; }
    }
}
