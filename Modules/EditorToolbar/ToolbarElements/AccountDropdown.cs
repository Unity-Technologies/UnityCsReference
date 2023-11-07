// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.UIElements;
using UnityEditor.Connect;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;
using System.Linq;
using System.Text.RegularExpressions;

namespace UnityEditor.Toolbars
{
    [EditorToolbarElement("Services/Account", typeof(DefaultMainToolbar))]
    sealed class AccountDropdown : EditorToolbarDropdown
    {
        readonly TextElement m_TextElement;
        readonly VisualElement m_ArrowElement;
        private readonly VisualElement m_AccountIconElement;
        bool m_LoggedIn;

        public AccountDropdown()
        {
            name = "AccountDropdown";
            text = L10n.Tr("Sign in");

            m_AccountIconElement = this.Q<Image>(className: EditorToolbar.elementIconClassName);
            m_AccountIconElement.AddToClassList("unity-icon-account");

            m_TextElement = this.Q<TextElement>(className: EditorToolbar.elementLabelClassName);
            m_TextElement.style.flexGrow = 1;
            m_TextElement.style.whiteSpace = WhiteSpace.NoWrap;
            m_TextElement.style.unityTextOverflowPosition = TextOverflowPosition.End;

            m_ArrowElement = this.Q(className: arrowClassName);

            clicked += () =>
            {
                if (m_LoggedIn)
                    ShowUserMenu(worldBound);
                else
                    UnityConnect.instance.ShowLogin();
            };

            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            EditorApplication.update += CheckAvailability;
            UnityConnect.instance.StateChanged += OnStateChange;
            OnStateChange(UnityConnect.instance.connectInfo);
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            EditorApplication.update -= CheckAvailability;
            UnityConnect.instance.StateChanged += OnStateChange;
        }

        void CheckAvailability()
        {
            style.display = MPE.ProcessService.level == MPE.ProcessLevel.Main ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void OnStateChange(ConnectInfo state)
        {
            if (state.ready)
                m_LoggedIn = !UnityConnect.instance.isDisableUserLogin && state.loggedIn;

            Refresh();
        }

        void Refresh()
        {
            m_AccountIconElement.style.display = m_LoggedIn ? DisplayStyle.Flex : DisplayStyle.None;
            m_AccountIconElement.style.visibility = m_LoggedIn ? Visibility.Visible : Visibility.Hidden;

            if (m_LoggedIn)
            {
                m_TextElement.text = GetUserInitials(UnityConnect.instance.userInfo.displayName);
                m_TextElement.style.maxWidth = 80;
                m_TextElement.style.textOverflow = TextOverflow.Ellipsis;
                m_TextElement.style.unityTextAlign = TextAnchor.MiddleLeft;
                m_ArrowElement.style.display = DisplayStyle.Flex;
                SetEnabled(true);
            }
            else
            {
                text = L10n.Tr("Sign in");
                m_TextElement.style.maxWidth = InitialStyle.maxWidth;
                m_TextElement.style.textOverflow = TextOverflow.Clip;
                m_TextElement.style.unityTextAlign = TextAnchor.MiddleCenter;
                m_ArrowElement.style.display = DisplayStyle.None;
                SetEnabled(UnityConnect.instance.connectInfo.online);
            }
        }

        void ShowUserMenu(Rect dropDownRect)
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

            if (!Application.HasProLicense())
            {
                menu.AddSeparator("");
                if (UnityConnect.instance.online)
                    menu.AddItem(EditorGUIUtility.TrTextContent("Upgrade your Unity plan"), false, () => Application.OpenURL("https://store.unity.com/"));
                else
                    menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Upgrade your Unity plan"));
            }

            menu.DropDown(dropDownRect, true);
        }

        internal static string GetUserInitials(string name)
        {
            if(string.IsNullOrEmpty(name))
                return string.Empty;

            var nameElements = Regex.Replace(name, @"/\s+/g", " ", RegexOptions.IgnoreCase).Trim().Split(' ');

            nameElements = nameElements.Where(element => !string.IsNullOrEmpty(element) &&
            Regex.IsMatch(element[0].ToString(), @"[A-Za-z\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF]")).ToArray() ?? new string[0];

            if (nameElements.Length > 1)
                return $"{ nameElements[0][0] }{ nameElements[nameElements.Length - 1][0] }".ToUpper();
            else if (nameElements.Length == 1)
                return $"{ nameElements[0][0] }".ToUpper();
            return string.Empty;
        }
    }
}
