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
        public const string kRunningIcon = "console.infoicon";
        public const string kFailedIcon = "console.erroricon";
        public const string kCanceledIcon = "console.warnicon";
        public const int kIconSize = 16;

        private const float WindowMinWidth = 280;
        private const float WindowWidth = 400;
        private const float WindowHeight = 300;

        private ScrollView m_ScrollView;
        private List<ProgressElement> m_Elements = new List<ProgressElement>();
        private int m_LastParentFailedIndex = -1;
        private static ProgressWindow m_Window;
        private Button m_DismissAllBtn;
        private Toggle m_FilterFailed;
        private Toggle m_FilterSuccess;
        private Toggle m_FilterCancelled;

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
            m_Window = this;

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

            m_FilterSuccess = CreateStatusFiler(toolbar, "Success", kSuccessIcon);
            m_FilterCancelled = CreateStatusFiler(toolbar, "Canceled", kCanceledIcon);
            m_FilterFailed = CreateStatusFiler(toolbar, "Failed", kFailedIcon);

            rootVisualElement.Add(toolbar);

            m_ScrollView = new ScrollView()
            {
                style =
                {
                    flexGrow = 1
                }
            };
            rootVisualElement.Add(m_ScrollView);

            UpdateModel();
            UpdateStatusFilter();

            Progress.added += OperationWasAdded;
            Progress.removed += OperationWasRemoved;
            Progress.updated += OperationWasUpdated;

            CheckUnresponsive();
        }

        private void OnDisable()
        {
            EditorApplication.delayCall -= CheckUnresponsive;
            Progress.added -= OperationWasAdded;
            Progress.removed -= OperationWasRemoved;
            Progress.updated -= OperationWasUpdated;
        }

        private void CheckUnresponsive()
        {
            EditorApplication.delayCall -= CheckUnresponsive;

            foreach (var progressElement in m_Elements)
            {
                progressElement.CheckUnresponsive();
            }

            if (Progress.running)
                EditorApplication.delayCall += CheckUnresponsive;
        }

        private void OperationWasAdded(Progress.Item[] ops)
        {
            Assert.IsNotNull(ops[0]);
            var el = AddElement(ops[0]);
            UpdateNbTasks();
            UpdateStatusFilter(el);
            CheckUnresponsive();
        }

        private void OperationWasRemoved(Progress.Item[] ops)
        {
            Assert.IsNotNull(ops[0]);

            var id = ops[0].id;

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

            EditorApplication.delayCall += () =>
            {
                UpdateNbTasks();
                CheckUnresponsive();
            };
        }

        private void OperationWasUpdated(Progress.Item[] ops)
        {
            Assert.IsNotNull(ops[0]);

            foreach (var op in ops)
            {
                bool statusWasChanged = false;
                int parentElementIndex = -1;
                for (int i = 0; i < m_Elements.Count; ++i)
                {
                    if (m_Elements[i].dataSource.id == op.id || m_Elements[i].dataSource.id == op.parentId)
                    {
                        if (m_Elements[i].dataSource.id == op.id && m_Elements[i].dataSource.status != op.status)
                            statusWasChanged = true;

                        if (m_Elements[i].TryUpdate(op, op.id))
                        {
                            parentElementIndex = i;
                            break;
                        }
                    }
                }

                Assert.IsTrue(parentElementIndex != -1);

                if (statusWasChanged && op.parentId < 0 && op.status == Progress.Status.Failed)
                {
                    if (m_Elements.Count > 1)
                    {
                        var elementToMove = m_Elements[parentElementIndex];
                        m_Elements.RemoveAt(parentElementIndex);
                        m_ScrollView.RemoveAt(parentElementIndex);
                        m_Elements.Insert(0, elementToMove);
                        m_ScrollView.Insert(0, elementToMove.rootVisualElement);
                    }
                    m_LastParentFailedIndex++;
                }
                UpdateStatusFilter(m_Elements[parentElementIndex]);
            }
            m_DismissAllBtn.SetEnabled(m_Elements.Any(el => !el.dataSource.running));
            CheckUnresponsive();
        }

        private int FindIndexFirstSucceededOrCanceledElement(List<ProgressElement> elements)
        {
            int firstSucceededOrCanceledIndex = elements.Count;
            for (int i = elements.Count - 1; i >= 0; i--)
            {
                if (elements[i].dataSource.status == Progress.Status.Succeeded || elements[i].dataSource.status == Progress.Status.Canceled)
                    firstSucceededOrCanceledIndex = i;
                else
                    break;
            }
            return firstSucceededOrCanceledIndex;
        }

        private UIElements.ToolbarToggle CreateStatusFiler(VisualElement parent, string controlName, string iconName)
        {
            var filterToggle = new UIElements.ToolbarToggle();
            filterToggle.tooltip = controlName;
            filterToggle.value = EditorPrefs.GetBool($"{preferenceKey}{controlName}_filter", true);
            var toggleCheck = filterToggle.Q<VisualElement>("unity-checkmark");
            toggleCheck.style.backgroundImage = new StyleBackground(EditorGUIUtility.LoadIcon(iconName));
            toggleCheck.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
            toggleCheck.style.width = kIconSize;
            toggleCheck.style.height = kIconSize;
            filterToggle.RegisterCallback<ChangeEvent<bool>>((evt) =>
            {
                EditorPrefs.SetBool($"{preferenceKey}{controlName}_filter", filterToggle.value);
                UpdateStatusFilter();
            });
            parent.Add(filterToggle);
            return filterToggle;
        }

        private void UpdateStatusFilter()
        {
            foreach (var el in m_Elements)
            {
                UpdateStatusFilter(el);
            }
        }

        private void UpdateStatusFilter(ProgressElement el)
        {
            if (el.dataSource.status == Progress.Status.Canceled)
            {
                UpdateStatusFilter(el.rootVisualElement, m_FilterCancelled.value);
            }
            else if (el.dataSource.status == Progress.Status.Failed)
            {
                UpdateStatusFilter(el.rootVisualElement, m_FilterFailed.value);
            }
            else if (el.dataSource.status == Progress.Status.Succeeded)
            {
                UpdateStatusFilter(el.rootVisualElement, m_FilterSuccess.value);
            }
        }

        private static void UpdateStatusFilter(VisualElement task, bool shouldShow)
        {
            if (shouldShow && task.style.display.value == DisplayStyle.None)
            {
                task.style.display = DisplayStyle.Flex;
            }
            else if (!shouldShow && task.style.display.value == DisplayStyle.Flex)
            {
                task.style.display = DisplayStyle.None;
            }
        }

        private void ClearAll()
        {
            var finishedTasks = m_Elements.Where(el => !el.dataSource.running).Select(el => el.dataSource.id).ToArray();
            foreach (var id in finishedTasks)
            {
                Progress.Remove(id);
            }
        }

        private void UpdateNbTasks()
        {
            if (m_Elements.Count > 1)
                titleContent = EditorGUIUtility.TrTextContent($"Background Tasks ({m_Elements.Count})");
            else
                titleContent = EditorGUIUtility.TrTextContent($"Background Tasks");

            m_DismissAllBtn.SetEnabled(m_Elements.Any(el => !el.dataSource.running));
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
            // if parent was not found
            var element = new ProgressElement(progressItem);
            if (m_Elements.Count > 0 && progressItem.status == Progress.Status.Failed)
            {
                var insertIndex = -1;
                for (int i = 0; i < m_Elements.Count; i++)
                {
                    if ((m_Elements[i].dataSource.status == Progress.Status.Failed && progressItem.updateTime > m_Elements[i].dataSource.updateTime)
                        || m_Elements[i].dataSource.status != Progress.Status.Failed)
                    {
                        insertIndex = i;
                        break;
                    }
                }
                m_Elements.Insert(insertIndex, element);
                m_ScrollView.Insert(insertIndex, element.rootVisualElement);
                if (insertIndex > m_LastParentFailedIndex)
                    m_LastParentFailedIndex = insertIndex;
                return element;
            }
            else
            {
                m_Elements.Insert(m_LastParentFailedIndex + 1, element);
                m_ScrollView.Insert(m_LastParentFailedIndex + 1, element.rootVisualElement);
            }
            return element;
        }

        private void RemoveAllElements()
        {
            m_Elements.Clear();
            for (var i = m_ScrollView.childCount - 1; i >= 0; i--)
                m_ScrollView.RemoveAt(i);
            m_LastParentFailedIndex = -1;
        }

        private void UpdateModel()
        {
            RemoveAllElements();

            foreach (var op in Progress.EnumerateItems())
            {
                AddElement(op);
            }

            UpdateNbTasks();
        }
    }
}
