// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Scripting;

namespace UnityEditor
{
    [UsedByNativeCode]
    internal class PackageImport : EditorWindow
    {
        [SerializeField]  private ImportPackageItem[] m_ImportPackageItems;
        [SerializeField]  private string              m_PackageName;
        [SerializeField]  private string              m_PackageIconPath;
        [SerializeField]  TreeViewState               m_TreeViewState;
        [NonSerialized]   PackageImportTreeView       m_Tree;

        public ImportPackageItem[] packageItems { get { return m_ImportPackageItems; } }

        private static Texture2D s_PackageIcon;
        private static Texture2D s_Preview;
        private static string    s_LastPreviewPath;

        readonly static HashSet<char> s_InvalidPathChars = new HashSet<char>(System.IO.Path.GetInvalidPathChars());

        internal class Constants
        {
            public GUIStyle ConsoleEntryBackEven  = "CN EntryBackEven";
            public GUIStyle ConsoleEntryBackOdd   = "CN EntryBackOdd";
            public GUIStyle title                 = "LargeBoldLabel";
            public GUIStyle subtitle              = "BoldLabel";
            public GUIStyle stepInfo              = "Label";
            public GUIStyle bottomBarBg           = "ProjectBrowserBottomBarBg";
            public GUIStyle topBarBg              = "OT TopBar";
            public GUIStyle textureIconDropShadow = "ProjectBrowserTextureIconDropShadow";
            public Color    lineColor;

            public Constants()
            {
                lineColor = EditorGUIUtility.isProSkin ? new Color(0.1f, 0.1f, 0.1f) : new Color(0.4f, 0.4f, 0.4f);
            }
        }
        static Constants ms_Constants;

        // Invoked from menu
        [UsedByNativeCode]
        public static void ShowImportPackage(string packagePath, ImportPackageItem[] items, string packageIconPath, int productId, string packageName, string packageVersion, int uploadId)
        {
            if (!ValidateInput(items))
                return;

            var origin = new AssetOrigin(productId, packageName, packageVersion, uploadId);
            PackageImportWizard.instance.StartImport(packagePath, items, packageIconPath, origin);
        }

        public PackageImport()
        {
            minSize = new Vector2(350, 350);
        }

        void OnDisable()
        {
            DestroyCreatedIcons();
        }

        void DestroyCreatedIcons()
        {
            if (s_Preview != null)
            {
                DestroyImmediate(s_Preview);
                s_Preview = null;
                s_LastPreviewPath = null;
            }

            if (s_PackageIcon != null)
            {
                DestroyImmediate(s_PackageIcon);
                s_PackageIcon = null;
            }
        }

        internal void Init(string packagePath, ImportPackageItem[] items, string packageIconPath)
        {
            DestroyCreatedIcons();

            m_TreeViewState      = null;
            m_Tree               = null;
            m_ImportPackageItems = items;

            m_PackageName = System.IO.Path.GetFileNameWithoutExtension(packagePath);
            m_PackageIconPath = packageIconPath;

            Repaint();
        }

        private bool ShowTreeGUI(ImportPackageItem[] items)
        {
            if (items.Length == 0)
                return false;

            for (int i = 0; i < items.Length; i++)
            {
                if (!items[i].isFolder && items[i].assetChanged)
                    return true;
            }

            return false;
        }

        internal override void OnResized()
        {
            m_Tree?.OnWindowResized();
            base.OnResized();
        }

        public void OnGUI()
        {
            if (ms_Constants == null)
                ms_Constants = new Constants();

            if (m_TreeViewState == null)
                m_TreeViewState = new TreeViewState();

            if (m_Tree == null)
                m_Tree = new PackageImportTreeView(this, m_TreeViewState, new Rect());

            if (m_ImportPackageItems != null && ShowTreeGUI(m_ImportPackageItems))
            {
                TopArea();
                TopButtonsArea();
                m_Tree.OnGUI(GUILayoutUtility.GetRect(1, 9999, 1, 99999));
                BottomArea();
            }
            else
            {
                GUILayout.Label("Nothing to import!", EditorStyles.boldLabel);
                GUILayout.Label("All assets from this package are already in your project.", "WordWrappedLabel");
                GUILayout.FlexibleSpace();

                // Background
                GUILayout.BeginVertical(ms_Constants.bottomBarBg);
                GUILayout.Space(8);
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("OK"))
                {
                    Close();
                    GUIUtility.ExitGUI();
                }
                GUILayout.Space(10);
                GUILayout.EndHorizontal();
                GUILayout.Space(5);
                GUILayout.EndVertical();
            }
        }

