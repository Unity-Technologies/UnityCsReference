// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Scripting;
using UnityObject = UnityEngine.Object;

namespace UnityEditor.VersionControl
{
    public static partial class VersionControlManager
    {
        static VersionControlDescriptor[] s_Descriptors;

        public static VersionControlDescriptor[] versionControlDescriptors => s_Descriptors ?? (s_Descriptors = GetDescriptors());

        public static VersionControlObject activeVersionControlObject
        {
            get => (VersionControlObject)GetActiveObject();
            private set => SetActiveObject(value);
        }

        internal static bool isConnected
        {
            [RequiredByNativeCode]
            get => activeVersionControlObject?.isConnected ?? false;
        }

        static VersionControlManager()
        {
            if (Provider.enabled)
                return;

            var mode = VersionControlSettings.mode;
            if (mode == ExternalVersionControl.Disabled || mode == ExternalVersionControl.Generic || mode == ExternalVersionControl.AutoDetect)
                return;

            // This can be true even if !Provider.enabled. It happens when this method is invoked from VCProvider::UpdateSettings.
            if (mode == "Perforce" || mode == "PlasticSCM")
                return;

            if (!HasDescriptor(mode))
                SetVersionControl(ExternalVersionControl.Disabled);
        }

        static VersionControlDescriptor[] GetDescriptors()
        {
            var types = TypeCache.GetTypesWithAttribute<VersionControlAttribute>();
            var descriptorList = new List<VersionControlDescriptor>(types.Count);
            foreach (var type in types)
            {
                var attribute = (VersionControlAttribute)type.GetCustomAttributes(typeof(VersionControlAttribute), false).Single();
                var name = attribute.name;
                if (!typeof(VersionControlObject).IsAssignableFrom(type))
                {
                    Debug.LogWarning($"Version control object {type.FullName} named '{name}' will be ignored because it's not derived from {typeof(VersionControlObject).FullName}.");
                    continue;
                }
                var duplicate = descriptorList.FirstOrDefault(d => string.Equals(d.name, name, StringComparison.Ordinal));
                if (duplicate != null)
                {
                    Debug.LogWarning($"Version control name '{name}' is not unique. Version control object {type.FullName} will be ignored. Another version control object is {duplicate.type.FullName}.");
                    continue;
                }
                descriptorList.Add(new VersionControlDescriptor(name, attribute.displayName, type));
            }
            descriptorList.Sort((l, r) => string.Compare(l.name, r.name));
            return descriptorList.ToArray();
        }

        [RequiredByNativeCode]
        static bool HasDescriptor(string name)
        {
            return versionControlDescriptors.Any(d => string.Equals(d.name, name, StringComparison.Ordinal));
        }

        public static bool SetVersionControl(string name)
        {
            var oldName = VersionControlSettings.mode;
            VersionControlSettings.mode = name;
            if (VersionControlSettings.mode != name)
                return false;

            Provider.UpdateSettings();

            if (oldName != name)
            {
                if (name == ExternalVersionControl.Disabled || name == ExternalVersionControl.Generic)
                    WindowPending.CloseAllWindows();
            }

            return true;
        }

        [RequiredByNativeCode]
        static bool Activate(string name)
        {
            Deactivate();

            var descriptor = versionControlDescriptors.FirstOrDefault(d => string.Equals(d.name, name, StringComparison.Ordinal));
            if (descriptor == null)
                return false;

            var vco = (VersionControlObject)ScriptableObject.CreateInstance(descriptor.type);
            vco.hideFlags = HideFlags.HideAndDontSave;
            vco.name = name;
            activeVersionControlObject = vco;
            try
            {
                vco.OnActivate();
            }
            catch (Exception ex)
            {
                activeVersionControlObject = null;
                Debug.LogError($"Failed to activate version control system '{name}': {ex}");
                return false;
            }
            return true;
        }

        [RequiredByNativeCode]
        internal static void Deactivate()
        {
            var vco = activeVersionControlObject;
            if (vco == null)
                return;
            var name = vco.name;
            try
            {
                vco.OnDeactivate();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to deactivate version control system '{name}': {ex}");
            }
            activeVersionControlObject = null;
            UnityObject.DestroyImmediate(vco);
        }

        [RequiredByNativeCode]
        static void Refresh()
        {
            var vco = activeVersionControlObject;
            if (vco != null)
                vco.Refresh();
        }
    }
}
