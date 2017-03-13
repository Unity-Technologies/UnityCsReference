// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;
using UnityEditor.IMGUI.Controls;

namespace UnityEditor
{
    internal class PackageImport : EditorWindow
    {
        [SerializeField]  private ImportPackageItem[] m_ImportPackageItems;
        [SerializeField]  private string              m_PackageName;
        [SerializeField]  private string              m_PackageIconPath;
        [SerializeField]  TreeViewState               m_TreeViewState;
        [NonSerialized]   PackageImportTreeView       m_Tree;

        private bool m_ShowReInstall;
        private bool m_ReInstallPackage;

        public bool                canReInstall { get { return m_ShowReInstall; } }
        public bool                doReInstall  { get { return m_ShowReInstall && m_ReInstallPackage; } }
        public ImportPackageItem[] packageItems { get { return m_ImportPackageItems; } }

        private static Texture2D s_PackageIcon;
        private static Texture2D s_Preview;
        private static string    s_LastPreviewPath;

        readonly static char[]   s_InvalidPathChars = System.IO.Path.GetInvalidPathChars();

        internal class Constants
        {
            public GUIStyle ConsoleEntryBackEven  = "CN EntryBackEven";
            public GUIStyle ConsoleEntryBackOdd   = "CN EntryBackOdd";
            public GUIStyle title                 = new GUIStyle(EditorStyles.largeLabel);
            public GUIStyle bottomBarBg           = "ProjectBrowserBottomBarBg";
            public GUIStyle topBarBg              = new GUIStyle("ProjectBrowserHeaderBgTop");
            public GUIStyle textureIconDropShadow = "ProjectBrowserTextureIconDropShadow";
            public Color    lineColor;

            public Constants()
            {
                lineColor = EditorGUIUtility.isProSkin ? new Color(0.1f, 0.1f, 0.1f) : new Color(0.4f, 0.4f, 0.4f);
                topBarBg.fixedHeight = 0;
                topBarBg.border.top = topBarBg.border.bottom = 2;

                title.fontStyle = FontStyle.Bold;
                title.alignment = TextAnchor.MiddleLeft;
            }
        }
        static Constants ms_Constants;


        // Invoked from menu
        public static void ShowImportPackage(string packagePath, ImportPackageItem[] items, string packageIconPath, bool allowReInstall)
        {
            if (!ValidateInput(items))
                return;

            var window = GetWindow<PackageImport>(true, "Import Unity Package");
            window.Init(packagePath, items, packageIconPath, allowReInstall);
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

        void Init(string packagePath, ImportPackageItem[] items, string packageIconPath, bool allowReInstall)
        {
            DestroyCreatedIcons();

            m_ShowReInstall      = allowReInstall;
            m_ReInstallPackage   = true;
            m_TreeViewState      = null;
            m_Tree               = null;
            m_ImportPackageItems = items;

            m_PackageName = System.IO.Path.GetFileNameWithoutExtension(packagePath);
            m_PackageIconPath = packageIconPath;

            Repaint();
        }

        private bool ShowTreeGUI(bool reInstalling, ImportPackageItem[] items)
        {
            if (reInstalling)
                return true;

            if (items.Length == 0)
                return false;

            for (int i = 0; i < items.Length; i++)
            {
                if (!items[i].isFolder && items[i].assetChanged)
                    return true;
            }

            return false;
        }

        public void OnGUI()
        {
            if (ms_Constants == null)
                ms_Constants = new Constants();

            if (m_TreeViewState == null)
                m_TreeViewState = new TreeViewState();

            if (m_Tree == null)
                m_Tree = new PackageImportTreeView(this, m_TreeViewState, new Rect());

            if (m_ImportPackageItems != null && ShowTreeGUI(doReInstall, m_ImportPackageItems))
            {
                TopArea();
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
                ReInstallToggle();
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

        void ReInstallToggle()
        {
            if (m_ShowReInstall)
            {
                EditorGUI.BeginChangeCheck();
                bool reInstall = GUILayout.Toggle(m_ReInstallPackage, "Re-Install Package");
                if (EditorGUI.EndChangeCheck())
                    m_ReInstallPackage = reInstall;
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
                titleRect = new Rect(iconRect.xMax + 10f, iconRect.yMin, r.width, iconRect.height);
            }
            else
            {
                titleRect = new Rect(r.x + 5f, r.yMin, r.width, r.height);
            }

            // Title
            GUI.Label(titleRect, m_PackageName, ms_Constants.title);
        }

        void BottomArea()
        {
            // Background
            GUILayout.BeginVertical(ms_Constants.bottomBarBg);

            GUILayout.Space(8);
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            if (GUILayout.Button(EditorGUIUtility.TextContent("All"), GUILayout.Width(50)))
            {
                m_Tree.SetAllEnabled(PackageImportTreeView.EnabledState.All);
            }

            if (GUILayout.Button(EditorGUIUtility.TextContent("None"), GUILayout.Width(50)))
            {
                m_Tree.SetAllEnabled(PackageImportTreeView.EnabledState.None);
            }

            ReInstallToggle();

            GUILayout.FlexibleSpace();
            if (GUILayout.Button(EditorGUIUtility.TextContent("Cancel")))
            {
                PopupWindowWithoutFocus.Hide();
                Close();
                GUIUtility.ExitGUI();
            }
            if (GUILayout.Button(EditorGUIUtility.TextContent("Import")))
            {
                bool doImport = true;
                if (doReInstall)
                    doImport = EditorUtility.DisplayDialog("Re-Install?", "Highlighted folders will be completely deleted first! Recommend backing up your project first. Are you sure?", "Do It", "Cancel");

                if (doImport)
                {
                    if (m_ImportPackageItems != null)
                        PackageUtility.ImportPackageAssets(m_PackageName, m_ImportPackageItems, doReInstall);

                    PopupWindowWithoutFocus.Hide();
                    Close();
                    GUIUtility.ExitGUI();
                }
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
}
