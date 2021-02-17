// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.UIElements;
using UnityEditor.Connect;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace UnityEditor.Toolbars
{
    [EditorToolbarElement("Services/Account", typeof(DefaultMainToolbar))]
    sealed class AccountDropdown : ToolbarButton
    {
        private readonly TextElement m_TextElement;
        private readonly VisualElement m_ArrowElement;
        private bool m_LoggedIn;

        public AccountDropdown()
        {
            name = "AccountDropdown";

            m_TextElement = new TextElement();
            m_TextElement.AddToClassList(EditorToolbar.elementLabelClassName);
            m_TextElement.text = L10n.Tr("Sign in");
            m_TextElement.style.flexGrow = 1;
            m_TextElement.style.whiteSpace = WhiteSpace.NoWrap;
            m_TextElement.style.unityTextOverflowPosition = TextOverflowPosition.End;

            m_ArrowElement = new VisualElement();
            m_ArrowElement.AddToClassList("unity-icon-arrow");

            Add(m_TextElement);
            Add(m_ArrowElement);

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
            if (m_LoggedIn)
            {
                m_TextElement.text = UnityConnect.instance.userInfo.displayName;
                m_TextElement.style.maxWidth = 80;
                m_TextElement.style.textOverflow = TextOverflow.Ellipsis;
                m_TextElement.style.unityTextAlign = TextAnchor.MiddleLeft;
                m_ArrowElement.style.display = DisplayStyle.Flex;
                SetEnabled(true);
            }
            else
            {
                m_TextElement.text = L10n.Tr("Sign in");
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

            var name = $"{L10n.Tr("Sign out")} {m_TextElement.text}";
            menu.AddItem(new GUIContent(name), false, () => UnityConnect.instance.Logout());

            if (!Application.HasProLicense())
            {
                menu.AddSeparator("");
                if (UnityConnect.instance.online)
                    menu.AddItem(EditorGUIUtility.TrTextContent("Upgrade to Unity Plus or Pro"), false, () => Application.OpenURL("https://store.unity.com/"));
                else
                    menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Upgrade to Unity Plus or Pro"));
            }

            menu.DropDown(dropDownRect, true);
        }

    }
}
