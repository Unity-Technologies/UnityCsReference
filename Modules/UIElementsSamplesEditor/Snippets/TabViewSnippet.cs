// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Samples
{
    internal class TabViewSnippet : ElementSnippet<TabViewSnippet>
    {
        internal override void Apply(VisualElement container)
        {
            /// <sample>
            // Create a TabView with Tabs that only contains a label.
            var csharpTabViewWithLabels = new TabView() { style = { marginTop = 15 } }; // marginTop not required, only for demonstration purposes.
            var tabOne = new Tab("One");
            tabOne.Add(new Label("Tab with labels only: This is some content for the first Tab.") { style = { marginTop = 10 } });
            csharpTabViewWithLabels.Add(tabOne);
            var tabTwo = new Tab("Two");
            tabTwo.Add(new Label("Tab with labels only: This is some content for the second Tab.") { style = { marginTop = 10 } });
            csharpTabViewWithLabels.Add(tabTwo);
            container.Add(csharpTabViewWithLabels);

            // Create a TabView with Tabs that only contains an icon.
            var csharpTabViewWithIcons = new TabView() { style = { marginTop = 15 } }; // marginTop not required, only for demonstration purposes.
            var tabIconConnect = new Tab(EditorGUIUtility.FindTexture("CloudConnect"));
            tabIconConnect.Add(new Label("Tab with icons only: This is some content for the first Tab.") { style = { marginTop = 10 } });
            csharpTabViewWithIcons.Add(tabIconConnect);
            var tabIconStore = new Tab(EditorGUIUtility.FindTexture("Asset Store"));
            tabIconStore.Add(new Label("Tab with icons only: This is some content for the second Tab.") { style = { marginTop = 10 } });
            csharpTabViewWithIcons.Add(tabIconStore);
            container.Add(csharpTabViewWithIcons);

            // Create a TabView with Tabs that only contains an icon and a label.
            var csharpTabViewWithIconsAndLabels = new TabView() { style = { marginTop = 15 } }; // marginTop not required, only for demonstration purposes.
            var tabConnect = new Tab("Connect", EditorGUIUtility.FindTexture("CloudConnect"));
            tabConnect.Add(new Label("Tab with an icon and a labels: This is some content for the first Tab.") { style = { marginTop = 10 } });
            csharpTabViewWithIconsAndLabels.Add(tabConnect);
            var tabStore = new Tab("Store", EditorGUIUtility.FindTexture("Asset Store"));
            tabStore.Add(new Label("Tab with an icon and a labels: This is some content for the second Tab.") { style = { marginTop = 10 } });
            csharpTabViewWithIconsAndLabels.Add(tabStore);
            container.Add(csharpTabViewWithIconsAndLabels);

            // Create a TabView that allows re-ordering of the tabs.
            var csharpReorderableTabView = new TabView() { reorderable = true, style = { marginTop = 10 } }; // marginTop not required, only for demonstration purposes.
            var tabA = new Tab("Tab A");
            tabA.Add(new Label("Reorderable tabs: This is some content for Tab A") { style = { marginTop = 10 } });
            csharpReorderableTabView.Add(tabA);
            var tabB = new Tab("Tab B");
            tabB.Add(new Label("Reorderable tabs: This is some content for Tab B") { style = { marginTop = 10 } });
            csharpReorderableTabView.Add(tabB);
            var tabC = new Tab("Tab C");
            tabC.Add(new Label("Reorderable tabs: This is some content for Tab C") { style = { marginTop = 10 } });
            csharpReorderableTabView.Add(tabC);
            container.Add(csharpReorderableTabView);

            // Create a TabView with closeable tabs.
            var closeTabInfoLabel = new Label($"Last tab closed: None");
            void UpdateLabel(string newLabel) => closeTabInfoLabel.text = $"Last tab closed: {newLabel}";
            var cSharpCloseableTabs = new TabView() { style = { marginTop = 10 } }; // marginTop not required, only for demonstration purposes.
            var closeableTabA = new Tab("Title A") { closeable = true };
            closeableTabA.closed += (tab) => { UpdateLabel(tab.label); };
            closeableTabA.Add(new Label("Closeable tabs: This is some content for Tab A") { style = { marginTop = 10 } });
            cSharpCloseableTabs.Add(closeableTabA);
            var closeableTabB = new Tab("Title B") { closeable = true };
            closeableTabB.closed += (tab) => { UpdateLabel(tab.label); };
            closeableTabB.Add(new Label("Closeable tabs: This is some content for Tab B") { style = { marginTop = 10 } });
            cSharpCloseableTabs.Add(closeableTabB);
            var closeableTabC = new Tab("Title C") { closeable = true };
            closeableTabC.closed += (tab) => { UpdateLabel(tab.label); };
            closeableTabC.Add(new Label("Closeable tabs: This is some content for Tab C") { style = { marginTop = 10 } });
            cSharpCloseableTabs.Add(closeableTabC);
            container.Add(cSharpCloseableTabs);
            container.Add(closeTabInfoLabel);

            // Create a TabView and apply custom styling to specific areas of their tabs.
            var csharpCustomStyledTabView = new TabView() { style = { marginTop = 15 }, classList = { "some-styled-class" }}; // marginTop not required, only for demonstration purposes.
            var customStyledTabOne = new Tab("One");
            customStyledTabOne.Add(new Label("Custom styled tabs: This is some content for the first Tab."));
            csharpCustomStyledTabView.Add(customStyledTabOne);
            var customStyledTabTwo = new Tab("Two");
            customStyledTabTwo.Add(new Label("Custom styled tabs: This is some content for the second Tab."));
            csharpCustomStyledTabView.Add(customStyledTabTwo);
            container.Add(csharpCustomStyledTabView);
            /// </sample>
        }
    }
}

