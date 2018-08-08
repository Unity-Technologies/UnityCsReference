// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Linq;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using Button = UnityEngine.Experimental.UIElements.Button;

namespace UnityEditor.Experimental.UIElements
{
    class UIElementsEditorWindowCreator : EditorWindow
    {
        const string k_UIElementsEditorWindowCreatorStyleSheetPath = "StyleSheets/UIElementsEditorWindowCreator.uss";
        const string k_UIElementsEditorWindowCreatorUxmlPath = "UXML/UIElementsEditorWindowCreator.uxml";

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

        [MenuItem("Assets/Create/UIElements Editor Window", true)]
        public static bool IsValidPath()
        {
            return ProjectWindowUtil.GetActiveFolderPath().Split('/').Contains("Editor");
        }

        [MenuItem("Assets/Create/UIElements Editor Window")]
        public static void CreateTemplateMenuItem()
        {
            UIElementsEditorWindowCreator editorWindow = GetWindow<UIElementsEditorWindowCreator>(true, "UIElements Editor Window Creator");
            editorWindow.maxSize = new Vector2(Styles.K_WindowWidth, Styles.K_WindowHeight);
            editorWindow.minSize = new Vector2(Styles.K_WindowWidth, Styles.K_WindowHeight);
            editorWindow.init();
        }

        public void init()
        {
            m_Folder = ProjectWindowUtil.GetActiveFolderPath();
        }

