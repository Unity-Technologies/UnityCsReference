// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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
    public class PanelTextSettings : TextSettings
    {
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
