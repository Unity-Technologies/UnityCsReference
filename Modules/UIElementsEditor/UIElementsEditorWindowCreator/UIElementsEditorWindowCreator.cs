// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Linq;
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

        VisualElement m_Root;
        VisualElement m_ErrorMessageBox;
        string m_CSharpName = String.Empty;
        string m_UxmlName = String.Empty;
        string m_UssName = String.Empty;

        string m_Folder = String.Empty;

        string m_ErrorMessage = String.Empty;

        bool m_IsCSharpEnable = true;
        bool m_IsUssEnable = true;
        bool m_IsUxmlEnable = true;

        [MenuItem("Assets/Create/UI Toolkit/Editor Window", false, 701, false)]
        public static void CreateTemplateEditorWindow()
        {
            if (CommandService.Exists(nameof(CreateTemplateEditorWindow)))
                CommandService.Execute(nameof(CreateTemplateEditorWindow), CommandHint.Menu);
            else
            {
                UIElementsEditorWindowCreator editorWindow = GetWindow<UIElementsEditorWindowCreator>(true, "UI Toolkit Editor Window Creator");
                editorWindow.maxSize = new Vector2(Styles.K_WindowWidth, Styles.K_WindowHeight);
                editorWindow.minSize = new Vector2(Styles.K_WindowWidth, Styles.K_WindowHeight);
                editorWindow.init();
            }
        }

        public void init()
        {
            m_Folder = string.Empty;

            if (!ProjectWindowUtil.TryGetActiveFolderPath(out m_Folder))
            {
                if (Selection.activeObject != null)
                {
                    m_Folder = AssetDatabase.GetAssetPath(Selection.activeObject);

                    if (!AssetDatabase.IsValidFolder(m_Folder))
                    {
                        m_Folder = Path.GetDirectoryName(m_Folder);
                    }

                    if (!AssetDatabase.IsValidFolder(m_Folder))
                    {
                        m_Folder = string.Empty;
                    }
                }
            }

            if (string.IsNullOrEmpty(m_Folder))
            {
                m_Folder = "Assets/Editor";
            }
        }

        public void OnEnable()
        {
            // After the c# file has been created and the domain.reload executed, we want to close the window
            if (m_CSharpName != "" && ClassExists())
            {
                EditorApplication.ExecuteMenuItem("Window/Project/" + m_CSharpName);

                EditorApplication.CallbackFunction handler = null;
                handler = () =>
                {
                    EditorApplication.update -= handler;
                    Close();
                };
                EditorApplication.update += handler;
            }
            else
            {
                SetupLayout();
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

            var cSharpTextField = m_Root.Q<TextField>("cSharpTextField");
            cSharpTextField.RegisterCallback<ChangeEvent<string>>(OnCSharpValueChanged);

            var cSharpTextInput = cSharpTextField.Q(TextField.textInputUssName);
            cSharpTextInput.RegisterCallback<KeyDownEvent>(OnReturnKey);
            cSharpTextInput.RegisterCallback<FocusEvent>(e => HideErrorMessage());

            m_Root.Q<Toggle>("cSharpToggle").RegisterValueChangedCallback((evt) =>
            {
                m_IsCSharpEnable = evt.newValue;
                if (!m_IsCSharpEnable)
                {
                    cSharpTextField.value = "";
                    m_CSharpName = "";
                }

                cSharpTextInput.SetEnabled(m_IsCSharpEnable);
            });

            m_Root.schedule.Execute(() => cSharpTextField.Focus());

            var uxmlTextField = m_Root.Q<TextField>("uxmlTextField");
            uxmlTextField.RegisterCallback<ChangeEvent<string>>(e =>
            {
                m_ErrorMessageBox.style.visibility = Visibility.Hidden;
                m_UxmlName = e.newValue;
            });

            var uxmlTextInput = uxmlTextField.Q(TextField.textInputUssName);
            uxmlTextInput.RegisterCallback<KeyDownEvent>(OnReturnKey);
            uxmlTextInput.RegisterCallback<FocusEvent>(e => HideErrorMessage());

            m_Root.Q<Toggle>("uxmlToggle").RegisterValueChangedCallback((evt) =>
            {
                m_IsUxmlEnable = evt.newValue;
                if (!m_IsUxmlEnable)
                {
                    uxmlTextField.value = "";
                    m_UxmlName = "";
                }

                uxmlTextInput.SetEnabled(m_IsUxmlEnable);
            });

            var ussTextField = m_Root.Q<TextField>("ussTextField");
            ussTextField.RegisterCallback<ChangeEvent<string>>(e =>
            {
                m_ErrorMessageBox.style.visibility = Visibility.Hidden;
                m_UssName = e.newValue;
            });

            var ussTextInput = ussTextField.Q(TextField.textInputUssName);
            ussTextInput.RegisterCallback<KeyDownEvent>(OnReturnKey);
            ussTextInput.RegisterCallback<FocusEvent>(e => HideErrorMessage());

            m_Root.Q<Toggle>("ussToggle").RegisterValueChangedCallback((evt) =>
            {
                m_IsUssEnable = evt.newValue;
                if (!m_IsUssEnable)
                {
                    ussTextField.value = "";
                    m_UssName = "";
                }

                ussTextInput.SetEnabled(m_IsUssEnable);
            });

            m_Root.Q<Button>("confirmButton").clickable.clicked += CreateNewTemplatesFiles;
            m_ErrorMessageBox.Q<Image>("warningIcon").image = EditorGUIUtility.GetHelpIcon(MessageType.Warning);
            HideErrorMessage();
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

        void CreateNewTemplatesFiles()
        {
            if (IsInputValid())
            {
                m_Root.SetEnabled(false);

                if (!Directory.Exists(m_Folder))
                {
                    Directory.CreateDirectory(m_Folder);
                }

                if (m_IsUxmlEnable)
                {
                    var uxmlPath = Path.Combine(m_Folder, m_UxmlName + ".uxml");
                    File.WriteAllText(uxmlPath, UIElementsTemplate.CreateUXMLTemplate(m_Folder, "<engine:Label text=\"Hello World! From UXML\" />"));
                }

                if (m_IsUssEnable)
                {
                    var ussPath = Path.Combine(m_Folder, m_UssName + ".uss");
                    File.WriteAllText(ussPath, GetUssTemplateContent());
                }

                if (m_IsUssEnable || m_IsUxmlEnable)
                {
                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                }

                if (m_IsCSharpEnable)
                {
                    var cSharpPath = Path.Combine(m_Folder, m_CSharpName + ".cs");
                    File.WriteAllText(cSharpPath, UIElementsTemplate.CreateCSharpTemplate(m_CSharpName, m_UxmlName, m_UssName, m_Folder));
                    AssetDatabase.Refresh();
                }
                else
                {
                    Close();
                }

                if (m_Root.Q<Toggle>("openFilesToggle").value)
                    OpenNewlyCreatedFiles();
            }
            else
            {
                ShowErrorMessage();
            }
        }

        internal static string GetUssTemplateContent()
        {
            return @"Label {
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
            m_CSharpName = changeEvt.newValue;
            HideErrorMessage();

            if (m_IsUxmlEnable)
            {
                m_Root.Q<TextField>("uxmlTextField").value = m_CSharpName;
                m_UxmlName = m_CSharpName;
            }

            if (m_IsUssEnable)
            {
                m_Root.Q<TextField>("ussTextField").value = m_CSharpName;
                m_UssName = m_CSharpName;
            }
        }

        bool IsInputValid()
        {
            if (!IsAtLeastOneFileCreated())
            {
                return false;
            }

            if (!IsValidPath())
            {
                return false;
            }

            if (m_IsCSharpEnable && (!Validate(m_CSharpName, ".cs") || ClassExists()))
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

        bool IsValidPath()
        {
            if (m_Folder.Split('/').Contains("Editor") == false)
            {
                m_ErrorMessage = "The target path must be located inside an Editor folder";
                return false;
            }

            return true;
        }

        bool IsAtLeastOneFileCreated()
        {
            bool isAtLeastOneFileCreated = m_IsCSharpEnable || m_IsUssEnable || m_IsUxmlEnable;
            if (!isAtLeastOneFileCreated)
            {
                m_ErrorMessage = "At least one file must be created.";
            }

            return isAtLeastOneFileCreated;
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
            if (m_IsCSharpEnable)
            {
                InternalEditorUtility.OpenFileAtLineExternal(Path.Combine(m_Folder, m_CSharpName + ".cs"), -1, -1);
            }

            if (m_IsUxmlEnable)
            {
                InternalEditorUtility.OpenFileAtLineExternal(Path.Combine(m_Folder, m_UxmlName + ".uxml"), -1, -1);
            }

            if (m_IsUssEnable)
            {
                InternalEditorUtility.OpenFileAtLineExternal(Path.Combine(m_Folder, m_UssName + ".uss"), -1, -1);
            }
        }
    }

    internal static class Styles
    {
        internal const float K_WindowHeight = 166;
        internal const float K_WindowWidth = 400;
    }
}