        public void OnEnable()
        {
            // After the c# file has been created and the domain.reload executed, we want to close the window
            if (m_CSharpName != "" && ClassExists())
            {
                EditorApplication.ExecuteMenuItem("Window/UIElements/" + m_CSharpName);

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
            m_Root = this.GetRootVisualContainer();
            m_Root.AddStyleSheetPath(k_UIElementsEditorWindowCreatorStyleSheetPath);

            var visualTree = EditorGUIUtility.Load(k_UIElementsEditorWindowCreatorUxmlPath) as VisualTreeAsset;
            VisualElement uxmlLayout = visualTree.CloneTree(null);
            m_Root.Add(uxmlLayout);

            m_ErrorMessageBox = m_Root.Q<VisualElement>("errorMessageBox");

            var cSharpTextField = m_Root.Q<TextField>("cSharpTextField");
            cSharpTextField.RegisterCallback<KeyDownEvent>((e) => OnReturnKey(e));
            cSharpTextField.RegisterCallback<ChangeEvent<string>>(e => OnCSharpValueChanged(e));
            cSharpTextField.RegisterCallback<FocusEvent>((e) => HideErrorMessage());

            m_Root.Q<Toggle>("cSharpToggle").OnValueChanged((evt) =>
            {
                m_IsCSharpEnable = evt.newValue;
                if (!m_IsCSharpEnable)
                {
                    cSharpTextField.value = "";
                    m_CSharpName = "";
                }

                cSharpTextField.SetEnabled(m_IsCSharpEnable);
            });

            m_Root.schedule.Execute(() => cSharpTextField.Focus());

            var uxmlTextField = m_Root.Q<TextField>("uxmlTextField");
            uxmlTextField.RegisterCallback<KeyDownEvent>((e) => OnReturnKey(e));
            uxmlTextField.RegisterCallback<ChangeEvent<string>>(e =>
            {
                m_ErrorMessageBox.style.visibility = Visibility.Hidden;
                m_UxmlName = e.newValue;
            });
            uxmlTextField.RegisterCallback<FocusEvent>((e) => HideErrorMessage());
            m_Root.Q<Toggle>("uxmlToggle").OnValueChanged((evt) =>
            {
                m_IsUxmlEnable = evt.newValue;
                if (!m_IsUxmlEnable)
                {
                    uxmlTextField.value = "";
                    m_UxmlName = "";
                }

                uxmlTextField.SetEnabled(m_IsUxmlEnable);
            });

            var ussTextField = m_Root.Q<TextField>("ussTextField");
            ussTextField.RegisterCallback<KeyDownEvent>((e) => OnReturnKey(e));
            ussTextField.RegisterCallback<ChangeEvent<string>>(e =>
            {
                m_ErrorMessageBox.style.visibility = Visibility.Hidden;
                m_UssName = e.newValue;
            });
            ussTextField.RegisterCallback<FocusEvent>((e) => HideErrorMessage());

            m_Root.Q<Toggle>("ussToggle").OnValueChanged((evt) =>
            {
                m_IsUssEnable = evt.newValue;
                if (!m_IsUssEnable)
                {
                    ussTextField.value = "";
                    m_UssName = "";
                }

                ussTextField.SetEnabled(m_IsUssEnable);
            });

            m_Root.Q<Button>("confirmButton").clickable.clicked += () => CreateNewTemplatesFiles();
            m_ErrorMessageBox.Q<Image>("warningIcon").image = EditorGUIUtility.GetHelpIcon(MessageType.Warning);
            HideErrorMessage();
        }

        void ShowErrorMessage()
        {
            if (m_ErrorMessageBox.style.visibility == Visibility.Hidden)
            {
                m_ErrorMessageBox.style.visibility = Visibility.Visible;
                m_ErrorMessageBox.style.positionType = PositionType.Relative;
                m_ErrorMessageBox.Q<Label>("errorLabel").text = m_ErrorMessage;
                maxSize = new Vector2(Styles.K_WindowWidth, Styles.K_WindowHeight + m_ErrorMessageBox.contentRect.height);
                minSize = new Vector2(Styles.K_WindowWidth, Styles.K_WindowHeight + m_ErrorMessageBox.contentRect.height);
                position =  new Rect(position.position, maxSize);
            }
        }

        void HideErrorMessage()
        {
            if (m_ErrorMessageBox.style.visibility == Visibility.Visible)
            {
                m_ErrorMessageBox.style.visibility = Visibility.Hidden;
                m_ErrorMessageBox.style.positionType = PositionType.Absolute;
                maxSize = new Vector2(Styles.K_WindowWidth, Styles.K_WindowHeight);
                minSize = new Vector2(Styles.K_WindowWidth, Styles.K_WindowHeight);
                position =  new Rect(position.position, maxSize);
            }
        }

        void CreateNewTemplatesFiles()
        {
            if (IsInputValid())
            {
                m_Root.SetEnabled(false);

                if (m_IsUxmlEnable)
                {
                    var uxmlPath = Path.Combine(m_Folder, m_UxmlName + ".uxml");
                    ProjectWindowUtil.CreateAssetWithContent(uxmlPath, UIElementsTemplate.CreateUXMLTemplate(m_Folder),
                        EditorGUIUtility.FindTexture(typeof(VisualTreeAsset)));
                }

                if (m_IsUssEnable)
                {
                    var ussPath = Path.Combine(m_Folder, m_UssName + ".uss");
                    ProjectWindowUtil.CreateAssetWithContent(ussPath, UIElementsTemplate.CreateUssTemplate(),
                        EditorGUIUtility.FindTexture(typeof(VisualTreeAsset)));
                }

                if (m_IsCSharpEnable)
                {
                    var cSharpPath = Path.Combine(m_Folder, m_CSharpName + ".cs");
                    ProjectWindowUtil.CreateScriptAssetWithContent(cSharpPath, UIElementsTemplate.CreateCSharpTemplate(m_CSharpName, m_UxmlName, m_UssName, m_Folder));
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
                m_Root.Q<TextField>("ussTextField").value = m_CSharpName + "_style";
                m_UssName = m_CSharpName + "_style";
            }
        }

        bool IsInputValid()
        {
            if (!IsAtLeastOneFileCreated())
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

            if (!IsUssAndUxmlNamedDifferently())
            {
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

        bool IsUssAndUxmlNamedDifferently()
        {
            bool isUssAndUxmlNamedDifferently = (m_UssName != m_UxmlName) || (!m_IsUxmlEnable || !m_IsUssEnable);
            if (!isUssAndUxmlNamedDifferently)
            {
                m_ErrorMessage = "The uss file and the uxml file must have different name.";
            }

            return isUssAndUxmlNamedDifferently;
        }

        bool Validate(string name, string extension)
        {
            bool isValid = true;
            if (name.Length <= 0)
            {
                m_ErrorMessage = "Filename fields cannot be blank.";
                isValid = false;
            }
            else if (!System.CodeDom.Compiler.CodeGenerator.IsValidLanguageIndependentIdentifier(name))
            {
                m_ErrorMessage = "Filename is invalid.";
                isValid = false;
            }
            else if (File.Exists(Path.Combine(m_Folder, name + extension)))
            {
                m_ErrorMessage = "Filename " + name + " already exists.";
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
                InternalEditorUtility.OpenFileAtLineExternal(Path.Combine(m_Folder, m_CSharpName + ".cs"), -1);
            }

            if (m_IsUxmlEnable)
            {
                InternalEditorUtility.OpenFileAtLineExternal(Path.Combine(m_Folder, m_UxmlName + ".uxml"), -1);
            }

            if (m_IsUssEnable)
            {
                InternalEditorUtility.OpenFileAtLineExternal(Path.Combine(m_Folder, m_UssName + ".uss"), -1);
            }
        }
    }

    internal static class Styles
    {
        internal const float K_WindowHeight = 180;
        internal const float K_WindowWidth = 400;
    }
}
