// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEditor.AssetPackage;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.UI.Internal;
using TreeViewState = UnityEditor.IMGUI.Controls.TreeViewState<int>;

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
            public GUIStyle trustInfoOuterBg      = "PI TrustInfoOuterBg";
            public Color     lineColor;
            public GUIContent verifiedIcon;
            public GUIContent errorIcon;
            public GUIContent infoIcon;
            public GUIContent warnIcon;

            public Constants()
            {
                lineColor = EditorGUIUtility.isProSkin ? new Color(0.1f, 0.1f, 0.1f) : new Color(0.4f, 0.4f, 0.4f);
                title.clipping = TextClipping.Ellipsis;

                verifiedIcon   = EditorGUIUtility.IconContent("Verified");
                errorIcon      = EditorGUIUtility.IconContent("console.erroricon");
                infoIcon       = EditorGUIUtility.IconContent("console.infoicon");
                warnIcon       = EditorGUIUtility.IconContent("console.warnicon");
            }
        }
        static Constants ms_Constants;

        [NativeHeader("Modules/AssetPackageEditor/AssetPackage.bindings.h")]
        [FreeFunction("Marshalling::GetImportPackageItems")]
        private static extern ImportPackageItem[] GetImportPackageItems(IntPtr nativeItems);

        [NativeHeader("Modules/AssetPackageEditor/AssetPackage.bindings.h")]
        [FreeFunction("Marshalling::GetAssetPackageInfo")]
        internal static extern AssetPackageInfo GetAssetPackageInfo(IntPtr nativeAssetPackageInfo);

        // Invoked from menu
        [UsedByNativeCode]
        public unsafe static void ShowImportPackage(string packagePath, IntPtr nativeItems, string packageIconPath, int productId, string packageName, string packageVersion, int uploadId, string packageExtractedPath, IntPtr nativeAssetPackageInfo)
        {
            var items = GetImportPackageItems(nativeItems);
            if (!ValidateInput(items))
                return;

            var origin = new AssetOrigin(productId, packageName, packageVersion, uploadId);
            var assetPackageInfo = GetAssetPackageInfo(nativeAssetPackageInfo);
            PackageImportWizard.instance.StartImport(packagePath, items, packageIconPath, origin, packageExtractedPath, assetPackageInfo);
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

            m_PackageName = ObjectNames.NicifyVariableName(System.IO.Path.GetFileNameWithoutExtension(packagePath));
            m_PackageIconPath = packageIconPath;

            Repaint();
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

            if (PackageImportWizard.AnyChangedAssets(m_ImportPackageItems))
            {
                TopAssetRestrictedArea();
                TopArea();
                TrustInfoArea();
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

        void TopAssetRestrictedArea()
        {
            if (m_ImportPackageItems == null)
                return;
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var restrictedItems = m_ImportPackageItems.Where(i => i.isRestricted).ToArray();
#pragma warning restore UA2001
            if (restrictedItems.Length > 0)
            {
                GUILayout.BeginHorizontal(EditorStyles.helpBox); // Horizontal layout for icon and text
                {
                    GUILayout.Label(ms_Constants.errorIcon, GUILayout.Width(32), GUILayout.Height(32));

                    GUILayout.BeginVertical();
                    {
                        GUILayout.Label("Restricted assets were found in this package.", EditorStyles.boldLabel);
                        
                        string text =
                            "These assets can not be imported,\n" +
                            "as they do not comply with Unity’s guidelines,\n" +
                            "which are designed to protect your project.\n" +
                            "The provider must update them to restore normal usage.\n" +
                            "As a result, some features may not work as expected.\n" +
                            "Contact the package provider for support.";

                        GUILayout.Label(text, EditorStyles.label);
                    }
                    GUILayout.EndVertical();
                }
                GUILayout.EndHorizontal();
            }
        }
        static TrustAndSignature GetEffectiveTrustAndSignature()
        {
            var wizard = PackageImportWizard.instance;
            var assetPackageInfo = wizard.assetPackageInfo;
            var trustAndSignature =  assetPackageInfo != null
                    ? TrustAndSignatureHelper.GetTrustAndSignature(assetPackageInfo)
                    : TrustAndSignature.NotApplicable;

            if (trustAndSignature == TrustAndSignature.UntrustedNoSignature && (PackageTrustLevel.HasBypassPackageTrustEntitlement() || wizard.isAssetStorePackage))
                return TrustAndSignature.NotApplicable;

            return trustAndSignature;
        }

        static void DrawTrustBadge(Rect rect, TrustAndSignature trustAndSignature)
        {
            var attestation = PackageImportWizard.instance.assetPackageInfo?.signature?.attestation;
            var signerName = string.IsNullOrEmpty(attestation?.publisherName) ? attestation?.ownerOrgName : attestation.publisherName;
            if (string.IsNullOrEmpty(signerName))
                signerName = L10n.Tr("Unknown Publisher");
            GUIContent icon;
            string label;
            switch (trustAndSignature)
            {
                case TrustAndSignature.FullTrustUnitySignature:
                    icon  = ms_Constants.verifiedIcon;
                    label = L10n.Tr("Signed for Unity Technologies");
                    break;
                case TrustAndSignature.FullTrustValidSignature:
                    icon  = ms_Constants.verifiedIcon;
                    label = string.Format(L10n.Tr("Signed for {0}"), signerName);
                    break;
                case TrustAndSignature.UntrustedInvalidSignature:
                    icon  = ms_Constants.errorIcon;
                    label = L10n.Tr("Invalid Signature");
                    break;
                case TrustAndSignature.LimitedTrust:
                    icon  = ms_Constants.infoIcon;
                    label = string.Format(L10n.Tr("Signed for {0}"), signerName);
                    break;
                case TrustAndSignature.UntrustedNoSignature:
                    icon  = ms_Constants.warnIcon;
                    label = L10n.Tr("Missing Signature");
                    break;
                case TrustAndSignature.FullTrustBuiltInPackage:
                case TrustAndSignature.FullTrustNoSignature:
                case TrustAndSignature.NotApplicable:
                default:
                    return;
            }

            GUI.Label(rect, new GUIContent(label, icon.image), ms_Constants.subtitle);
        }

        void TrustInfoArea()
        {
            var trustAndSignature = GetEffectiveTrustAndSignature();

            GUIContent icon;
            string message;

            switch (trustAndSignature)
            {
                case TrustAndSignature.UntrustedInvalidSignature:
                    icon    = ms_Constants.errorIcon;
                    message = L10n.Tr("This package has an invalid signature which can indicate unsafe or malicious content. Remove this package to reduce risk to your project.");
                    break;
                case TrustAndSignature.LimitedTrust:
                    icon    = ms_Constants.infoIcon;
                    message = L10n.Tr("This package is signed and distributed outside of Unity trusted sources. Please ensure you understand where this package originated from.");
                    break;
                case TrustAndSignature.UntrustedNoSignature:
                    icon    = ms_Constants.warnIcon;
                    message = L10n.Tr("Unity can't verify this package because it doesn't have a signature. Use signed packages to reduce risk to your project.");
                    break;
                default:
                    return;
            }

            GUILayout.BeginVertical(ms_Constants.trustInfoOuterBg);
            {
                GUILayout.Space(10);
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Space(10);
                    GUILayout.BeginHorizontal(EditorStyles.helpBox);
                    {
                        GUILayout.Label(icon, GUILayout.Width(32), GUILayout.Height(32));
                        GUILayout.Space(4);
                        GUILayout.Label(message, "WordWrappedLabel");
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.Space(10);
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(10);
            }
            GUILayout.EndVertical();
        }

        void TopArea()
        {
            const float margin = 10f;
            const float rightPadding = 10f;
            const float imageSize = 64f;
            const float rowHeight = imageSize / 4;

            if (s_PackageIcon is null && !string.IsNullOrEmpty(m_PackageIconPath))
                LoadTexture(m_PackageIconPath, ref s_PackageIcon);

            var hasPackageIcon = s_PackageIcon is not null;
            var trustAndSignature = GetEffectiveTrustAndSignature();
            var showTrustBadge = trustAndSignature != TrustAndSignature.NotApplicable && trustAndSignature != TrustAndSignature.FullTrustNoSignature;
            var packageImportWizard = PackageImportWizard.instance;

            var textAreaHeight = rowHeight;
            if (showTrustBadge)
                textAreaHeight += rowHeight;
            // We use 4 * rowHeight because when there are multi-step wizard information it always takes up 4 rows even if
            // there are no trust badge (the second row would just be empty in this case)
            if (packageImportWizard.IsMultiStepWizard)
                textAreaHeight = 4 * rowHeight;

            // We want the topArea to be at least 2 * rowHeight so it wouldn't show up too small in 1 row case
            var totalHeightWithoutMargin = hasPackageIcon ? imageSize : Math.Max(textAreaHeight, rowHeight * 2);
            var r = GUILayoutUtility.GetRect(position.width, totalHeightWithoutMargin + margin * 2);

            // Background
            GUI.Label(r, GUIContent.none, ms_Constants.topBarBg);

            // The padding is added to keep the text centered
            var textAreaTopPadding = (totalHeightWithoutMargin - textAreaHeight) * 0.5f;
            float textAreaX, textAreaY, textContentWidth;
            if (hasPackageIcon)
            {
                var iconRect = new Rect(r.x + margin, r.y + margin, imageSize, imageSize);
                DrawTexture(iconRect, s_PackageIcon, true);

                textAreaX = iconRect.xMax + margin;
                textAreaY = iconRect.yMin + textAreaTopPadding;
                textContentWidth = r.width - rightPadding - iconRect.width;
            }
            else
            {
                textAreaX = r.x + margin;
                textAreaY = r.y + margin + textAreaTopPadding;
                textContentWidth = r.width - rightPadding;
            }

            var titleRect = new Rect(textAreaX, textAreaY, textContentWidth, rowHeight);
            GUI.Label(titleRect, new GUIContent(m_PackageName, m_PackageName), ms_Constants.title);

            if (showTrustBadge)
            {
                var trustRect = new Rect(textAreaX, textAreaY + rowHeight, textContentWidth, rowHeight);
                DrawTrustBadge(trustRect, trustAndSignature);
            }

            if (packageImportWizard.IsMultiStepWizard)
            {
                var subtitleRect = new Rect(textAreaX + 1f, textAreaY + rowHeight * 2, textContentWidth, rowHeight);
                var subtitleText = packageImportWizard.IsProjectSettingStep ? "Import Settings Overrides" : "Import Content";
                GUI.Label(subtitleRect, EditorGUIUtility.TrTextContent(subtitleText), ms_Constants.subtitle);

                var stepInfoRect = new Rect(textAreaX, textAreaY + rowHeight * 3, textContentWidth, rowHeight);
                var stepInfoText = packageImportWizard.IsProjectSettingStep ? "Step 2 of 2" : "Step 1 of 2";
                GUI.Label(stepInfoRect, EditorGUIUtility.TrTextContent(stepInfoText), ms_Constants.stepInfo);
            }
        }

        void TopButtonsArea()
        {
            GUILayout.BeginVertical();
            GUILayout.Space(8);
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);

            GUI.enabled = !m_Tree.isAllItemsEnabled;
            if (GUILayout.Button(EditorGUIUtility.TrTextContent("All"), GUILayout.Width(50)))
            {
                m_Tree.SetAllEnabled(PackageImportTreeView.EnabledState.All);
            }
            GUI.enabled = true;

            GUI.enabled = m_Tree.isAnyItemEnabled;
            if (GUILayout.Button(EditorGUIUtility.TrTextContent("None"), GUILayout.Width(50)))
            {
                m_Tree.SetAllEnabled(PackageImportTreeView.EnabledState.None);
            }
            GUI.enabled = true;

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

            var packageImportWizard = PackageImportWizard.instance;
            var trustAndSignature = GetEffectiveTrustAndSignature();
            var hasTrustIssue = trustAndSignature is TrustAndSignature.LimitedTrust or TrustAndSignature.UntrustedInvalidSignature or TrustAndSignature.UntrustedNoSignature;
            if (hasTrustIssue)
            {
                if (GUILayout.Button(EditorGUIUtility.TrTextContent("Learn More"), EditorStyles.linkLabel))
                    Application.OpenURL("https://docs.unity3d.com/Manual/upm-signature.html");
                EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(EditorGUIUtility.TrTextContent("Cancel")))
            {
                packageImportWizard.CancelImport();
            }

            var hasPreviousStep = packageImportWizard.IsMultiStepWizard && packageImportWizard.IsProjectSettingStep;
            var hasNextStep = packageImportWizard.IsMultiStepWizard && !packageImportWizard.IsProjectSettingStep;
            var anyElementsSelected = packageImportWizard.AreAnyElementsSelected();
            if (hasPreviousStep && GUILayout.Button(EditorGUIUtility.TrTextContent("Back")))
            {
                packageImportWizard.DoPreviousStep(m_ImportPackageItems);
            }
            if (hasNextStep && GUILayout.Button(EditorGUIUtility.TrTextContent("Next")))
            {
                packageImportWizard.DoNextStep(m_ImportPackageItems);
            }
            GUI.enabled = anyElementsSelected;
            if (!hasNextStep && GUILayout.Button(hasTrustIssue ? EditorGUIUtility.TrTextContent("Import Anyway") : EditorGUIUtility.TrTextContent("Import")))
            {
                packageImportWizard.DoImportStep(m_ImportPackageItems);
            }
            GUI.enabled = true;

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
        private string m_PackageExtractedPath;
        private AssetPackageInfo m_AssetPackageInfo;
        public AssetPackageInfo assetPackageInfo => m_AssetPackageInfo;
        public bool isAssetStorePackage => m_AssetOrigin.IsValid();

        public void StartImport(string packagePath, ImportPackageItem[] items, string packageIconPath, AssetOrigin origin, string packageExtractedPath, AssetPackageInfo assetPackageInfo)
        {
            ClearImportData();

            m_PackagePath = packagePath;
            m_PackageIconPath = packageIconPath;
            m_PackageName = System.IO.Path.GetFileNameWithoutExtension(packagePath);
            m_AssetOrigin = origin;
            m_PackageExtractedPath = packageExtractedPath;
            m_AssetPackageInfo = assetPackageInfo;

            m_InitialImportItems = items;
            foreach (var item in items)
            {
                // We don't want to add `ProjectVersion.txt` since it would override the project Editor version and if it's a lower version, it would be downgraded
                if (item.destinationAssetPath == "ProjectSettings/ProjectVersion.txt")
                    continue;

                if (item.destinationAssetPath.StartsWith("ProjectSettings/"))
                    m_ProjectSettingItems.Add(item);
                else
                    m_AssetContentItems.Add(item);
            }

            var anyChangedAssets = AnyChangedAssets(m_AssetContentItems);
            var anyChangedProjectSettings = AnyChangedAssets(m_ProjectSettingItems);
            m_IsMultiStepWizard = anyChangedAssets && anyChangedProjectSettings;
            if (!anyChangedAssets && m_ProjectSettingItems.Count > 0)
            {
                m_IsProjectSettingStep = true;
                ShowImportWindow(m_ProjectSettingItems.ToArray());
            }
            else
            {
                ShowImportWindow(m_AssetContentItems.ToArray());
            }
        }

        public void DoNextStep(ImportPackageItem[] importPackageItems)
        {
            if (!IsMultiStepWizard && IsProjectSettingStep)
                return;

            m_AssetContentItems = new List<ImportPackageItem>(importPackageItems);
            m_IsProjectSettingStep = true;
            ShowImportWindow(m_ProjectSettingItems.ToArray());
        }

        public void DoImportStep(ImportPackageItem[] importPackageItems)
        {
            if (IsMultiStepWizard && !IsProjectSettingStep)
                return;

            m_ProjectSettingItems = new List<ImportPackageItem>(importPackageItems);
            FinishImport();
        }

        public void DoPreviousStep(ImportPackageItem[] importPackageItems)
        {
            if (!IsProjectSettingStep || !IsMultiStepWizard)
                return;

            m_ProjectSettingItems = new List<ImportPackageItem>(importPackageItems);
            m_IsProjectSettingStep = false;
            ShowImportWindow(m_AssetContentItems.ToArray());
        }

        public void CancelImport()
        {
            Utility.ImportPackageAssetsCancelledFromGUI(m_PackageName, m_InitialImportItems);
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
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            Utility.ImportPackageAssetsWithOrigin(m_AssetOrigin, m_AssetContentItems.Concat(m_ProjectSettingItems).ToArray(), m_PackageExtractedPath, true);
#pragma warning restore UA2001
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
            m_AssetPackageInfo = null;
        }

        public bool AreAnyElementsSelected()
        {
            return m_AssetContentItems.Exists(i => i.enabledStatus == 1) || m_ProjectSettingItems.Exists(i => i.enabledStatus == 1);
        }

        public static bool AnyChangedAssets(IReadOnlyCollection<ImportPackageItem> items)
        {
            if (items == null || items.Count == 0)
                return false;
            foreach (var item in items)
                if (!item.isFolder && item.assetChanged)
                    return true;
            return false;
        }
    }
}
