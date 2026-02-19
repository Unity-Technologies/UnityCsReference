// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderApiReflection
{
    internal class ShaderIncludeEditorExtensions
    {
        public static VisualElement CreateInspectorGUI(Object target)
        {
            if (target is not ShaderInclude targetInclude)
                return null;

            // Load reflection data
            ShaderIncludeReflection reflectionData = targetInclude.Reflection;
            if (reflectionData == null)
                return null;

            VisualElement inspectorRoot = new VisualElement();

            // Display log messages
            foreach (LogMessage logMessage in reflectionData.LogMessages)
            {
                HelpBoxMessageType helpBoxType = (logMessage.MessageSeverity == LogMessage.Severity.Error) ?
                    HelpBoxMessageType.Error : HelpBoxMessageType.Warning;
                string text = $"Line {logMessage.Location.Line}: {logMessage.Text}";
                HelpBox helpBox = new HelpBox(text, helpBoxType);

                // Give the box a touch of padding on top
                helpBox.style.marginTop = new StyleLength(3);

                inspectorRoot.Add(helpBox);
            }

            // Display each reflected function's signature in a dropdown
            List<ReflectedFunction> reflectedFunctions =
                new List<ReflectedFunction>(reflectionData.ReflectedFunctions);
            if (reflectedFunctions.Count > 0)
            {
                Foldout signatureContainer = new Foldout()
                {
                    text = "Reflected Functions",
                };
                inspectorRoot.Add(signatureContainer);

                foreach (ReflectedFunction func in reflectedFunctions)
                    signatureContainer.contentContainer.Add(new Label(func.GetSignature()));
            }

            return inspectorRoot;
        }
    }
}
