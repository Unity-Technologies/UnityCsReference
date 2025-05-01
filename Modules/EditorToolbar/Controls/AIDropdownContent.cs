// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;
using Cursor = UnityEngine.UIElements.Cursor;

namespace UnityEditor.Toolbars;

class AIDropdownContent : PopupWindowContent
{
    internal record Link
    {
        public string id;
        public string url;
    }

    internal record Data
    {
        public string text;
        public List<Link> links;
        public List<string> packages;
        public string installButtonText;
        public string noInternet;
        public string installingPackages;
    }

    static bool IsNetworkReachable => Application.internetReachability != NetworkReachability.NotReachable;

    internal Data data = new()
    {
        text = "I have read and agree to the <link=terms><color=#7BAEFA>Unity AI Terms of Service</color></link> and the <link=supplemental><color=#7BAEFA>Generative AI Supplemental Privacy Notice.</color></link>"
            + "\n\nI acknowledge and understand Unity AI uses <link=thirdparty><color=#7BAEFA>these third-party services</color></link>.",
        links = new()
        {
            new() {id = "terms", url = "https://unity.com/legal/terms-of-service"},
            new() {id = "supplemental", url = "https://unity.com/legal/supplemental-privacy-statement-unity-muse"},
            new() {id = "thirdparty", url = "https://unity.com/legal/terms-of-service"}
        },
        noInternet = "You need an internet connection to be able to use the AI features.",
        installingPackages = "Installing packages",

        packages = new()
        {
            "com.unity.ai.generators",
            "com.unity.ai.assistant"
        },

        installButtonText = "Agree and install AI features"
    };

    Label m_Text;

    VisualElement m_NetworkUnreachableView;
    VisualElement m_LoadingView;
    VisualElement m_AgreementView;
    VisualElement m_CurrentView;
    VisualElement m_MainView;

    AddAndRemoveRequest m_Request;

    VisualElement CreateAgreementView()
    {
        var agreement = new VisualElement();

        m_Text = new Label(data.text)
        {
            enableRichText = true,
            style =
            {
                flexGrow = 1,
                flexWrap = Wrap.Wrap,
                whiteSpace = WhiteSpace.Normal
            }
        };

        m_Text.RegisterCallback<PointerDownLinkTagEvent>(TextLinkClick);
        m_Text.RegisterCallback<PointerOverLinkTagEvent>(_ => m_Text.style.cursor = new Cursor {defaultCursorId = 4});  // defaultCursorId maps to the UnityEditor.MouseCursor enum where 4 is the link cursor.
        m_Text.RegisterCallback<PointerOutLinkTagEvent>(_ => m_Text.style.cursor = new Cursor());

        var button = new Button(OnClicked)
        {
            name = "accept",
            text = data.installButtonText,
            style =
            {
                marginTop = 8,
                marginLeft = 0,
                marginRight = 0
            }
        };

        agreement.Add(m_Text);
        agreement.Add(button);

        return agreement;
    }

    VisualElement CreateLoadingView()
    {
        var loading = new VisualElement
        {
            style =
            {
                alignItems = Align.Center
            }
        };
        var loadingLabel = new Label(data.installingPackages)
        {
            enableRichText = true,
            style =
            {
                marginTop = 8,
                marginBottom = 8
            }
        };
        loading.Add(loadingLabel);
        return loading;
    }

    VisualElement CreateNetworkUnreachableView()
    {
        var network = new VisualElement
        {
            style =
            {
                marginTop = 8,
                marginBottom = 8,
                flexDirection = FlexDirection.Row,
                alignItems = Align.Center
            }
        };
        var warningIcon = new Image
        {
            image = EditorGUIUtility.GetHelpIcon(MessageType.Warning),
            style =
            {
                maxHeight = EditorGUI.kSingleLineHeight,
                flexShrink = 0,
                marginRight = 4
            }
        };
        var label = new Label(data.noInternet)
        {
            enableRichText = true,
            style =
            {
                flexGrow = 1,
                flexShrink = 1,
                whiteSpace = WhiteSpace.Normal
            }
        };
        network.Add(warningIcon);
        network.Add(label);
        return network;
    }

    void EnsureInit()
    {
        if (m_MainView != null)
            return;

        if (!EditorGUIUtility.isProSkin)
            data.text = data.text.Replace("#7BAEFA", "#0479D9");

        m_MainView = new()
        {
            style =
            {
                maxWidth = 300,
                marginTop = 8,
                marginBottom = 8,
                marginLeft = 8,
                marginRight = 8
            }
        };

        m_AgreementView = CreateAgreementView();
        m_LoadingView = CreateLoadingView();
        m_NetworkUnreachableView = CreateNetworkUnreachableView();
    }

    void Refresh()
    {
        EnsureInit();
        CheckIsPackageInstalling();

        VisualElement currentView;
        if (IsNetworkReachable)
        {
            if (m_Request != null)
                currentView = m_LoadingView;
            else
                currentView = m_AgreementView;
        }
        else
        {
            currentView = m_NetworkUnreachableView;
        }

        if (currentView != m_CurrentView)
        {
            m_CurrentView = currentView;
            m_MainView.Clear();
            m_MainView.Add(m_CurrentView);
        }
    }

    public override VisualElement CreateGUI()
    {
        Refresh();
        return m_MainView;
    }

    void TextLinkClick(PointerDownLinkTagEvent evt)
    {
        var link = data.links.Find(link => link.id == evt.linkID);
        if (link != null)
            Application.OpenURL(link.url);
    }

    void OnClicked()
    {
        if (Unsupported.IsDeveloperMode())
            Debug.Log("This button is currently a placeholder. It’s visible only in developer mode and does not perform any action because the associated packages have not yet been published. This behavior is expected for now.");
        else
            Debug.Log($"Installing AI Packages.\n{string.Join("\n", data.packages)}");

        AIDropdownConfig.instance.termsAccepted = true;

        editorWindow?.Close();
        if (data.packages.Count > 0)
            m_Request = Client.AddAndRemove(data.packages.ToArray());
    }

    void CheckIsPackageInstalling()
    {
        if (m_Request != null && m_Request.Status != StatusCode.InProgress)
            m_Request = null;
    }
}
