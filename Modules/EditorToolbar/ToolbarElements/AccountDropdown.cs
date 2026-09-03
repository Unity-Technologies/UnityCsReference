// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: SceneTooling not yet converted
using UnityEditor.Connect;
using System;
using UnityEngine;
using System.Linq;
using System.Text.RegularExpressions;
using Unity.Scripting.LifecycleManagement;

namespace UnityEditor.Toolbars
{
    static class AccountDropdown
    {
        [NoAutoStaticsCleanup] // Recomputed from MPE.ProcessService.level on every EditorApplication.update tick; safe to persist.
        static bool s_Available;
        [NoAutoStaticsCleanup] // Re-derived from UnityConnect connect state via the StateChanged callback; safe to persist.
        static bool s_LoggedIn;
        [NoAutoStaticsCleanup] // Editor icon loaded by fixed name (EditorGUIUtility.LoadIcon); the asset survives domain reload.
        static Texture2D s_AccountIcon;

        [UnityOnlyMainToolbarPreset]
        [MainToolbarElement("Services/Account", defaultDockIndex = 0, defaultDockPosition = MainToolbarDockPosition.Left)]
        static MainToolbarElement QueryElementInfo()
        {
            MainToolbarElement info;
            if (s_LoggedIn)
            {
                var textContent = GetUserInitials(UnityConnect.instance.userInfo.displayName);
                info = new MainToolbarDropdown(new MainToolbarContent(textContent, s_AccountIcon, String.Empty), ShowUserMenu);

            }
            else
            {
                string text = s_LoggedIn ? GetUserInitials(UnityConnect.instance.userInfo.displayName) : L10n.Tr("Sign in", null);
                info = new MainToolbarButton(new MainToolbarContent(text), UnityConnect.instance.ShowLogin);
            }
            info.displayed = s_Available;
            return info;
        }

        static AccountDropdown()
        {
            s_Available = MPE.ProcessService.level == MPE.ProcessLevel.Main;
            s_LoggedIn = false;
            s_AccountIcon = EditorGUIUtility.LoadIcon("Account");

            EditorApplication.delayCall += DelayInitialization;
        }

        static void DelayInitialization()
        {
            EditorApplication.update += CheckAvailability;
            UnityConnect.instance.StateChanged += OnStateChange;
            OnStateChange(UnityConnect.instance.connectInfo);
        }

        static void CheckAvailability()
        {
            var available = MPE.ProcessService.level == MPE.ProcessLevel.Main;
            if (s_Available != available)
                MainToolbar.Refresh("Services/Account");
            s_Available = available;
        }

        static void OnStateChange(ConnectInfo state)
        {
            if (state.ready)
                s_LoggedIn = !UnityConnect.instance.isDisableUserLogin && state.loggedIn;

            MainToolbar.Refresh("Services/Account");
        }

        static void ShowUserMenu(Rect dropDownRect)
        {
            var menu = new GenericMenu();
            if (UnityConnect.instance.online)
            {
                var accountUrl = UnityConnect.instance.GetConfigurationURL(CloudConfigUrl.CloudPortal);
                menu.AddItem(EditorGUIUtility.TrTextContent("My account"), false, () => UnityConnect.instance.OpenAuthorizedURLInWebBrowser(accountUrl));
            }
            else
            {
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent("My account"));
            }

            var name = $"{L10n.Tr("Sign out", null)} {UnityConnect.instance.userInfo.displayName}";
            menu.AddItem(new GUIContent(name), false, () => UnityConnect.instance.Logout());

            if (!UnityEngine.Application.HasProLicense())
            {
                menu.AddSeparator("");
                if (UnityConnect.instance.online)
                    menu.AddItem(EditorGUIUtility.TrTextContent("Upgrade your Unity plan"), false, () => UnityEngine.Application.OpenURL("https://store.unity.com/"));
                else
                    menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Upgrade your Unity plan"));
            }

            menu.DropDown(dropDownRect, true);
        }

        internal static string GetUserInitials(string name)
        {
            if (string.IsNullOrEmpty(name))
                return string.Empty;

            var nameElements = Regex.Replace(name, @"/\s+/g", " ", RegexOptions.IgnoreCase).Trim().Split(' ');

            #pragma warning disable UAC2001 // Avoid Linq
            nameElements = nameElements.Where(element => !string.IsNullOrEmpty(element) &&
#pragma warning restore UAC2001
            Regex.IsMatch(element[0].ToString(), @"[A-Za-z\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF]")).ToArray() ?? Array.Empty<string>();

            if (nameElements.Length > 1)
                return $"{nameElements[0][0]}{nameElements[nameElements.Length - 1][0]}".ToUpper();
            else if (nameElements.Length == 1)
                return $"{nameElements[0][0]}".ToUpper();
            return string.Empty;
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
