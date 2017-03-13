// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;


namespace UnityEditor.TreeViewExamples
{
    internal class TreeViewTestWindow : EditorWindow, IHasCustomMenu
    {
        // Test 1
        private BackendData m_BackendData;
        private TreeViewTest m_TreeViewTest;
        private TreeViewTest m_TreeViewTest2;

        // Test 2
        private BackendData m_BackendData2;
        private TreeViewTestWithCustomHeight m_TreeViewWithCustomHeight;

        private TestType m_TestType = TestType.LargeTreesWithStandardGUI;

        enum TestType
        {
            LargeTreesWithStandardGUI,
            TreeWithCustomItemHeight
        }

        public TreeViewTestWindow()
        {
            titleContent = new GUIContent("TreeView Test");
        }

        void OnEnable()
        {
            position = new Rect(100, 100, 600, 600);
        }

        void OnGUI()
        {
            switch (m_TestType)
            {
                case TestType.LargeTreesWithStandardGUI:
                    TestLargeTreesWithFixedItemHeightAndPingingAndFraming();
                    break;
                case TestType.TreeWithCustomItemHeight:
                    TestTreeWithCustomItemHeights();
                    break;
            }
        }

        void TestTreeWithCustomItemHeights()
        {
            Rect rect = new Rect(0, 0, position.width, position.height);
            if (m_TreeViewWithCustomHeight == null)
            {
                m_BackendData2 = new BackendData();
                m_BackendData2.GenerateData(300);

                m_TreeViewWithCustomHeight = new TreeViewTestWithCustomHeight(this, m_BackendData2, rect);
            }

            m_TreeViewWithCustomHeight.OnGUI(rect);
        }

        void TestLargeTreesWithFixedItemHeightAndPingingAndFraming()
        {
            Rect leftRect = new Rect(0, 0, position.width / 2, position.height);
            Rect rightRect = new Rect(position.width / 2, 0, position.width / 2, position.height);
            if (m_TreeViewTest == null)
            {
                m_BackendData = new BackendData();
                m_BackendData.GenerateData(1000000);

                bool lazy = false;
                m_TreeViewTest = new TreeViewTest(this, lazy);
                m_TreeViewTest.Init(leftRect, m_BackendData);

                lazy = true;
                m_TreeViewTest2 = new TreeViewTest(this, lazy);
                m_TreeViewTest2.Init(rightRect, m_BackendData);
            }

            m_TreeViewTest.OnGUI(leftRect);
            m_TreeViewTest2.OnGUI(rightRect);
            EditorGUI.DrawRect(new Rect(leftRect.xMax - 1, 0, 2, position.height), new Color(0.4f, 0.4f, 0.4f, 0.8f));
        }

        public virtual void AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Large TreeView"), m_TestType == TestType.LargeTreesWithStandardGUI, () => m_TestType = TestType.LargeTreesWithStandardGUI);
            menu.AddItem(new GUIContent("Custom Item Height TreeView"), m_TestType == TestType.TreeWithCustomItemHeight, () => m_TestType = TestType.TreeWithCustomItemHeight);
        }
    }
} // UnityEditor
