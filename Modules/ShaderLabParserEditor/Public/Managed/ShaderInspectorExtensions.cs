// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Collections.ObjectModel;
using Unity.CodeEditor;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderApiReflection
{
    /// <summary>
    /// Contains helper functions for rendering shader interface-specific UI elements in the
    /// shader inspector.
    /// </summary>
    internal static class ShaderInspectorExtensions
    {
        internal static class USS
        {
            public const string kStyleSheetPath = "StyleSheets/ShaderLabParserEditor/ShaderInspector.uss";

            public static class Classes
            {
                public const string kAccentText = "accent-text";
                public const string kBoldFoldout = "bold-foldout";
                public const string kCodeBlock = "code-block";
                public const string kOverridableFunction = "overridable-function";
                public const string kVirtualFunctionMetaRow = "virtual-function-meta-row";
            }

            public static class Names
            {
                public const string kHeader = "header";
                public const string kHeaderBase = "header-base";
                public const string kNoOverridesText = "no-overrides-text";
                public const string kOverridableFunctions = "overridable-functions";
            }
        }

        static void AddStyleSheet(VisualElement element)
        {
            StyleSheet styleSheet = EditorGUIUtility.Load(USS.kStyleSheetPath) as StyleSheet;
            if (styleSheet != null)
                element.styleSheets.Add(styleSheet);
        }

        static List<VirtualFunctionOverride> GetVisualizableOverrides(Shader localShader,
            VirtualFunction function)
        {
            List<VirtualFunctionOverride> result = new();
            foreach (VirtualFunctionOverride localOverride in function.LocalOverrides)
            {
                if (localOverride.BaseImplementationOwner != localShader)
                    result.Add(localOverride);
            }
            return result;
        }

        static bool IsUnimplementedPureVirtual(VirtualFunction function)
        {
            return function.Declaration.VirtualAttribute == VirtualFunctionDeclaration.Attribute.PureVirtual
                && !function.HasDefinition;
        }

        /// <summary>
        /// Creates a kind of inspector header for the given shader interface.
        /// It would be ideal to have this in the actual inspector header, but there's no public entry point
        /// in <see cref="Editor"/> to do so.
        /// </summary>
        public static VisualElement CreateInterfaceHeader(Shader s)
        {
            ShaderInterfaceReflection reflection = ShaderUtil.GetShaderInterfaceReflection(s);
            if (reflection == null)
                return null;

            if (reflection.BaseShader == null)
                return null;

            VisualElement root = new();
            AddStyleSheet(root);
            root.name = USS.Names.kHeader;

            // "Base" entry
            {
                ObjectField baseField = new ObjectField("Base")
                {
                    name = USS.Names.kHeaderBase,
                    value = reflection.BaseShader,
                    tooltip = "The shader from which this shader derives.",
                };
                // Make the field read-only
                baseField.Q(className: ObjectField.inputUssClassName)?.SetEnabled(false);
                root.Add(baseField);
            }

            return root;
        }

        /// <summary>
        /// Renders a "show generated code" button using IMGUI if the given shader asset is a
        /// shader interface.
        /// </summary>
        public static void DoShowSplicedCodeButton(Shader s)
        {
            // Locate the spliced text
            var assetPath = AssetDatabase.GetAssetPath(s);
            TextAsset splicedTextObject = null;
            foreach (Object assetObject in AssetDatabase.LoadAllAssetsAtPath(assetPath))
            {
                if (assetObject is TextAsset textAssetObject && textAssetObject.name == "SplicedText")
                {
                    splicedTextObject = textAssetObject;
                    break;
                }
            }

            if (splicedTextObject == null || string.IsNullOrEmpty(splicedTextObject.text))
                return;

            // Since this button is embedded within the regular shader inspector, we have to follow their
            // standard and use IMGUI.
            EditorGUILayout.BeginHorizontal();
            GUIContent labelContent = new("Shader interface",
                tooltip: "This shader uses shader interface syntax such as public or private sections.");
            EditorGUILayout.PrefixLabel(labelContent, EditorStyles.miniButton);
            GUIContent buttonContent = new("Show generated code",
                tooltip: "Opens the generated ShaderLab code in a text editor.");
            if (GUILayout.Button(buttonContent, EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
            {
                string tempFilePath = System.IO.Path.ChangeExtension($"Temp/{assetPath}", ".gen.shader");
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(tempFilePath));
                System.IO.File.WriteAllText(tempFilePath, splicedTextObject.text);
                CodeEditor.CurrentEditor?.OpenProject(tempFilePath);
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Creates a foldout containing information on all overridable functions in the given
        /// shader interface.
        /// </summary>
        public static VisualElement CreateOverridableFunctions(Shader s)
        {
            ShaderInterfaceReflection reflection = ShaderUtil.GetShaderInterfaceReflection(s);
            if (reflection == null)
                return null;

            ReadOnlyCollection<VirtualFunction> functions = reflection.VirtualFunctions;

            // Add foldout to hold virtual function info
            var root = new VisualElement()
            {
                name = USS.Names.kOverridableFunctions,
            };
            AddStyleSheet(root);
            var foldout = new Foldout()
            {
                text = "Overridable Functions",
            };
            if (functions.Count > 0)
                foldout.text += $" ({functions.Count})";
            Label foldoutLabel = foldout.Q<Label>(className: Foldout.textUssClassName);
            if (foldoutLabel != null)
                foldoutLabel.tooltip = "Overridable functions are functions declared by this shader or an ancestor"
                    + " whose behavior may be overridden.";

            // If the shader is abstract, throw up a help box
            bool isAbstract = false;
            foreach (var function in functions)
                isAbstract |= IsUnimplementedPureVirtual(function);
            if(isAbstract)
            {
                string helpText = $"This shader is abstract because it has at least one pure virtual function, so it cannot compile."
                    + " Extend it and implement all pure virtual functions to use it.";
                VisualElement infoBox = new HelpBox(helpText, HelpBoxMessageType.Info);
                foldout.Add(infoBox);
            }

            // Add virtual function info
            foreach (VirtualFunction function in functions)
            {
                List<VirtualFunctionOverride> localOverrides = GetVisualizableOverrides(s, function);
                foldout.Add(CreateOverridableFunction(s, function, localOverrides));
                if (localOverrides.Count > 0)
                    foldout.AddToClassList(USS.Classes.kBoldFoldout);
            }

            // Handle "no virtual functions" case
            if (functions.Count == 0)
            {
                var noOverridesLabel = new Label("This shader doesn't extend another shader or declare any"
                    + " overridable functions, so there's nothing to override.")
                {
                    name = USS.Names.kNoOverridesText,
                };
                foldout.Add(noOverridesLabel);
            }

            root.Add(foldout);
            return root;
        }

        class FunctionMetadataRow : VisualElement
        {
            public FunctionMetadataRow(string key, string value)
            {
                AddToClassList(USS.Classes.kVirtualFunctionMetaRow);
                Add(new Label(key));
                Add(new Label(value));
            }
        }

        static VisualElement CreateOverridableFunction(Shader inspectorTarget,
            VirtualFunction function,
            List<VirtualFunctionOverride> localOverrides)
        {
            VisualElement functionContainer = new();
            functionContainer.AddToClassList(USS.Classes.kOverridableFunction);

            VirtualFunctionDeclaration declaration = function.Declaration;

            // Create foldout
            string displayName = string.Empty;
            if (declaration.EnclosingNamespace.Count > 0)
            {
                displayName += string.Join("::", declaration.EnclosingNamespace);
                displayName += "::";
            }
            displayName += declaration.Name;
            VisualElement functionFoldout = new Foldout() { text = displayName };

            // If the function is pure virtual and not implemented, throw up a help box.
            // If the function is declared within this shader, though, the user obviously knows it's pure virtual,
            // so skip it.
            if (IsUnimplementedPureVirtual(function) && function.DeclarationOwner != inspectorTarget)
            {
                string helpText = $"{declaration.Name} is not implemented. It is declared as a pure virtual"
                    + " function, so this shader cannot compile. To make the shader compilable, implement it.";
                VisualElement infoBox = new HelpBox(helpText, HelpBoxMessageType.Info);
                VisualElement copyButton = new Button(() =>
                {
                    GUIUtility.systemCopyBuffer =
                     $"UNITY_OVERRIDE {declaration.GetSignature(includeNamespace: false)}\n{{\n}}\n";
                });
                copyButton.Add(new Label("Copy function stub"));
                infoBox.Add(copyButton);
                functionFoldout.Add(infoBox);
            }

            // Add function declaration
            {
                Label declLabel = new(declaration.GetSignature());
                declLabel.isSelectable = true;
                declLabel.AddToClassList(USS.Classes.kCodeBlock);
                functionFoldout.Add(declLabel);
            }

            // Add declaration scope
            static string ScopeToString(ShaderScope scope)
            {
                string passString = string.Empty;
                if (!string.IsNullOrEmpty(scope.PassName))
                    passString = $" \u00b7 {scope.PassName}";
                return $"Subshader {scope.SubShaderIndex}" + passString;
            }
            var scopeMetadata = new FunctionMetadataRow("Scope", ScopeToString(declaration.Scope))
            {
                tooltip = "The location within the shader where the function is defined.",
            };
            functionFoldout.Add(scopeMetadata);

            // Add override info
            if (localOverrides.Count > 0)
            {
                functionFoldout.AddToClassList(USS.Classes.kBoldFoldout);
                foreach (VirtualFunctionOverride funcOverride in localOverrides)
                {
                    if (funcOverride.BaseImplementationOwner == null)
                        continue;
                    var overridesMetadata = new FunctionMetadataRow("Overrides", funcOverride.BaseImplementationOwner.name)
                    {
                        tooltip = "The name of the shader which contains the original definition"
                            + $" of {function.Declaration.Name}.",
                    };
                    functionFoldout.Add(overridesMetadata);
                }
            }
            else if (function.DeclarationOwner != inspectorTarget)
            {
                var ownerName = function.DeclarationOwner?.name ?? "unknown";
                var declarationMetadata = new FunctionMetadataRow("Declared in", ownerName)
                {
                    tooltip = $"The name of the shader which first declares {function.Declaration.Name}.",
                };
                functionFoldout.Add(declarationMetadata);
            }

            functionContainer.Add(functionFoldout);
            return functionContainer;
        }
    }
}
