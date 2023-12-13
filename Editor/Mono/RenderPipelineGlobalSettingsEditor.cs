// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Reflection;
using UnityEditor.UIElements;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering
{
    [CustomEditor(typeof(RenderPipelineGlobalSettings), editorForChildClasses: true)]
    public class RenderPipelineGlobalSettingsEditor : Editor
    {
        static class Strings
        {
            public static readonly string k_WarningAttributeMessage = $"{nameof(RenderPipelineGlobalSettings)} should be used with {nameof(SupportedOnRenderPipelineAttribute)}";
            public static readonly string k_WarningEditionMessage = $"{nameof(RenderPipelineGlobalSettings)} should be edited from Project Settings > Graphics";
            public const string k_OpenGraphicsSettingsPanel = "Open Project Settings > Graphics ...";
        }

        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new();

            var attribute = target.GetType().GetCustomAttribute<SupportedOnRenderPipelineAttribute>();
            if (attribute == null)
                root.Add(new HelpBox(Strings.k_WarningAttributeMessage, HelpBoxMessageType.Warning));

            root.Add(new HelpBox(Strings.k_WarningEditionMessage, HelpBoxMessageType.Warning));
            var button = new Button() { text = Strings.k_OpenGraphicsSettingsPanel };
            button.clicked += () => SettingsService.OpenProjectSettings(GraphicsSettingsProvider.s_GraphicsSettingsProviderPath);
            root.Add(button);

            if (Unsupported.IsDeveloperMode())
            {
                var property = serializedObject.FindProperty("m_Settings");
                UnityEngine.Debug.Assert(property != null);
                root.Add(new PropertyField(property));
            }

            return root;
        }
    }
}
