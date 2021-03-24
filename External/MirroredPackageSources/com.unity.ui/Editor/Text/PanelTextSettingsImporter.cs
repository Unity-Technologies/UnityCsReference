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
