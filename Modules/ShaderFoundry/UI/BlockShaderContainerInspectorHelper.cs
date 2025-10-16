// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor.Experimental;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderFoundry
{
    // A helper class in charge of rendering the inspector GUI for a BlockShaderContainer.
    internal static class BlockShaderContainerInspectorHelper
    {
        private static class USS
        {
            public const string kStyleSheetPath = "StyleSheets/ShaderFoundry/BlockShaderContainerEditor.uss";

            public static class Classes
            {
                public const string kInspectorSection = "inspector-section";
                public const string kErrorContainer = "error-container";
                public const string kErrorList = "error-list";
            }
        }

        // A helper class used to render a pretty list of import errors.
        private static class InspectorErrorList
        {
            public static readonly Texture2D ErrorIcon = EditorGUIUtility.Load("icons/console.erroricon.png") as Texture2D;
            public static readonly Texture2D WarningIcon = EditorGUIUtility.Load("icons/console.warnicon.png") as Texture2D;

            public static VisualElement CreateGUI(IEnumerable<BlockShaderErrors.Error> errors)
            {
                VisualElement errorList = new VisualElement();
                errorList.AddToClassList(USS.Classes.kErrorList);
                foreach (BlockShaderErrors.Error error in errors)
                {
                    VisualElement errorContainer = new VisualElement();
                    errorContainer.AddToClassList(USS.Classes.kErrorContainer);

                    // Add click event handling to the error
                    Manipulator errorClickManipulator = CreateDoubleClickManipulator(
                        () => { TryOpenBlockShader(error.FilePath, (int)error.Line); });
                    errorContainer.AddManipulator(errorClickManipulator);

                    // Add an icon
                    Texture2D iconTexture = error.IsWarning ? WarningIcon : ErrorIcon;
                    errorContainer.Add(new Image() { image = iconTexture });

                    // Add the error message
                    string message = $"{error.Message} (line {error.Line})";
                    errorContainer.Add(new Label(message));

                    errorList.Add(errorContainer);
                    break;
                }
                return errorList;
            }

            // Helper method for creating a callback which fires when an element is double-left-clicked
            private static Manipulator CreateDoubleClickManipulator(System.Action handler)
            {
                Clickable manipulator = new Clickable(handler);
                // Only respond to double left clicks
                manipulator.activators.Clear();
                manipulator.activators.Add(new ManipulatorActivationFilter()
                {
                    button = MouseButton.LeftMouse,
                    clickCount = 2,
                });
                return manipulator;
            }

            private static void TryOpenBlockShader(string assetPath, int line)
            {
                if (string.IsNullOrEmpty(assetPath))
                    return;

                BlockShaderContainer containerObject = AssetDatabase.LoadAssetAtPath<BlockShaderContainer>(assetPath);
                if (containerObject != null)
                    AssetDatabase.OpenAsset(containerObject, line);
            }
        }

        // Primary entry point for rendering the inspector
        public static VisualElement CreateGUI(BlockShaderContainer target)
        {
            if (target == null)
                return null;

            VisualElement inspector = new VisualElement();
            // For some reason this call fails if we request a StyleSheet, rather than a generic Object
            StyleSheet styleSheet = EditorResources.Load<Object>(USS.kStyleSheetPath, isRequired: false) as StyleSheet;
            if (styleSheet != null)
                inspector.styleSheets.Add(styleSheet);

            inspector.Add(CreateReferencesSection(target));
            inspector.Add(CreateErrorsSection(target));

            return inspector;
        }

        private static VisualElement CreateReferencesSection(BlockShaderContainer target)
        {
            BlockShaderContainer[] references = target.GetDependencies();
            if (references.Length == 0)
                return null;

            VisualElement referencesSection = new VisualElement();
            referencesSection.AddToClassList(USS.Classes.kInspectorSection);
            referencesSection.Add(new Label("Imported Assets"));

            foreach (BlockShaderContainer reference in references)
            {
                VisualElement referenceContainer = new VisualElement();

                // Display the asset as an object field
                ObjectField field = new ObjectField();
                field.value = reference;
                field.SetEnabled(false); // Make the field non-editable
                referenceContainer.Add(field);

                // If the dependency has errors, display them
                VisualElement errorList = InspectorErrorList.CreateGUI(reference.GetErrors());
                referenceContainer.Add(errorList);

                referencesSection.Add(referenceContainer);
            }
            return referencesSection;
        }

        private static VisualElement CreateErrorsSection(BlockShaderContainer target)
        {
            List<BlockShaderErrors.Error> targetErrors = new List<BlockShaderErrors.Error>(target.GetErrors());
            if (targetErrors.Count == 0)
                return null;

            VisualElement errorsSection = new VisualElement();
            errorsSection.AddToClassList(USS.Classes.kInspectorSection);
            errorsSection.Add(new Label("Errors"));
            VisualElement errorList = InspectorErrorList.CreateGUI(targetErrors);
            errorsSection.Add(errorList);
            return errorsSection;
        }
    }
}
