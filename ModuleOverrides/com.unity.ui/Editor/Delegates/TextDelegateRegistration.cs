// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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
