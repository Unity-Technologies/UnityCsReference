// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using Object = UnityEngine.Object;

namespace Unity.UIToolkit.Editor
{
    /// <summary>
    /// Field for image reference attributes that accepts Texture2D, Sprite, VectorImage, or RenderTexture.
    /// </summary>
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal class ImageField : BaseField<Object>
    {
        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public static readonly string UssClass = "unity-image-field";
        /// <summary>
        /// USS class name of the object field in elements of this type.
        /// </summary>
        public static readonly string ObjectFieldUssClass = UssClass + "__object-field";
        /// <summary>
        /// USS class name of the options popup container in elements of this type.
        /// </summary>
        public static readonly string OptionsPopupContainerName = UssClass + "__options-popup-container";
        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public static readonly string InputUssClass = UssClass + "__visual-input";
        /// <summary>
        /// USS class name for the edit button (hidden when not Sprite).
        /// </summary>
        public static readonly string EditButtonHiddenClass = UssClass + "__edit-button--hidden";

        const string k_2DSpriteEditorPackageName = "com.unity.2d.sprite";

        static readonly string k_2DSpriteEditorButtonText = L10n.Tr("Open in Sprite Editor");
        static readonly string k_2DSpriteEditorButtonTooltip_Installed = L10n.Tr("Use the Sprite Editor to 9-slice the image or edit its 9-slicing values.");
        static readonly string k_2DSpriteEditorButtonTooltip_NotInstalled = L10n.Tr("Use the Sprite Editor to 9-slice the image or edit its 9-slicing values. Unity will prompt you to install the com.unity.2d.sprite package first.");
        static readonly string k_No2DSpriteEditorPackageInstalledTitle = L10n.Tr("Package required - 2D Sprite Editor");
        static readonly string k_No2DSpriteEditorPackageInstalledMessage = L10n.Tr(
            "You must install the 2D Sprite Editor package to edit Sprites.\n" +
            "If you do not install the package, you can use existing Sprites, but you cannot create or modify them.\n" +
            "Do you want to install the package now?");
        static readonly string k_InstallButtonText = L10n.Tr("Install");
        static readonly string k_CancelButtonText = L10n.Tr("Cancel");
        static readonly string k_PackageInstallSuccessMessage = L10n.Tr("Successfully installed package \"{0}\".");
        static readonly string k_PackageInstallErrorMessage = L10n.Tr("Could not install package \"{0}\". Error: {1}");

        static readonly string k_TextureTypeDisplayName = L10n.Tr("Texture");
        static readonly string k_RenderTextureTypeDisplayName = L10n.Tr("Render Texture");
        static readonly string k_SpriteTypeDisplayName = L10n.Tr("Sprite");
        static readonly string k_VectorTypeDisplayName = L10n.Tr("Vector");

        static readonly List<Type> s_SupportedImageTypes = new()
        {
            typeof(Texture2D),
            typeof(RenderTexture),
            typeof(Sprite),
            typeof(VectorImage)
        };

        static bool? s_Is2DSpriteEditorInstalled;
        static ListRequest s_PackageListRequest;

        readonly ObjectField m_ObjectField;
        readonly Dictionary<string, Type> m_TypeOptions;
        readonly PopupField<string> m_TypePopup;
        readonly Button m_EditButton;

        public ObjectField objectField => m_ObjectField;
        public PopupField<string> typePopup => m_TypePopup;

        static bool Is2DSpriteEditorInstalled
        {
            get
            {
                // Start the async request if not already started
                if (s_Is2DSpriteEditorInstalled == null && s_PackageListRequest == null)
                {
                    s_PackageListRequest = Client.List(true);
                    EditorApplication.update += CheckPackageListCompletion;
                }

                // Return false as default until we know for sure
                return s_Is2DSpriteEditorInstalled ?? false;
            }
        }

        static void CheckPackageListCompletion()
        {
            if (s_PackageListRequest == null || !s_PackageListRequest.IsCompleted)
                return;

            EditorApplication.update -= CheckPackageListCompletion;

            s_Is2DSpriteEditorInstalled = false;
            if (s_PackageListRequest.Status == StatusCode.Success)
            {
                foreach (var package in s_PackageListRequest.Result)
                {
                    if (package.name == k_2DSpriteEditorPackageName)
                    {
                        s_Is2DSpriteEditorInstalled = true;
                        break;
                    }
                }
            }

            s_PackageListRequest = null;
        }

        public ImageField() : this(null) {}

        public ImageField(string label) : base(label, null)
        {
            AddToClassList(UssClass);

            // Load stylesheet
            var styleSheet = EditorGUIUtility.Load("UIToolkitAuthoring/Inspector/Controls/Fields/ImageField.uss") as StyleSheet;
            if (styleSheet != null)
                styleSheets.Add(styleSheet);

            m_TypeOptions = new Dictionary<string, Type>();
            m_ObjectField = new ObjectField().WithClassList(ObjectFieldUssClass);
            m_ObjectField.RegisterValueChangedCallback(OnObjectValueChange);

            m_ObjectField.objectFieldDisplay.RegisterDefaultDragAndDrop(s_SupportedImageTypes);

            m_TypePopup = new PopupField<string>() { formatSelectedValueCallback = OnFormatSelectedValue, choices = [] };

            var popupContainer = new VisualElement() { name = OptionsPopupContainerName }.WithClassList(OptionsPopupContainerName);

            popupContainer.Add(m_ObjectField);
            popupContainer.Add(m_TypePopup);

            visualInput.AddToClassList(InputUssClass);
            visualInput.Add(popupContainer);

            // Add Sprite Editor button
            m_EditButton = new Button(OnEditButtonClicked)
            {
                text = k_2DSpriteEditorButtonText,
                // Start with "not installed" tooltip as default until check completes
                tooltip = k_2DSpriteEditorButtonTooltip_NotInstalled
            };
            m_EditButton.SetEnabled(false); // Disable until package check completes
            visualInput.Add(m_EditButton);

            // Register lifecycle callbacks for symmetrical subscription management
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            AddType(typeof(Texture2D), k_TextureTypeDisplayName);
            AddType(typeof(RenderTexture), k_RenderTextureTypeDisplayName);
            AddType(typeof(Sprite), k_SpriteTypeDisplayName);
            AddType(typeof(VectorImage), k_VectorTypeDisplayName);

            // Update edit button visibility based on type
            m_TypePopup.RegisterValueChangedCallback(evt =>
            {
                UpdateEditButtonVisibility();
            });
            UpdateEditButtonVisibility();
        }

        void OnObjectValueChange(ChangeEvent<Object> evt)
        {
            value = evt.newValue;
            evt.StopImmediatePropagation();
        }

        protected void AddType(Type type)
        {
            AddType(type, type.Name);
        }

        protected void AddType(Type type, string displayName)
        {
            if (m_TypeOptions.ContainsKey(displayName))
                throw new ArgumentException($"Item with the name: {displayName} already exists.", nameof(displayName));

            m_TypeOptions.Add(displayName, type);
            m_TypePopup.choices.Add(displayName);

            m_TypePopup.style.display = m_TypeOptions.Count <= 1
                ? DisplayStyle.None
                : DisplayStyle.Flex;

            if (string.IsNullOrEmpty(m_TypePopup.value))
                m_TypePopup.value = displayName;
        }

        string OnFormatSelectedValue(string formatValue)
        {
            if (m_TypeOptions.Count > 0)
            {
                m_ObjectField.objectType = m_TypeOptions[formatValue];
                if (!m_ObjectField.value) return formatValue;
                if (!m_ObjectField.objectType.IsInstanceOfType(m_ObjectField.value))
                    m_ObjectField.value = null;
            }

            return formatValue;
        }

        public override void SetValueWithoutNotify(Object newValue)
        {
            m_ObjectField.SetValueWithoutNotify(newValue);
            if (newValue)
            {
                foreach (var pair in m_TypeOptions)
                {
                    if (pair.Value.IsInstanceOfType(newValue))
                    {
                        m_TypePopup.SetValueWithoutNotify(pair.Key);
                        break;
                    }
                }
            }

            base.SetValueWithoutNotify(newValue);
        }

        void UpdateEditButtonVisibility()
        {
            // Only show the edit button when Sprite type is selected
            var isSprite = m_TypePopup.value == k_SpriteTypeDisplayName;
            m_EditButton.EnableInClassList(EditButtonHiddenClass, !isSprite);
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            // Trigger package check and subscribe to updates
            _ = Is2DSpriteEditorInstalled; // Trigger the check
            EditorApplication.update += UpdateEditButtonTooltip;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            // Symmetrical cleanup: unsubscribe from EditorApplication.update
            EditorApplication.update -= UpdateEditButtonTooltip;
        }

        void UpdateEditButtonTooltip()
        {
            // Once we know if the package is installed, update tooltip and enable button
            if (s_Is2DSpriteEditorInstalled.HasValue)
            {
                EditorApplication.update -= UpdateEditButtonTooltip;
                m_EditButton.tooltip = s_Is2DSpriteEditorInstalled.Value
                    ? k_2DSpriteEditorButtonTooltip_Installed
                    : k_2DSpriteEditorButtonTooltip_NotInstalled;
                m_EditButton.SetEnabled(true);
            }
        }

        void OnEditButtonClicked()
        {
            if (Is2DSpriteEditorInstalled)
            {
                OpenSpriteEditor(value);
                return;
            }

            // Prompt user to install the 2D Sprite Editor package
            if (EditorUtility.DisplayDialog(
                k_No2DSpriteEditorPackageInstalledTitle,
                k_No2DSpriteEditorPackageInstalledMessage,
                k_InstallButtonText,
                k_CancelButtonText))
            {
                var addRequest = Client.Add(k_2DSpriteEditorPackageName);
                EditorApplication.update += CheckInstallationProgress;

                void CheckInstallationProgress()
                {
                    if (addRequest.IsCompleted)
                    {
                        EditorApplication.update -= CheckInstallationProgress;

                        if (addRequest.Status == StatusCode.Success)
                        {
                            s_Is2DSpriteEditorInstalled = true;
                            Debug.Log(string.Format(k_PackageInstallSuccessMessage, k_2DSpriteEditorPackageName));
                        }
                        else
                        {
                            Debug.LogError(string.Format(k_PackageInstallErrorMessage, k_2DSpriteEditorPackageName, addRequest.Error?.message));
                        }
                    }
                }
            }
        }

        static void OpenSpriteEditor(Object sprite)
        {
            if (sprite == null || !(sprite is Sprite))
                return;

            // Use reflection to call SpriteUtilityWindow.ShowSpriteEditorWindow
            var spriteUtilityWindowType = Type.GetType("UnityEditor.SpriteUtilityWindow, UnityEditor");
            if (spriteUtilityWindowType != null)
            {
                var showMethod = spriteUtilityWindowType.GetMethod("ShowSpriteEditorWindow",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (showMethod != null)
                {
                    showMethod.Invoke(null, new object[] { sprite });
                }
            }
        }
    }
}
