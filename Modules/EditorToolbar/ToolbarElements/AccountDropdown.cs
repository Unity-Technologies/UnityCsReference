// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Connect;
using System;
using UnityEngine;
using System.Linq;
using System.Text.RegularExpressions;

namespace UnityEditor.Toolbars
{
    static class AccountDropdown
    {
        static bool s_Available;
        static bool s_LoggedIn;
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
                string text = s_LoggedIn ? GetUserInitials(UnityConnect.instance.userInfo.displayName) : L10n.Tr("Sign in");
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

            var name = $"{L10n.Tr("Sign out")} {UnityConnect.instance.userInfo.displayName}";
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

            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            nameElements = nameElements.Where(element => !string.IsNullOrEmpty(element) &&
#pragma warning restore RS0030
            Regex.IsMatch(element[0].ToString(), @"[A-Za-z\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF]")).ToArray() ?? new string[0];

            if (nameElements.Length > 1)
                return $"{nameElements[0][0]}{nameElements[nameElements.Length - 1][0]}".ToUpper();
            else if (nameElements.Length == 1)
                return $"{nameElements[0][0]}".ToUpper();
            return string.Empty;
        }
    }
}
