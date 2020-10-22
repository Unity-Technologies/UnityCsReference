// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.UIElements;
using UnityEditor.Connect;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Toolbars
{
    [EditorToolbarElement("Services/Account", typeof(DefaultMainToolbar))]
    sealed class AccountDropdown : ToolbarButton
    {
        public AccountDropdown()
        {
            name = "AccountDropdown";

            EditorToolbarUtility.MakeDropdown(this).text = L10n.Tr("Account");
            clicked += () => ShowUserMenu(worldBound);
            tooltip = L10n.Tr("Account profile");

            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            EditorApplication.update += CheckAvailability;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            EditorApplication.update -= CheckAvailability;
        }

        void CheckAvailability()
        {
            style.display = MPE.ProcessService.level == MPE.ProcessLevel.Main ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void ShowUserMenu(Rect dropDownRect)
        {
            var menu = new GenericMenu();
            if (!UnityConnect.instance.online || UnityConnect.instance.isDisableUserLogin)
            {
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Go to account"));
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Sign in..."));

                if (!Application.HasProLicense())
                {
                    menu.AddSeparator("");
                    menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Upgrade to Unity Plus or Pro"));
                }
            }
            else
            {
                string accountUrl = UnityConnect.instance.GetConfigurationURL(CloudConfigUrl.CloudPortal);
                if (UnityConnect.instance.loggedIn)
                    menu.AddItem(EditorGUIUtility.TrTextContent("Go to account"), false, () => UnityConnect.instance.OpenAuthorizedURLInWebBrowser(accountUrl));
                else
                    menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Go to account"));

                if (UnityConnect.instance.loggedIn)
                {
                    string name = "Sign out " + UnityConnect.instance.userInfo.displayName;
                    menu.AddItem(new GUIContent(name), false, () => { UnityConnect.instance.Logout(); });
                }
                else
                    menu.AddItem(EditorGUIUtility.TrTextContent("Sign in..."), false, () => { UnityConnect.instance.ShowLogin(); });

                if (!Application.HasProLicense())
                {
                    menu.AddSeparator("");
                    menu.AddItem(EditorGUIUtility.TrTextContent("Upgrade to Unity Plus or Pro"), false, () => Application.OpenURL("https://store.unity.com/"));
                }
            }

            menu.DropDown(dropDownRect, true);
        }
    }
}
