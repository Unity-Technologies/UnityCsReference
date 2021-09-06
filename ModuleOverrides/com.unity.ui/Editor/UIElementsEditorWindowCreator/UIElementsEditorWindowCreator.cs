// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;   
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor.UIElements.StyleSheets;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace UnityEditor.UIElements
{
    class UIElementsEditorWindowCreator : EditorWindow
    {
        const string k_UIElementsEditorWindowCreatorStyleSheetPath = "UIPackageResources/StyleSheets/UIElementsEditorWindowCreator.uss";
        const string k_UIElementsEditorWindowCreatorUxmlPath = "UIPackageResources/UXML/UIElementsEditorWindowCreator.uxml";

        internal const string k_CSharpTextFieldName = "cSharpTextField";
        internal const string k_UXMLTextFieldName = "uxmlTextField";
        internal const string k_USSTextFieldName = "ussTextField";
        internal const string k_UXMLToggleName = "uxmlToggle";
        internal const string k_USSToggleName = "ussToggle";
        internal const string k_OpenFilesToggleName = "openFilesToggle";
        internal const string k_ActionsDropdownName = "actionsDropdown";
        internal const string k_PathLabelName = "pathLabel";
        internal const string k_PathIconName = "pathIcon";
        internal const string k_ChooseFolderButtonName = "chooseFolderButton";

        internal const string k_JustCreateFilesOption = "Create files only";
        internal const string k_OpenFilesInUIBuilderOption = "Create files and open in UI Builder";
        internal const string k_OpenFilesInExternalEditorOption = "Create files and open in external editor";

        VisualElement m_Root;
        VisualElement m_ErrorMessageBox;

        string m_CSharpName = String.Empty;
        string m_UxmlName = String.Empty;
        string m_UssName = String.Empty;
        string m_Folder = String.Empty;
        string m_ErrorMessage = String.Empty;
        string m_ActionSelected = String.Empty;

        bool m_IsUssEnable = true;
        bool m_IsUxmlEnable = true;
        bool m_WaitingForSecondImport = false;

        private string cSharpPath
        {
            get
            {
                return Path.Combine(m_Folder, m_CSharpName + ".cs");
            }
        }

        private string uxmlPath
        {
            get
            {
                return Path.Combine(m_Folder, m_UxmlName + ".uxml");
            }
        }

        private string ussPath
        {
            get
            {
                return Path.Combine(m_Folder, m_UssName + ".uss");
            }
        }

        [MenuItem("Assets/Create/UI Toolkit/Editor Window", false, 701, false)]
        public static void CreateTemplateEditorWindow()
        {
            UIElementsEditorWindowCreator editorWindow = GetWindow<UIElementsEditorWindowCreator>(true, "UI Toolkit Editor Window Creator");
            editorWindow.maxSize = new Vector2(Styles.K_WindowWidth, Styles.K_WindowHeight);
            editorWindow.minSize = new Vector2(Styles.K_WindowWidth, Styles.K_WindowHeight);
            editorWindow.init();
        }

        public void init()
        {
            m_Folder = string.Empty;
            if (Selection.activeObject != null)
            {
                m_Folder = AssetDatabase.GetAssetPath(Selection.activeObject);
                if (!AssetDatabase.IsValidFolder(m_Folder))
                    m_Folder = string.Empty;
            }

            if (string.IsNullOrEmpty(m_Folder))
                ProjectWindowUtil.TryGetActiveFolderPath(out m_Folder);

            if (string.IsNullOrEmpty(m_Folder) || m_Folder.Equals("Assets"))
                m_Folder = "Assets/Editor";

            RefreshFolderLabel();
        }

        public void CreateGUI()
        {
            // After the c# file has been created and the domain.reload executed, we want to close the creator window and open the new editor window
            if (m_CSharpName != "" && ClassExists() && !m_WaitingForSecondImport)
            {
                EditorApplication.delayCall += () =>
                {
                    var defaultReferenceNames = new List<string>();
                    var defaultReferenceObjects = new List<UnityEngine.Object>();

                    // Add serialized references
                    if (m_IsUxmlEnable)
                    {
                        var vta = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);

                        defaultReferenceNames.Add("m_VisualTreeAsset");
                        defaultReferenceObjects.Add(vta);
                    }
                    else if (!m_IsUxmlEnable && m_IsUssEnable)
                    {
                        // If there is no uxml file, the stylesheet will be added to an element in the C# script
                        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(ussPath);

                        defaultReferenceNames.Add("m_StyleSheet");
                        defaultReferenceObjects.Add(styleSheet);
                    }

                    if (defaultReferenceNames.Count > 0)
                    {
                        m_WaitingForSecondImport = true;
                        RestoreWindowState();

                        var importer = AssetImporter.GetAtPath(cSharpPath) as MonoImporter;
                        importer.SetDefaultReferences(defaultReferenceNames.ToArray(), defaultReferenceObjects.ToArray());
                        AssetDatabase.ImportAsset(cSharpPath);
                    }
                    else
                    {
                        OnAfterScriptCreation();
                    }
                };
            }
            else
            {
                SetupLayout();
            }
        }

        private void RefreshFolderLabel()
        {
            var pathLabel = m_Root.Q<Label>(k_PathLabelName);
            pathLabel.text = m_Folder;
        }

        private void OnChooseFolderClicked()
        {
            var folder = EditorUtility.OpenFolderPanel("Choose Folder", m_Folder, "");
            if (!string.IsNullOrEmpty(folder))
            {
                folder = FileUtil.GetProjectRelativePath(folder);
                m_Folder = folder;
                RefreshFolderLabel();
            }
        }

        private void RestoreWindowState()
        {
            SetupLayout();

            var cSharpTextField = m_Root.Q<TextField>(k_CSharpTextFieldName);
            var uxmlTextField = m_Root.Q<TextField>(k_UXMLTextFieldName);
            var ussTextField = m_Root.Q<TextField>(k_USSTextFieldName);
            var uxmlToggle = m_Root.Q<Toggle>(k_UXMLToggleName);
            var ussToggle = m_Root.Q<Toggle>(k_USSToggleName);
            var actionsDropdown = m_Root.Q<DropdownField>(k_ActionsDropdownName);
            var pathLabel = m_Root.Q<Label>(k_PathLabelName);

            uxmlToggle.value = m_IsUxmlEnable;
            ussToggle.value = m_IsUssEnable;
            cSharpTextField.value = m_CSharpName;
            uxmlTextField.value = m_UxmlName;
            ussTextField.value = m_UssName;
            actionsDropdown.value = m_ActionSelected;
            pathLabel.text = m_Folder;

            m_Root.SetEnabled(false);
        }

        private void OnAfterScriptCreation()
        {
            // Open files if requested
            if (m_ActionSelected != k_JustCreateFilesOption)
            {
                OpenNewlyCreatedFiles();
            }

            // Show new editor window
            EditorApplication.ExecuteMenuItem("Window/UI Toolkit/" + m_CSharpName);

            // Ping file to open folder where it was created
            var cSharpFile = AssetDatabase.LoadAssetAtPath<MonoScript>(cSharpPath);
            Selection.activeObject = cSharpFile;
            EditorGUIUtility.PingObject(cSharpFile.GetInstanceID());

            // Close current window
            Close();
        }

        void OnGUI()
        {
            if (m_WaitingForSecondImport && !EditorApplication.isCompiling)
            {
                OnAfterScriptCreation();
                m_WaitingForSecondImport = false;
            }
        }

        void SetupLayout()
        {
            m_Root = rootVisualElement;
            m_Root.AddStyleSheetPath(k_UIElementsEditorWindowCreatorStyleSheetPath);

            var visualTree = EditorGUIUtility.Load(k_UIElementsEditorWindowCreatorUxmlPath) as VisualTreeAsset;
            VisualElement uxmlLayout = visualTree.Instantiate();
            m_Root.Add(uxmlLayout);

            m_ErrorMessageBox = m_Root.Q("errorMessageBox");

            var cSharpTextField = m_Root.Q<TextField>(k_CSharpTextFieldName);
            cSharpTextField.RegisterCallback<ChangeEvent<string>>(OnCSharpValueChanged);

            var cSharpTextInput = cSharpTextField.Q(TextField.textInputUssName);
            cSharpTextInput.RegisterCallback<KeyDownEvent>(OnReturnKey);
            cSharpTextInput.RegisterCallback<FocusEvent>(e => HideErrorMessage());

            m_Root.schedule.Execute(() => cSharpTextField.Focus());

            var uxmlTextField = m_Root.Q<TextField>(k_UXMLTextFieldName);
            uxmlTextField.RegisterCallback<ChangeEvent<string>>(e =>
            {
                m_ErrorMessageBox.style.visibility = Visibility.Hidden;
                m_UxmlName = e.newValue;
            });

            var uxmlTextInput = uxmlTextField.Q(TextField.textInputUssName);
            uxmlTextInput.RegisterCallback<KeyDownEvent>(OnReturnKey);
            uxmlTextInput.RegisterCallback<FocusEvent>(e => HideErrorMessage());

            m_Root.Q<Toggle>(k_UXMLToggleName).RegisterValueChangedCallback((evt) =>
            {
                m_IsUxmlEnable = evt.newValue;
                if (!m_IsUxmlEnable)
                {
                    uxmlTextField.value = "";
                    m_UxmlName = "";
                }

                uxmlTextInput.SetEnabled(m_IsUxmlEnable);

                UpdateActionChoices();
            });

            var ussTextField = m_Root.Q<TextField>(k_USSTextFieldName);
            ussTextField.RegisterCallback<ChangeEvent<string>>(e =>
            {
                m_ErrorMessageBox.style.visibility = Visibility.Hidden;
                m_UssName = e.newValue;
            });

            var ussTextInput = ussTextField.Q(TextField.textInputUssName);
            ussTextInput.RegisterCallback<KeyDownEvent>(OnReturnKey);
            ussTextInput.RegisterCallback<FocusEvent>(e => HideErrorMessage());

            m_Root.Q<Toggle>(k_USSToggleName).RegisterValueChangedCallback((evt) =>
            {
                m_IsUssEnable = evt.newValue;
                if (!m_IsUssEnable)
                {
                    ussTextField.value = "";
                    m_UssName = "";
                }

                ussTextInput.SetEnabled(m_IsUssEnable);
            });

            var actionsDropdown = m_Root.Q<DropdownField>(k_ActionsDropdownName);
            actionsDropdown.RegisterValueChangedCallback((evt) =>
            {
                m_ActionSelected = evt.newValue;
            });
            actionsDropdown.value = string.IsNullOrEmpty(m_ActionSelected) ? k_JustCreateFilesOption : m_ActionSelected;

            UpdateActionChoices();

            var pathIcon = m_Root.Q<VisualElement>(k_PathIconName);
            pathIcon.style.backgroundImage = EditorGUIUtility.LoadIcon("FolderOpened Icon");

            var chooseFolderButton = m_Root.Q<Button>(k_ChooseFolderButtonName);
            chooseFolderButton.clicked += OnChooseFolderClicked;

            m_Root.Q<Button>("confirmButton").clickable.clicked += CreateNewTemplatesFiles;
            m_ErrorMessageBox.Q<Image>("warningIcon").image = EditorGUIUtility.GetHelpIcon(MessageType.Warning);
            HideErrorMessage();
        }

        void UpdateActionChoices()
        {
            var actionsDropdown = m_Root.Q<DropdownField>(k_ActionsDropdownName);

            if (m_IsUxmlEnable)
            {
                actionsDropdown.choices = new List<string>() { k_JustCreateFilesOption, k_OpenFilesInUIBuilderOption, k_OpenFilesInExternalEditorOption };
            }
            else
            {
                actionsDropdown.choices = new List<string>() { k_JustCreateFilesOption, k_OpenFilesInExternalEditorOption };

                if (m_ActionSelected == k_OpenFilesInUIBuilderOption)
                {
                    actionsDropdown.value = k_JustCreateFilesOption;
                }
            }
        }

        void ShowErrorMessage()
        {
            if (m_ErrorMessageBox.resolvedStyle.visibility == Visibility.Hidden)
            {
                m_ErrorMessageBox.style.visibility = Visibility.Visible;
                m_ErrorMessageBox.style.position = Position.Relative;
                m_ErrorMessageBox.Q<Label>("errorLabel").text = m_ErrorMessage;
                maxSize = new Vector2(Styles.K_WindowWidth, Styles.K_WindowHeight + m_ErrorMessageBox.contentRect.height);
                minSize = new Vector2(Styles.K_WindowWidth, Styles.K_WindowHeight + m_ErrorMessageBox.contentRect.height);
            }
        }

        void HideErrorMessage()
        {
            if (m_ErrorMessageBox.resolvedStyle.visibility == Visibility.Visible)
            {
                m_ErrorMessageBox.style.visibility = Visibility.Hidden;
                m_ErrorMessageBox.style.position = Position.Absolute;
                maxSize = new Vector2(Styles.K_WindowWidth, Styles.K_WindowHeight);
                minSize = new Vector2(Styles.K_WindowWidth, Styles.K_WindowHeight);
            }
        }

        internal void CreateNewTemplatesFiles()
        {
            if (IsInputValid())
            {
                m_Root.SetEnabled(false);

                if (!Directory.Exists(m_Folder))
                {
                    Directory.CreateDirectory(m_Folder);
                }

                StyleSheet styleSheet = null;
                VisualTreeAsset visualTreeAsset = null;

                if (m_IsUssEnable)
                {
                    File.WriteAllText(ussPath, GetUssTemplateContent());
                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                    styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(ussPath);
                }

                if (m_IsUxmlEnable)
                {
                    var stringBuilder = new StringBuilder();

                    if (m_IsUssEnable)
                    {
                        var assetUri = URIHelpers.MakeAssetUri(styleSheet);
                        var encodedUri = URIHelpers.EncodeUri(assetUri);
                        stringBuilder.AppendLine(string.Format(@"<Style src=""{0}"" />", encodedUri));
                        stringBuilder.Append('\t');
                    }

                    stringBuilder.AppendLine(@"<engine:Label text=""Hello World! From UXML"" />");

                    if (m_IsUssEnable)
                    {
                        stringBuilder.Append('\t');
                        stringBuilder.AppendLine(@"<engine:Label class=""custom-label"" text=""Hello World! With Style"" />");
                    }

                    File.WriteAllText(uxmlPath, UIElementsTemplate.CreateUXMLTemplate(m_Folder, stringBuilder.ToString()));
                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                    visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
                }

                File.WriteAllText(cSharpPath, UIElementsTemplate.CreateCSharpTemplate(m_CSharpName, m_IsUxmlEnable, m_IsUssEnable && !m_IsUxmlEnable));
                AssetDatabase.Refresh();
            }
            else
            {
                ShowErrorMessage();
            }
        }

        internal static string GetUssTemplateContent()
        {
            return @".custom-label {
    font-size: 20px;
    -unity-font-style: bold;
    color: rgb(68, 138, 255);
}";
        }

        void OnReturnKey(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Return)
            {
                CreateNewTemplatesFiles();
            }
        }

        void OnCSharpValueChanged(ChangeEvent<string> changeEvt)
        {
            var previousName = m_CSharpName;
            m_CSharpName = changeEvt.newValue;
            HideErrorMessage();

            if (m_IsUxmlEnable)
            {
                var uxmlTextField = m_Root.Q<TextField>(k_UXMLTextFieldName);
                if (uxmlTextField.value == previousName)
                {
                    uxmlTextField.value = m_CSharpName;
                    m_UxmlName = m_CSharpName;
                }
            }

            if (m_IsUssEnable)
            {
                var ussTextField = m_Root.Q<TextField>(k_USSTextFieldName);
                if (ussTextField.value == previousName)
                {
                    ussTextField.value = m_CSharpName;
                    m_UssName = m_CSharpName;
                }
            }
        }

        bool IsInputValid()
        {
            if (string.IsNullOrEmpty(m_Folder))
            {
                m_ErrorMessage = "Path is invalid.";
                return false;
            }

            if (!Validate(m_CSharpName, ".cs") || ClassExists())
            {
                return false;
            }

            if (m_IsUssEnable && !Validate(m_UssName, ".uss"))
            {
                return false;
            }

            if (m_IsUxmlEnable && !Validate(m_UxmlName, ".uxml"))
            {
                return false;
            }

            return true;
        }

        bool Validate(string fileName, string extension)
        {
            bool isValid = true;
            if (fileName.Length <= 0)
            {
                m_ErrorMessage = "Filename fields cannot be blank.";
                isValid = false;
            }
            else if (!System.CodeDom.Compiler.CodeGenerator.IsValidLanguageIndependentIdentifier(fileName))
            {
                m_ErrorMessage = "Filename is invalid.";
                isValid = false;
            }
            else if (File.Exists(Path.Combine(m_Folder, fileName + extension)))
            {
                m_ErrorMessage = "Filename " + fileName + " already exists.";
                isValid = false;
            }

            return isValid;
        }

        bool ClassExists()
        {
            bool classExists = AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetType(m_CSharpName, false) != null);
            if (classExists)
            {
                m_ErrorMessage = "Class name " + name + " already exists.";
            }

            return classExists;
        }

        void OpenNewlyCreatedFiles()
        {
            if (m_ActionSelected == k_OpenFilesInUIBuilderOption && m_IsUxmlEnable)
            {
                var uxmlFile = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
                AssetDatabase.OpenAsset(uxmlFile);
            }
            else if (m_ActionSelected == k_OpenFilesInExternalEditorOption)
            {
                InternalEditorUtility.OpenFileAtLineExternal(cSharpPath, -1, -1);

                if (m_IsUxmlEnable)
                {
                    InternalEditorUtility.OpenFileAtLineExternal(uxmlPath, -1, -1);
                }

                if (m_IsUssEnable)
                {
                    InternalEditorUtility.OpenFileAtLineExternal(ussPath, -1, -1);
                }
            }
        }
    }

    internal static class Styles
    {
        internal const float K_WindowHeight = 205;
        internal const float K_WindowWidth = 400;
    }
}
