// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEditor.Experimental;
using UnityEditor.Licensing;
using UnityEditorInternal;
using UnityEngine.UIElements;
using Unity.Scripting.LifecycleManagement;

namespace UnityEditor
{
    internal partial class AboutWindow : EditorWindow
    {
        // s_Instance is nulled when the window is closed (OnDestroy)
        [AutoStaticsCleanupOnCodeReload]
        static AboutWindow s_Instance;

        [RequiredByNativeCode]
        internal static void ShowAboutWindow()
        {
            // UUM-92333 HACK: In order for position to be correct, we need to close the existing instance
            // Otherwise, the new instance will open with the wrong sizing.
            s_Instance?.Close();

            var mainWindowRect = EditorGUIUtility.GetMainWindowPosition();
            var aboutRect = EditorGUIUtility.GetCenteredWindowPosition(mainWindowRect, new Vector2(573, 545));

            // UUM-92333 HACK: Clear any stored window position so that we always open in the center of the main window
            // If not, we run the risk of opening the about window at the wrong size if the main window was moved or resized between monitor setups
            var key = typeof(AboutWindow).ToString();

            EditorPrefs.DeleteKey(key + "x");
            EditorPrefs.DeleteKey(key + "y");
            EditorPrefs.DeleteKey(key + "w");
            EditorPrefs.DeleteKey(key + "h");
            EditorPrefs.DeleteKey(key + "z");

            AboutWindow w = GetWindow<AboutWindow>(utility: true, title: string.Empty);
            w.position = aboutRect;
            w.minSize = w.maxSize = w.position.size;

            s_Instance = w;
        }

        bool m_ShowDetailedVersion = false;
        private int m_InternalCodeProgress;


        private VisualElement buildDetailsContainer;
        private Label versionLabel;
        private const string darkClassname = "dark";

        void CreateGUI()
        {
            rootVisualElement.AddToClassList("root-element");


            var layout = EditorResources.Load("UXML/About/AboutWindow.uxml", typeof(UnityEngine.Object)) as VisualTreeAsset;

            string extensionVersion = FormatExtensionVersionString();
            int t = InternalEditorUtility.GetUnityVersionDate();
            DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            string branch = InternalEditorUtility.GetUnityBuildBranch();


            if (layout == null)
            {
                // We display a minimal layout just in case
                rootVisualElement.Add(new Label("About Unity"));
                versionLabel = new Label($"{InternalEditorUtility.GetUnityDisplayVersion()}{extensionVersion}");
                rootVisualElement.Add(versionLabel);
            }
            else
            {
                layout.CloneTree(rootVisualElement);

                versionLabel = rootVisualElement.Q<Label>("version");

                buildDetailsContainer = rootVisualElement.Q("detail-info");
                buildDetailsContainer.AddToClassList("hide-details");

                if (EditorGUIUtility.isProSkin)
                {
                    rootVisualElement.AddToClassList(darkClassname);
                    rootVisualElement.Query<VisualElement>(className: "logo")
                        .ForEach((x) => x.AddToClassList(darkClassname));
                }

                SetLabelValue("product-name", InternalEditorUtility.GetUnityProductName());

                SetLabelValue("build-revision", $"{branch} {InternalEditorUtility.GetUnityBuildHash()}");
                SetLabelValue("build-date", $"{dt.AddSeconds(t):r}");


                SetLabelValue("license-type", GetLicenseTypeString());
                SetLabelValue("serial-number", LicensingUtility.GetEditorLicenseIdentifier(true));
                SetLabelValue("unity-copyright", InternalEditorUtility.GetUnityCopyright());
            }

            rootVisualElement.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);

            var contextMenu = new ContextualMenuManipulator((evt) =>
            {
                evt.menu.AppendAction("Copy Version Info", (act) => CopyVersionInfoToClipboard());
                evt.menu.AppendAction("Copy License Info", (act) => CopyLicenseInfoToClipboard());
            });

            rootVisualElement.AddManipulator(contextMenu);
            rootVisualElement.focusable = true;

