// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    internal class PanelTextSettingsImporter
    {
        internal static string k_DefaultPanelTextSettingsPath =
            "UIPackageResources/Text/Default Panel Text Settings.asset";


        PanelTextSettingsImporter() {}

        internal static PanelTextSettings GetDefaultPanelTextSettings()
        {
            return EditorGUIUtility.Load(k_DefaultPanelTextSettingsPath) as PanelTextSettings;
        }
    }
}