        void TopArea()
        {
            const float margin = 10f;
            const float imageSize = 64;

            if (s_PackageIcon == null && !string.IsNullOrEmpty(m_PackageIconPath))
                LoadTexture(m_PackageIconPath, ref s_PackageIcon);
            bool hasPackageIcon = s_PackageIcon != null;

            float totalTopHeight = hasPackageIcon ? (margin + imageSize + margin) : 52f;
            Rect r = GUILayoutUtility.GetRect(position.width, totalTopHeight);

            // Background
            GUI.Label(r, GUIContent.none, ms_Constants.topBarBg);

            Rect titleRect;
            if (hasPackageIcon)
            {
                Rect iconRect = new Rect(r.x + margin, r.y + margin, imageSize, imageSize);
                DrawTexture(iconRect, s_PackageIcon, true);

                var textContentWidth = r.width - iconRect.width;
                var textContentX = iconRect.xMax + margin;
                if (!PackageImportWizard.instance.IsMultiStepWizard)
                    titleRect = new Rect(textContentX, iconRect.yMin, textContentWidth, iconRect.height);
                else
                {
                    titleRect = new Rect(textContentX, iconRect.yMin, textContentWidth, iconRect.height / 3);

                    // Subtitle
                    var subtitleRect = new Rect(textContentX + 1f, iconRect.yMin + iconRect.height * 0.50f, textContentWidth, iconRect.height / 4);
                    var subtitleText = PackageImportWizard.instance.IsProjectSettingStep ? "Import Settings Overrides" : "Import Content";
                    GUI.Label(subtitleRect, EditorGUIUtility.TrTextContent(subtitleText), ms_Constants.subtitle);

                    // "Step x of y" label
                    var stepInfoRect = new Rect(textContentX, iconRect.yMin + iconRect.height * 0.75f, textContentWidth, iconRect.height / 4);
                    var stepInfoText = PackageImportWizard.instance.IsProjectSettingStep ? "Step 2 of 2" : "Step 1 of 2";
                    GUI.Label(stepInfoRect, EditorGUIUtility.TrTextContent(stepInfoText), ms_Constants.stepInfo);
                }
            }
            else
            {
                titleRect = new Rect(r.x + 5f, r.yMin, r.width, r.height);
            }

            // Title
            GUI.Label(titleRect, m_PackageName, ms_Constants.title);
        }

        void TopButtonsArea()
        {
            GUILayout.BeginVertical();
            GUILayout.Space(8);
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);

            if (GUILayout.Button(EditorGUIUtility.TrTextContent("All"), GUILayout.Width(50)))
            {
                m_Tree.SetAllEnabled(PackageImportTreeView.EnabledState.All);
            }

            if (GUILayout.Button(EditorGUIUtility.TrTextContent("None"), GUILayout.Width(50)))
            {
                m_Tree.SetAllEnabled(PackageImportTreeView.EnabledState.None);
            }