            UpdateVersionLabel();

            rootVisualElement.Focus();
        }

        void SetLabelValue(string labelName, string text)
        {
            var lbl = rootVisualElement.Q<TextElement>(labelName);
            if(lbl != null)
                lbl.text = text.Replace("(c)", "\u00A9");
        }

        void OnKeyDown(KeyDownEvent evt)
        {
            bool altPressed = ((int)evt.modifiers & (int)EventModifiers.Alt) == (int)EventModifiers.Alt;

            UpdateOnAlt(altPressed);

            ListenForSecretCodes(evt.character);

            if (evt.keyCode == KeyCode.C)
            {
                if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX)
                {
                    if (evt.modifiers == EventModifiers.Command)
                    {
                        CopyVersionInfoToClipboard();
                    }
                }
                else if (evt.modifiers == EventModifiers.Control)
                {
                    CopyVersionInfoToClipboard();
                }
            }
        }

        void OnEnable()
        {
            EditorApplication.modifierKeysChanged += ModifierKeysChanged;
        }

        void OnDisable()
        {
            EditorApplication.modifierKeysChanged -= ModifierKeysChanged;
        }

        void ModifierKeysChanged()
        {
            // because we show the detailed version string when Option (Alt) is pressed
            Repaint();
        }

        public void OnGUI()
        {
            var evt = Event.current;
            UpdateOnAlt(evt.alt);
        }

        private void UpdateOnAlt(bool altPressed)
        {
            if (!m_ShowDetailedVersion && altPressed != m_ShowDetailedVersion)
            {
                m_ShowDetailedVersion |= altPressed;

                UpdateVersionLabel();
            }
        }

        private void CopyVersionInfoToClipboard()
        {
            string extensionVersion = FormatExtensionVersionString();

            if (m_ShowDetailedVersion)
            {
                int t = InternalEditorUtility.GetUnityVersionDate();
                DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, 0);
                string branch = InternalEditorUtility.GetUnityBuildBranch();
                Clipboard.stringValue = $"{InternalEditorUtility.GetUnityProductName()}\n" +
                                        $"{InternalEditorUtility.GetUnityDisplayVersionVerbose()}{extensionVersion}\n" +
                                        $"Revision: {branch} {InternalEditorUtility.GetUnityBuildHash()}\n" +
                                        $"Built: {dt.AddSeconds(t):r}";
            }
            else
            {
                Clipboard.stringValue = $"{InternalEditorUtility.GetUnityProductName()}\n" +
                                        $"{InternalEditorUtility.GetUnityDisplayVersion()}{extensionVersion}";
            }
        }

        private void CopyLicenseInfoToClipboard()
        {
            Clipboard.stringValue = $"License type: {GetLicenseTypeString()}\n" +
                                    $"Serial number: {LicensingUtility.GetEditorLicenseIdentifier(true)}";
        }

        static string GetLicenseTypeString()
        {
            // BuildForNintendo3DS is never read, but is queried to keep the request identical to the native one
            var entitlements = new[]
            {
                CommonEntitlements.UseEditorUI,
                CommonEntitlements.DisableSplashScreen,
                CommonEntitlements.UseLegacyProFlag,
                CommonEntitlements.UseLegacyEmbeddedFlag,
                CommonEntitlements.UseTrialWatermark,
                CommonEntitlements.BuildForiOS,
                CommonEntitlements.BuildForiOSPro,
                CommonEntitlements.BuildForAndroid,
                CommonEntitlements.BuildForAndroidPro,
                CommonEntitlements.BuildForUWP,
                CommonEntitlements.BuildForWinRTPro,
                CommonEntitlements.BuildForNintendo3DS,
                CommonEntitlements.UsePrototypingWatermark,
                CommonEntitlements.UseEduWatermark,
            };

            var granted = new HashSet<string>(LicensingUtility.HasEntitlements(entitlements));

            string licenseType;
            if (granted.Contains(CommonEntitlements.UseLegacyProFlag))
                licenseType = "Unity Pro";
            else if (granted.Contains(CommonEntitlements.UseEditorUI) && !granted.Contains(CommonEntitlements.DisableSplashScreen))
                licenseType = "Unity Personal";
            else
                licenseType = "(Unlicensed)";

            if (granted.Contains(CommonEntitlements.UseLegacyEmbeddedFlag))
                licenseType = "Unity for Embedded Systems";

            if (granted.Contains(CommonEntitlements.UseTrialWatermark))
                licenseType += " (Trial)";

            if (granted.Contains(CommonEntitlements.BuildForiOS))
            {
                licenseType += ", iOS";
                if (granted.Contains(CommonEntitlements.BuildForiOSPro))
                    licenseType += " Pro";
            }

            if (granted.Contains(CommonEntitlements.BuildForAndroid))
            {
                licenseType += ", Android";
                if (granted.Contains(CommonEntitlements.BuildForAndroidPro))
                    licenseType += " Pro";
            }

            if (granted.Contains(CommonEntitlements.BuildForUWP))
            {
                licenseType += ", Windows Store";
                if (granted.Contains(CommonEntitlements.BuildForWinRTPro))
                    licenseType += " Pro";
            }

            if (granted.Contains(CommonEntitlements.UsePrototypingWatermark))
                licenseType += "\nNot for release";

            if (granted.Contains(CommonEntitlements.UseEduWatermark))
                licenseType += "\nFor educational use only";

            return licenseType;
        }

        void UpdateVersionLabel()
        {
            if (buildDetailsContainer != null)
            {
                buildDetailsContainer.EnableInClassList("hide-details", !m_ShowDetailedVersion);
                buildDetailsContainer.Query<VisualElement>()
                    .ForEach((x) => x.EnableInClassList("hide-details", !m_ShowDetailedVersion));
            }

            string extensionVersion = FormatExtensionVersionString();

            if (m_ShowDetailedVersion)
            {
                SetLabelValue("version", $"{InternalEditorUtility.GetUnityDisplayVersionVerbose()}{extensionVersion}");
            }
            else
            {
                // The non verbose version should be shorter on public builds
                SetLabelValue("version", $"{InternalEditorUtility.GetUnityDisplayVersion()}{extensionVersion}");
            }

        }

        private void ListenForSecretCodes(char current)
        {
            if (current == '\0')
                return;

            if (SecretCodeHasBeenTyped("internal", current, ref m_InternalCodeProgress))
            {
                ToggleInternalMode();
            }
        }

        private bool SecretCodeHasBeenTyped(string code, char current, ref int characterProgress)
        {
            if (characterProgress < 0 || characterProgress >= code.Length || code[characterProgress] != current)
                characterProgress = 0;

            // Don't use else here. Even if key was mismatch, it should still be recognized as first key of sequence if it matches.
            if (code[characterProgress] == current)
            {
                characterProgress++;

                if (characterProgress >= code.Length)
                {
                    characterProgress = 0;
                    return true;
                }
            }
            return false;
        }

        private void ToggleInternalMode()
        {
            bool enabled = !EditorPrefs.GetBool("DeveloperMode", false);
            EditorPrefs.SetBool("DeveloperMode", enabled);
            ShowNotification(new GUIContent(string.Format(L10n.Tr("Developer Mode {0}", null), (enabled ? L10n.Tr("On", null) : L10n.Tr("Off", null)))));
            EditorUtility.RequestScriptReload();

            // Repaint all views to show/hide debug repaint indicator
            InternalEditorUtility.RepaintAllViews();

            EditorApplication.DisplayRestartRequiredDialog(L10n.Tr("Developer Mode", null),
                L10n.Tr("Modules keep their current JIT optimization until you restart.", null));
        }

        private string FormatExtensionVersionString()
        {
            string extStr = EditorUserBuildSettings.selectedBuildTargetGroup.ToString();
            string ext = Modules.ModuleManager.GetExtensionVersion(extStr);

            if (!string.IsNullOrEmpty(ext))
                return " [" + extStr + ": " + ext + "]";

            return "";
        }
    }
}
