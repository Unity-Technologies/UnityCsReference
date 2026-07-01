// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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
        text = "Add the Unity AI packages to your project. AI Assistant is an in-Editor assistant that helps you" +
            " write scripts, generate assets, and solve common Unity problems." +
            "\n\n<link=learnmore><color=#7BAEFA>Learn more.</color></link>",
        links = new()
        {
            new() {id = "learnmore", url = "https://unity.com/features/ai"}
        },
        noInternet = "You need an internet connection to be able to use the AI features.",
        installingPackages = "Installing packages",

        packages = new()
        {
            "com.unity.ai.assistant",
            "com.unity.ai.inference"
        },

        installButtonText = "Install Unity AI"
    };

    Label m_Text;

    VisualElement m_NetworkUnreachableView;
    VisualElement m_LoadingView;
    VisualElement m_InstallView;
    VisualElement m_CurrentView;
    VisualElement m_MainView;

    AddAndRemoveRequest m_Request;

    VisualElement CreateInstallView()
    {
        var install = new VisualElement();

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
            name = "install",
            text = data.installButtonText,
            style =
            {
                marginTop = 8,
                marginLeft = 0,
                marginRight = 0
            }
        };

        install.Add(m_Text);
        install.Add(button);

        return install;
    }

    VisualElement CreateLoadingView()
    {
        var loading = new VisualElement
        {
            style =
            {
                alignItems = Align.Center,
                justifyContent = Justify.Center,
                flexDirection = FlexDirection.Row,
            }
        };
        var loadingSpinner = new LoadingSpinner
        {
            style =
            {
                marginRight = 4
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
        loading.Add(loadingSpinner);
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

        m_InstallView = CreateInstallView();
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
                currentView = m_InstallView;
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
        EditorAIAssistantAnalytics.ReportAIInstallAcceptedEvent();

        Debug.Log($"Installing AI Packages.\n{string.Join("\n", data.packages)}");

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
