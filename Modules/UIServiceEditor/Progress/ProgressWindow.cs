// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEngine.Assertions;
using System.Linq;

namespace UnityEditor
{
    internal class ProgressWindow : EditorWindow
    {
        internal const string ussPath = "StyleSheets/ProgressWindow/ProgressWindow.uss";
        internal const string ussPathDark = "StyleSheets/ProgressWindow/ProgressWindowDark.uss";
        internal const string ussPathLight = "StyleSheets/ProgressWindow/ProgressWindowLight.uss";
        public const string preferenceKey = "ProgressWindow.";

        public const string kSuccessIcon = "completed_task";
        public const string kFailedIcon = "console.erroricon";
        public const string kCanceledIcon = "console.warnicon";
        public const int kIconSize = 16;

        private const float WindowMinWidth = 280;
        private const float WindowWidth = 400;
        private const float WindowHeight = 300;

        private ScrollView m_ScrollView;
        private List<ProgressElement> m_Elements = new List<ProgressElement>();
        private static ProgressWindow m_Window;
        private Button m_DismissAllBtn;
        private double m_LastUpdate;

        const double k_CheckUnresponsiveDelayInSecond = 1.0;

        private TaskReorderingHelper m_TaskReorderingHelper;

        [MenuItem("Window/General/Progress", priority = 50)]
        public static void ShowDetails()
        {
            ShowDetails(false);
        }

        internal static bool canHideDetails => m_Window && !m_Window.docked;

        internal static void HideDetails()
        {
            if (canHideDetails)
            {
                m_Window.Close();
                m_Window = null;
            }
        }

        internal static void ShowDetails(bool shouldReposition)
        {
            if (m_Window && m_Window.docked)
                shouldReposition = false;

            if (m_Window == null)
            {
                var wins = Resources.FindObjectsOfTypeAll<ProgressWindow>();
                if (wins.Length > 0)
                    m_Window = wins[0];
            }

            bool newWindowCreated = false;
            if (!m_Window)
            {
                m_Window = CreateInstance<ProgressWindow>();
                newWindowCreated = true;
            }

            m_Window.Show();
            m_Window.Focus();

            if (newWindowCreated && shouldReposition)
            {
                var mainWindowRect = EditorGUIUtility.GetMainWindowPosition();
                var size = new Vector2(WindowWidth, WindowHeight);
                m_Window.position = new Rect(mainWindowRect.xMax - WindowWidth - 6, mainWindowRect.yMax - WindowHeight - 50, size.x, size.y);
                m_Window.minSize = new Vector2(WindowMinWidth, size.y);
            }
        }

        // only for tests
        internal static Progress.Item GetProgressItem(int id)
        {
            if (m_Window == null)
                return null;

            foreach (var elem in m_Window.m_Elements)
            {
                if (elem.dataSource.id == id)
                    return elem.dataSource;

                var childItem = elem.GetSubTaskItem(id);
                if (childItem != null)
                    return childItem;
            }
            return null;
        }

        private void OnEnable()
        {
            // using (new EditorPerformanceTracker("ProgressWindow.OnEnable"))
            {
                m_Window = this;
                titleContent = EditorGUIUtility.TrTextContent($"Background Tasks");

                rootVisualElement.AddStyleSheetPath(ussPath);
                if (EditorGUIUtility.isProSkin)
                    rootVisualElement.AddStyleSheetPath(ussPathDark);
                else
                    rootVisualElement.AddStyleSheetPath(ussPathLight);

                var toolbar = new UIElements.Toolbar();
                m_DismissAllBtn = new UIElements.ToolbarButton(ClearAll)
                {
                    name = "DismissAllBtn",
                    text = "Clear inactive",
                };
                toolbar.Add(m_DismissAllBtn);

                // This is our friend the spacer
                toolbar.Add(new VisualElement()
                {
                    style =
                    {
                        flexGrow = 1
                    }
                });

                rootVisualElement.Add(toolbar);

                m_ScrollView = new ScrollView()
                {
                    style =
                    {
                        flexGrow = 1
                    }
                };
                rootVisualElement.Add(m_ScrollView);

                m_TaskReorderingHelper = new TaskReorderingHelper(RemoveAtInsertAt);
                UpdateModel();

                Progress.added += OperationWasAdded;
                Progress.removed += OperationWasRemoved;
                Progress.updated += OperationWasUpdated;

                CheckUnresponsive();
            }
        }

        private void RemoveAtInsertAt(int insertIndex, int itemIndex)
        {
            var elementToMove = m_Elements[itemIndex];
            m_Elements.RemoveAt(itemIndex);
            m_ScrollView.RemoveAt(itemIndex);
            if (itemIndex <= insertIndex)
                --insertIndex;
            m_Elements.Insert(insertIndex, elementToMove);
            m_ScrollView.Insert(insertIndex, elementToMove.rootVisualElement);
        }

        private void Update()
        {
            var now = EditorApplication.timeSinceStartup;
            if (Progress.running && (now - m_LastUpdate) > k_CheckUnresponsiveDelayInSecond)
            {
                CheckUnresponsive();
                m_LastUpdate = now;
            }
        }

