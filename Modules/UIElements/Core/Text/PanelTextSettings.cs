// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Scripting.LifecycleManagement;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.TextCore.Text;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Represents text rendering settings for a specific UI panel.
    /// <seealso cref="PanelSettings.textSettings"/>
    /// </summary>
    [HelpURL("UIE-text-setting-asset")]
    public partial class PanelTextSettings : TextSettings
    {
        [AutoStaticsCleanupOnCodeReload]
        // Lazy default instance: InitializeDefaultPanelTextSettingsIfNull() recreates it on the next
        // access after cleanup nulls it, so a constructor that forces it leaves nothing stale behind.
        [IgnoreForUAL0015("Lazy default instance recreated on demand by InitializeDefaultPanelTextSettingsIfNull()")]
        private static PanelTextSettings s_DefaultPanelTextSettings;

        internal static PanelTextSettings defaultPanelTextSettings
        {
            get
            {
                InitializeDefaultPanelTextSettingsIfNull();
                return s_DefaultPanelTextSettings;
            }
        }

        internal static void InitializeDefaultPanelTextSettingsIfNull()
        {
            if (s_DefaultPanelTextSettings == null)
            {
                s_DefaultPanelTextSettings = ScriptableObject.CreateInstance<PanelTextSettings>();
            }
        }
    }
}