            GUILayout.Space(10);
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
            GUILayout.EndVertical();
        }

        void BottomArea()
        {
            // Background
            GUILayout.BeginVertical(ms_Constants.bottomBarBg);
            GUILayout.Space(8);
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);

            GUILayout.FlexibleSpace();
            if (GUILayout.Button(EditorGUIUtility.TrTextContent("Cancel")))
            {
                PackageImportWizard.instance.CancelImport();
            }
            if (PackageImportWizard.instance.IsProjectSettingStep && GUILayout.Button(EditorGUIUtility.TrTextContent("Back")))
            {
                PackageImportWizard.instance.DoPreviousStep(m_ImportPackageItems);
            }
            var buttonText = PackageImportWizard.instance.IsMultiStepWizard
                && !PackageImportWizard.instance.IsProjectSettingStep ? "Next" : "Import";
            if (GUILayout.Button(EditorGUIUtility.TrTextContent(buttonText)))
            {
                if (m_ImportPackageItems != null)
                    PackageImportWizard.instance.DoNextStep(m_ImportPackageItems);
                else
                    PackageImportWizard.instance.CloseImportWindow();
            }

            GUILayout.Space(10);
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
            GUILayout.EndVertical();
        }

        // Reuses old texture if it's created
        static void LoadTexture(string filepath, ref Texture2D texture)
        {
            if (!texture)
                texture = new Texture2D(128, 128);

            byte[] fileContents = null;
            try
            {
                fileContents = System.IO.File.ReadAllBytes(filepath);
            }
            catch
            {
                // ignore
            }

            if (filepath == "" || fileContents == null || !texture.LoadImage(fileContents))
            {
                Color[] pixels = texture.GetPixels();
                for (int i = 0; i < pixels.Length; ++i)
                    pixels[i] = new Color(0.5f, 0.5f, 0.5f, 0f);
                texture.SetPixels(pixels);
                texture.Apply();
            }
        }

        public static void DrawTexture(Rect r, Texture2D tex, bool useDropshadow)
        {
            if (tex == null)
                return;

            // Clamp size (preserve aspect ratio)
            float texwidth = tex.width;
            float texheight = tex.height;
            if (texwidth >= texheight && texwidth > r.width)
            {
                texheight = texheight * r.width / texwidth;
                texwidth = r.width;
            }
            else if (texheight > texwidth && texheight > r.height)
            {
                texwidth = texwidth * r.height / texheight;
                texheight = r.height;
            }

            // Center
            float x = r.x + Mathf.Round((r.width - texwidth) / 2.0f);
            float y = r.y + Mathf.Round((r.height - texheight) / 2.0f);

            r = new Rect(x, y, texwidth, texheight);

            // Dropshadow
            if (useDropshadow && Event.current.type == EventType.Repaint)
            {
                Rect borderPosition = new RectOffset(1, 1, 1, 1).Remove(ms_Constants.textureIconDropShadow.border.Add(r));
                ms_Constants.textureIconDropShadow.Draw(borderPosition, GUIContent.none, false, false, false, false);
            }

            GUI.DrawTexture(r, tex, ScaleMode.ScaleToFit, true);
        }

        public static Texture2D GetPreview(string previewPath)
        {
            if (previewPath != s_LastPreviewPath)
            {
                s_LastPreviewPath = previewPath;
                LoadTexture(previewPath, ref s_Preview);
            }

            return s_Preview;
        }

        static bool ValidateInput(ImportPackageItem[] items)
        {
            string errorMessage;
            if (!IsAllFilePathsValid(items, out errorMessage))
            {
                errorMessage += "\nDo you want to import the valid file paths of the package or cancel importing?";
                return EditorUtility.DisplayDialog("Invalid file path found", errorMessage, "Import", "Cancel importing");
            }
            return true;
        }

        static bool IsAllFilePathsValid(ImportPackageItem[] assetItems, out string errorMessage)
        {
            foreach (var item in assetItems)
            {
                if (item.isFolder)
                    continue;

                char invalidChar;
                int invalidCharIndex;
                if (HasInvalidCharInFilePath(item.destinationAssetPath, out invalidChar, out invalidCharIndex))
                {
                    errorMessage = string.Format("Invalid character found in file path: '{0}'. Invalid ascii value: {1} (at character index {2}).", item.destinationAssetPath, (int)invalidChar, invalidCharIndex);
                    return false;
                }
            }

            errorMessage = "";
            return true;
        }

        static bool HasInvalidCharInFilePath(string filePath, out char invalidChar, out int invalidCharIndex)
        {
            for (int i = 0; i < filePath.Length; ++i)
            {
                char c = filePath[i];
                if (s_InvalidPathChars.Contains(c))
                {
                    invalidChar = c;
                    invalidCharIndex = i;
                    return true;
                }
            }

            invalidChar = ' ';
            invalidCharIndex = -1;
            return false;
        }

        public static bool HasInvalidCharInFilePath(string filePath)
        {
            char invalidChar;
            int invalidCharIndex;
            return HasInvalidCharInFilePath(filePath, out invalidChar, out invalidCharIndex);
        }
    }

    [Serializable]
    internal sealed class PackageImportWizard : ScriptableSingleton<PackageImportWizard>
    {
        [SerializeField]
        private PackageImport m_ImportWindow;

        [SerializeField]
        private string m_PackagePath;
        [SerializeField]
        private string m_PackageIconPath;
        [SerializeField]
        private string m_PackageName;

        [SerializeField]
        private ImportPackageItem[] m_InitialImportItems;
        [SerializeField]
        private List<ImportPackageItem> m_AssetContentItems;
        [SerializeField]
        private List<ImportPackageItem> m_ProjectSettingItems;

        [SerializeField]
        private bool m_IsMultiStepWizard;
        public bool IsMultiStepWizard => m_IsMultiStepWizard;
        [SerializeField]
        private bool m_IsProjectSettingStep;
        public bool IsProjectSettingStep => m_IsProjectSettingStep;

        private AssetOrigin m_AssetOrigin;

        public void StartImport(string packagePath, ImportPackageItem[] items, string packageIconPath, AssetOrigin origin)
        {
            ClearImportData();

            m_PackagePath = packagePath;
            m_PackageIconPath = packageIconPath;
            m_PackageName = System.IO.Path.GetFileNameWithoutExtension(packagePath);
            m_AssetOrigin = origin;

            m_InitialImportItems = items;
            foreach (var item in items)
            {
                if (item.destinationAssetPath.StartsWith("ProjectSettings/"))
                    m_ProjectSettingItems.Add(item);
                else
                    m_AssetContentItems.Add(item);
            }

            m_IsMultiStepWizard = m_ProjectSettingItems.Any();
            ShowImportWindow(m_AssetContentItems.ToArray());
        }

        public void DoNextStep(ImportPackageItem[] importPackageItems)
        {
            if (IsProjectSettingStep)
                m_ProjectSettingItems = new List<ImportPackageItem>(importPackageItems);
            else
                m_AssetContentItems = new List<ImportPackageItem>(importPackageItems);


            if (!IsMultiStepWizard || IsProjectSettingStep)
                FinishImport();
            else
            {
                m_IsProjectSettingStep = true;
                ShowImportWindow(m_ProjectSettingItems.ToArray());
            }
        }

        public void DoPreviousStep(ImportPackageItem[] importPackageItems)
        {
            if (IsProjectSettingStep)
            {
                m_ProjectSettingItems = new List<ImportPackageItem>(importPackageItems);
                m_IsProjectSettingStep = false;
                ShowImportWindow(m_AssetContentItems.ToArray());
            }
        }

        public void CancelImport()
        {
            PackageUtility.ImportPackageAssetsCancelledFromGUI(m_PackageName, m_InitialImportItems);
            CloseImportWindow();
        }

        public void CloseImportWindow()
        {
            if (m_ImportWindow != null)
            {
                ClearImportData();
                m_ImportWindow.Close();
                GUIUtility.ExitGUI();
            }
        }

        private void ShowImportWindow(ImportPackageItem[] items)
        {
            m_ImportWindow = PackageImport.GetWindow<PackageImport>(true, "Import Unity Package");
            m_ImportWindow.Init(m_PackagePath, items, m_PackageIconPath);
        }

        private void FinishImport()
        {
            var completeItemList = IsMultiStepWizard ? m_AssetContentItems.Concat(m_ProjectSettingItems) : m_AssetContentItems;
            PackageUtility.ImportPackageAssetsWithOrigin(m_AssetOrigin, completeItemList.ToArray());
            CloseImportWindow();
        }

        private void ClearImportData()
        {
            m_PackagePath = string.Empty;
            m_PackageIconPath = string.Empty;
            m_PackageName = string.Empty;

            m_InitialImportItems = null;
            m_AssetContentItems = new List<ImportPackageItem>();
            m_ProjectSettingItems = new List<ImportPackageItem>();

            m_IsMultiStepWizard = false;
            m_IsProjectSettingStep = false;
        }
    }
}