        private void OnDisable()
        {
            Progress.added -= OperationWasAdded;
            Progress.removed -= OperationWasRemoved;
            Progress.updated -= OperationWasUpdated;
        }

        private void CheckUnresponsive()
        {
            // using (new EditorPerformanceTracker("ProgressWindow.CheckUnresponsive"))
            {
                foreach (var progressElement in m_Elements)
                {
                    progressElement.CheckUnresponsive();
                }
            }
        }

        private void OperationWasAdded(Progress.Item[] ops)
        {
            // using (new EditorPerformanceTracker("ProgressWindow.OperationWasAdded"))
            {
                Assert.IsNotNull(ops[0]);
                var el = AddElement(ops[0]);
                DismissAllBtn();
                CheckUnresponsive();
            }
        }

        private void OperationWasRemoved(Progress.Item[] ops)
        {
            // using (new EditorPerformanceTracker("ProgressWindow.OperationWasRemoved"))
            {
                foreach (var op in ops)
                {
                    Assert.IsNotNull(op);

                    var id = op.id;

                    var foundElement = m_Elements.Find(e => e.dataSource.id == id);
                    if (foundElement != null)
                    {
                        m_ScrollView.Remove(foundElement.rootVisualElement);
                        m_Elements.Remove(foundElement);
                    }
                    else
                    {
                        foreach (var element in m_Elements)
                        {
                            if (element.TryRemove(id))
                            {
                                break;
                            }
                        }
                    }
                }
                CheckUnresponsive();
                DismissAllBtn();
            }
        }

        private void OperationWasUpdated(Progress.Item[] ops)
        {
            // using (new EditorPerformanceTracker("ProgressWindow.OperationWasUpdated"))
            {
                Assert.IsNotNull(ops[0]);

                HashSet<ProgressElement> elementsSubtasksToReorder = new HashSet<ProgressElement>();
                m_TaskReorderingHelper.Clear();
                foreach (var op in ops)
                {
                    bool rootTaskFound = false;
                    for (int i = 0; i < m_Elements.Count; ++i)
                    {
                        if (m_Elements[i].dataSource.id == op.id || m_Elements[i].dataSource.id == op.parentId)
                        {
                            if (m_Elements[i].TryUpdate(op, op.id, elementsSubtasksToReorder.Contains(m_Elements[i]))) // We need to know if we already have this root task, it allows to reset the helper status
                            {
                                rootTaskFound = true;
                                if (m_Elements[i].dataSource.id == op.id)
                                    m_TaskReorderingHelper.AddItemToReorder(op, i); // add the reordering info for the root task
                                else
                                    elementsSubtasksToReorder.Add(m_Elements[i]); // add the root task of the subtask we might need to reorder (the reordering info are added during TryUpdate)
                                break;
                            }
                        }
                    }

                    Assert.IsTrue(rootTaskFound);
                }

                // After the update, we deal with sorting, first for the root tasks, then for the subtasks
                m_TaskReorderingHelper.ReorderItems(m_Elements.Count, i => m_Elements[i].dataSource);
                foreach (var parentElement in elementsSubtasksToReorder)
                {
                    parentElement.ReorderSubtasks();
                }

                m_DismissAllBtn.SetEnabled(m_Elements.Any(el => el.dataSource.finished));
                CheckUnresponsive();
            }
        }

        private void ClearAll()
        {
            var finishedTasks = m_Elements.Where(el => el.dataSource.finished).Select(el => el.dataSource.id).ToArray();
            foreach (var id in finishedTasks)
            {
                Progress.Remove(id);
            }
        }

        private void DismissAllBtn()
        {
            m_DismissAllBtn.SetEnabled(m_Elements.Any(el => el.dataSource.finished));
        }

        private ProgressElement AddElement(Progress.Item progressItem)
        {
            if (progressItem.parentId >= 0)
            {
                for (int i = 0; i < m_Elements.Count; ++i)
                {
                    if (progressItem.parentId == m_Elements[i].dataSource.id)
                    {
                        m_Elements[i].AddElement(progressItem);
                        return m_Elements[i];
                    }
                }
                Assert.IsTrue(true);
            }
            // if there is no parent
            var element = new ProgressElement(progressItem);
            if (m_Elements.Count > 0)
            {
                int insertIndex = m_TaskReorderingHelper.FindIndexToInsertAt(progressItem, m_Elements.Count, i => m_Elements[i].dataSource);
                m_Elements.Insert(insertIndex, element);
                m_ScrollView.Insert(insertIndex, element.rootVisualElement);
                return element;
            }
            else
            {
                m_Elements.Add(element);
                m_ScrollView.Add(element.rootVisualElement);
            }
            return element;
        }

        private void RemoveAllElements()
        {
            m_Elements.Clear();
            for (var i = m_ScrollView.childCount - 1; i >= 0; i--)
                m_ScrollView.RemoveAt(i);
        }

        private void UpdateModel()
        {
            RemoveAllElements();

            foreach (var op in Progress.EnumerateItems())
            {
                AddElement(op);
            }

            DismissAllBtn();
        }
    }
}
