using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    internal static class TextDelegateRegistration
    {
        [InitializeOnLoadMethod]
        static void Initialize()
        {
            PanelTextSettings.GetDefaultPanelTextSettings += PanelTextSettingsImporter.GetDefaultPanelTextSettings;
        }
    }
}
