// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor.VersionControl;
using System.Collections.Generic;

namespace UnityEditor
{
    [CustomEditor(typeof(UnityEditor.MonoManager))]
    internal class ScriptExecutionOrderInspector : Editor
    {
        /*
         * @TODO
        - Make it work with imported dll's + meta file serialization
        */

        internal override string targetTitle
        {
            get { return "Script Execution Order"; }
        }

        public class SortMonoScriptNameOrder : IComparer<MonoScript>
        {
            public virtual int Compare(MonoScript x, MonoScript y)
            {
                if (x != null && y != null)
                {
                    var xClass = x.GetClass();
                    var yClass = y.GetClass();
                    if (xClass != null && yClass != null)
                        return xClass.FullName.CompareTo(yClass.FullName);

                    return x.name.CompareTo(y.name);
                }
                return -1;
            }
        }

        public class SortMonoScriptExecutionOrder : SortMonoScriptNameOrder
        {
            ScriptExecutionOrderInspector inspector;

            public SortMonoScriptExecutionOrder(ScriptExecutionOrderInspector inspector)
            {
                this.inspector = inspector;
            }

            public override int Compare(MonoScript x, MonoScript y)
            {
                if (x != null && y != null)
                {
                    int orderX = inspector.GetExecutionOrder(x);
                    int orderY = inspector.GetExecutionOrder(y);
                    if (orderX == orderY)
                        return base.Compare(x, y);

                    return orderX.CompareTo(orderY);
                }
                return -1;
            }
        }

        private const int kOrderRangeMin = -32000;
        private const int kOrderRangeMax =  32000;
        private const int kListElementHeight = 21;
        private const int kIntFieldWidth = 50;
        private const int kPreferredSpacing = 100;
        private int[] kRoundingAmounts = new int[] { 1000, 500, 100, 50, 10, 5, 1 };

        private MonoScript m_Edited = null;
        private List<MonoScript> m_CustomTimeScripts;
        private List<MonoScript> m_DefaultTimeScripts;
        private static MonoScript sDummyScript;
        private Vector2 m_Scroll = Vector2.zero;
        private static readonly List<ScriptExecutionOrderInspector> m_Instances = new List<ScriptExecutionOrderInspector>();

        // Important that these 3 use data types that are serializable.
        // That way we don't loose the unapplied reordering upon a script compile.
        private MonoScript[] m_AllScripts;
        private int[] m_AllOrders;
        private bool m_DirtyOrders = false;

        private static int s_DropFieldHash = "DropField".GetHashCode();

        public static Styles m_Styles;

        public class Styles
        {
            public GUIContent helpText = EditorGUIUtility.TextContent("Add scripts to the custom order and drag them to reorder.\n\nScripts in the custom order can execute before or after the default time and are executed from top to bottom. All other scripts execute at the default time in the order they are loaded.\n\n(Changing the order of a script may modify the meta data for more than one script.)");
            public GUIContent iconToolbarPlus = EditorGUIUtility.IconContent("Toolbar Plus", "|Add script to custom order");
            public GUIContent iconToolbarMinus = EditorGUIUtility.IconContent("Toolbar Minus", "|Remove script from custom order");
            public GUIContent defaultTimeContent = EditorGUIUtility.TextContent("Default Time|All scripts not in the custom order are executed at the default time.");
            public GUIStyle toolbar = "TE Toolbar";
            public GUIStyle toolbarDropDown = "TE ToolbarDropDown";
            public GUIStyle boxBackground = "TE NodeBackground";
            public GUIStyle removeButton = "InvisibleButton";
            public GUIStyle elementBackground = new GUIStyle("OL Box");
            public GUIStyle defaultTime = new GUIStyle(EditorStyles.inspectorBig);
            public GUIStyle draggingHandle = "WindowBottomResize";
            public GUIStyle dropField = new GUIStyle(EditorStyles.objectFieldThumb);

            public Styles()
            {
                boxBackground.margin = new RectOffset();
                boxBackground.padding = new RectOffset(1, 1, 1, 0);

                elementBackground.overflow = new RectOffset(1, 1, 1, 0);

                defaultTime.alignment = TextAnchor.MiddleCenter;
                defaultTime.overflow = new RectOffset(0, 0, 1, 0);

                // Drop field style that has extra overflow and is only visible when "on".
                // Used to draw a blue glow when dragging scripts into the ordering to
                // indicate that drag-and-drop is supported.
                dropField.overflow = new RectOffset(2, 2, 2, 2);
                dropField.normal.background = null;
                dropField.hover.background = null;
                dropField.active.background = null;
                dropField.focused.background = null;
            }
        }

        [MenuItem("CONTEXT/MonoManager/Reset")]
        private static void Reset(MenuCommand cmd)
        {
            var instances = ScriptExecutionOrderInspector.GetInstances();

            foreach (var instance in instances)
            {
                for (var i = 0; i < instance.m_AllOrders.Length; i++)
                    instance.m_AllOrders[i] = 0;

                instance.Apply();
            }
        }

        public void OnEnable()
        {
            if (sDummyScript == null)
                sDummyScript = new MonoScript();

            // Don't reload the order if an unapplied reordering exists
            if (m_AllScripts == null || !m_DirtyOrders)
                PopulateScriptArray();

            if (!m_Instances.Contains(this))
                m_Instances.Add(this);

            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        public void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        private void AskApplyRevertIfNecessary()
        {
            if (!m_DirtyOrders)
                return;

            if (EditorUtility.DisplayDialog("Unapplied execution order", "Unapplied script execution order", "Apply", "Revert"))
                Apply();
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode)
                return;

            AskApplyRevertIfNecessary();
        }

        static Object MonoScriptValidatorCallback(Object[] references, System.Type objType, SerializedProperty property, EditorGUI.ObjectFieldValidatorOptions options)
        {
            foreach (Object i in references)
            {
                var monoScript = i as MonoScript;
                if (monoScript != null && IsValidScript(monoScript))
                {
                    return monoScript;
                }
            }
            return null;
        }

        static bool IsValidScript(MonoScript script)
        {
            if (script == null)
                return false;

            // The user can only define the order of scripts that contains valid classes (see case 579536)
            if (script.GetClass() == null)
                return false;

            // Only allow MonoBehaviours and ScriptableObjects
            bool isMonoBehaviour = typeof(MonoBehaviour).IsAssignableFrom(script.GetClass());
            bool isScriptableObject = typeof(ScriptableObject).IsAssignableFrom(script.GetClass());
            if (!isMonoBehaviour && !isScriptableObject)
            {
                return false;
            }

            // The user can only define the order of scripts in the assets folder.
            if (AssetDatabase.GetAssetPath(script).IndexOf("Assets/") != 0)
                return false;

            return true;
        }

        internal static List<ScriptExecutionOrderInspector> GetInstances()
        {
            return m_Instances;
        }

        void PopulateScriptArray()
        {
            m_AllScripts = MonoImporter.GetAllRuntimeMonoScripts();
            m_AllOrders = new int[m_AllScripts.Length];

            // Create cleaned up list of scripts
            m_CustomTimeScripts = new List<MonoScript>();
            m_DefaultTimeScripts = new List<MonoScript>();
            for (int i = 0; i < m_AllScripts.Length; i++)
            {
                MonoScript script = m_AllScripts[i];
                m_AllOrders[i] = MonoImporter.GetExecutionOrder(script);

                if (!IsValidScript(script))
                    continue;

                if (GetExecutionOrder(script) == 0)
                    m_DefaultTimeScripts.Add(script);
                else
                    m_CustomTimeScripts.Add(script);
            }

            // Add two dummy items used for the default time area
            m_CustomTimeScripts.Add(sDummyScript);
            m_CustomTimeScripts.Add(sDummyScript);

            // Assign and sort
            m_CustomTimeScripts.Sort(new SortMonoScriptExecutionOrder(this));
            m_DefaultTimeScripts.Sort(new SortMonoScriptNameOrder());
            m_Edited = null;

            m_DirtyOrders = false;
        }

        private int GetExecutionOrder(MonoScript script)
        {
            int index = System.Array.IndexOf<MonoScript>(m_AllScripts, script);
            if (index >= 0)
                return m_AllOrders[index];
            return 0;
        }

        private void SetExecutionOrder(MonoScript script, int order)
        {
            int index = System.Array.IndexOf<MonoScript>(m_AllScripts, script);
            if (index >= 0)
            {
                m_AllOrders[index] = Mathf.Clamp(order, kOrderRangeMin, kOrderRangeMax);
                m_DirtyOrders = true;
            }
        }

        private void Apply()
        {
            var changedIndices = new List<int>();
            var changedScripts = new List<MonoScript>();
            for (int i = 0; i < m_AllScripts.Length; i++)
            {
                if (MonoImporter.GetExecutionOrder(m_AllScripts[i]) != m_AllOrders[i])
                {
                    changedIndices.Add(i);
                    changedScripts.Add(m_AllScripts[i]);
                }
            }

            bool editable = true;

            if (Provider.enabled)
            {
                var task = Provider.Checkout(changedScripts.ToArray(), CheckoutMode.Meta);
                task.Wait();
                editable = task.success;
            }

            if (editable)
            {
                foreach (int index in changedIndices)
                    MonoImporter.SetExecutionOrder(m_AllScripts[index], m_AllOrders[index]);

                PopulateScriptArray();
            }
            else
            {
                Debug.LogError("Could not checkout scrips in version control for changing script execution order");
            }
        }

        private void Revert()
        {
            PopulateScriptArray();
        }

        private void OnDestroy()
        {
            if (m_Instances.Contains(this))
                m_Instances.Remove(this);

            if (!Application.isPlaying)
                AskApplyRevertIfNecessary();
        }

        private void ApplyRevertGUI()
        {
            EditorGUILayout.Space();
            bool wasEnabled = GUI.enabled;
            GUI.enabled = m_DirtyOrders;

            GUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Revert"))
                    Revert();

                if (GUILayout.Button("Apply"))
                    Apply();
            } GUILayout.EndHorizontal();

            GUI.enabled = wasEnabled;
        }

        private void MenuSelection(object userData, string[] options, int selected)
        {
            AddScriptToCustomOrder(m_DefaultTimeScripts[selected]);
        }

        private void AddScriptToCustomOrder(MonoScript script)
        {
            if (!IsValidScript(script))
                return;

            if (m_CustomTimeScripts.Contains(script))
                return;

            int orderOfAddedScript = RoundByAmount(GetExecutionOrderAtIndex(m_CustomTimeScripts.Count - 1) + kPreferredSpacing, kPreferredSpacing);
            SetExecutionOrder(script, orderOfAddedScript);
            m_CustomTimeScripts.Add(script);
            m_DefaultTimeScripts.Remove(script);
        }

        private void ShowScriptPopup(Rect r)
        {
            int length = m_DefaultTimeScripts.Count;
            string[] names = new string[length];
            bool[] enabled = new bool[length];
            for (int c = 0; c < length; c++)
            {
                names[c] = m_DefaultTimeScripts[c].GetClass().FullName;
                enabled[c] = true;
            }
            EditorUtility.DisplayCustomMenu(r, names, enabled, null, MenuSelection, null);
        }

        private int RoundBasedOnContext(int val, int lowerBound, int upperBound)
        {
            // Make the bounds a bit closer to avoid the value ending up right next to one of the bounds
            int fraction = Mathf.Max(0, (upperBound - lowerBound) / 6);
            lowerBound += fraction;
            upperBound -= fraction;

            // Round by smaller amounts until we find a value that fit within the bounds
            for (int i = 0; i < kRoundingAmounts.Length; i++)
            {
                int roundedVal = RoundByAmount(val, kRoundingAmounts[i]);
                if (roundedVal > lowerBound && roundedVal < upperBound)
                    return roundedVal;
            }

            return val;
        }

        private int RoundByAmount(int val, int rounding)
        {
            return Mathf.RoundToInt(val / (float)rounding) * rounding;
        }

        private int GetAverageRoundedAwayFromZero(int a, int b)
        {
            if ((a + b) % 2 == 0)
                return (a + b) / 2;
            else
                return (a + b + System.Math.Sign(a + b)) / 2;
        }

        private void SetExecutionOrderAtIndexAccordingToNeighbors(int indexOfChangedItem, int pushDirection)
        {
            // Ignore invalid index
            if (indexOfChangedItem < 0 || indexOfChangedItem >= m_CustomTimeScripts.Count)
                return;

            // Set order if changed is first in list
            if (indexOfChangedItem == 0)
            {
                SetExecutionOrderAtIndex(
                    indexOfChangedItem,
                    RoundByAmount(GetExecutionOrderAtIndex(indexOfChangedItem + 1) - kPreferredSpacing, kPreferredSpacing));
                return;
            }

            // Set order if changed is last in list
            if (indexOfChangedItem == m_CustomTimeScripts.Count - 1)
            {
                SetExecutionOrderAtIndex(
                    indexOfChangedItem,
                    RoundByAmount(GetExecutionOrderAtIndex(indexOfChangedItem - 1) + kPreferredSpacing, kPreferredSpacing));
                return;
            }

            // Make nr average of prev and next script nr, but rounded to a nice round number
            int prevOrder = GetExecutionOrderAtIndex(indexOfChangedItem - 1);
            int nextOrder = GetExecutionOrderAtIndex(indexOfChangedItem + 1);
            int newAverageExecutionOrder = RoundBasedOnContext(GetAverageRoundedAwayFromZero(prevOrder, nextOrder), prevOrder, nextOrder);

            if (newAverageExecutionOrder != 0)
            {
                if (pushDirection == 0)
                    pushDirection = GetBestPushDirectionForOrderValue(newAverageExecutionOrder);

                // Ensure new value is at least one higher/lower than the neighbor
                if (pushDirection > 0)
                    newAverageExecutionOrder = Mathf.Max(newAverageExecutionOrder, prevOrder + 1);
                else
                    newAverageExecutionOrder = Mathf.Min(newAverageExecutionOrder, nextOrder - 1);
            }

            SetExecutionOrderAtIndex(indexOfChangedItem, newAverageExecutionOrder);
        }

        private void UpdateOrder(MonoScript changedScript)
        {
            // Remove the script prior to reordering
            // Remember to add later, either to custom or default time list
            m_CustomTimeScripts.Remove(changedScript);

            int changedScriptOrder = GetExecutionOrder(changedScript);

            // If script order was set to zero, script gets removed from list, so no reordering needed
            if (changedScriptOrder == 0)
            {
                m_DefaultTimeScripts.Add(changedScript);
                m_DefaultTimeScripts.Sort(new SortMonoScriptNameOrder());
                return;
            }

            // See if any other scripts have an order that conflicts with the one just moved
            int conflictedIndex = -1;
            for (int i = 0; i < m_CustomTimeScripts.Count; i++)
            {
                if (GetExecutionOrderAtIndex(i) == changedScriptOrder)
                {
                    conflictedIndex = i;
                    break;
                }
            }

            // If not, add the changed script back and sort orders.
            // This will also happen if there' only one script in the custom order,
            // so we don't need to worry about that case further down.
            if (conflictedIndex == -1)
            {
                m_CustomTimeScripts.Add(changedScript);
                m_CustomTimeScripts.Sort(new SortMonoScriptExecutionOrder(this));
                return;
            }

            int pushDirection = GetBestPushDirectionForOrderValue(changedScriptOrder);
            if (pushDirection == 1)
            {
                m_CustomTimeScripts.Insert(conflictedIndex, changedScript);
                conflictedIndex++;
            }
            else
            {
                m_CustomTimeScripts.Insert(conflictedIndex + 1, changedScript);
            }

            PushAwayToAvoidConflicts(conflictedIndex, pushDirection);
        }

        private void PushAwayToAvoidConflicts(int startIndex, int pushDirection)
        {
            int curIndex = startIndex;
            while (curIndex >= 0 && curIndex < m_CustomTimeScripts.Count)
            {
                // Check if there's any conflict between the order at this index and the previous one. If not, stop here.
                if ((GetExecutionOrderAtIndex(curIndex) - GetExecutionOrderAtIndex(curIndex - pushDirection)) * pushDirection >= 1)
                    break;

                SetExecutionOrderAtIndexAccordingToNeighbors(curIndex, pushDirection);
                curIndex += pushDirection;
            }
        }

        private int GetBestPushDirectionForOrderValue(int order)
        {
            // If there's any conflicts we want to push neighboring scripts until the conflict has been resolved.
            // However, we can't push the order of any script past 0 (from either direction)
            // or past the minimum or maximum allowed order values.
            // So we push away from zero if the new order is close to zero and towards zero if it's closer to the min or max.
            // This should only be able to fail if the user has more than 16000 scripts (a quarter of the total range).
            int pushDirection = (int)Mathf.Sign(order);
            if (order < kOrderRangeMin / 2 || order > kOrderRangeMax / 2)
                pushDirection = -pushDirection;
            return pushDirection;
        }

        public override bool UseDefaultMargins() { return false; }

        public override void OnInspectorGUI()
        {
            if (m_Styles == null)
                m_Styles = new Styles();

            if (m_Edited)
            {
                UpdateOrder(m_Edited);
                m_Edited = null;
            }

            EditorGUILayout.BeginVertical(EditorStyles.inspectorFullWidthMargins);
            {
                GUILayout.Label(m_Styles.helpText, EditorStyles.helpBox);

                EditorGUILayout.Space();

                // Vertical that contains box and the toolbar below it
                Rect listRect = EditorGUILayout.BeginVertical();
                {
                    int dropFieldId = EditorGUIUtility.GetControlID(s_DropFieldHash, FocusType.Passive, listRect);
                    MonoScript dropped = EditorGUI.DoDropField(listRect, dropFieldId, typeof(MonoScript), MonoScriptValidatorCallback, false, m_Styles.dropField) as MonoScript;
                    if (dropped)
                        AddScriptToCustomOrder(dropped);

                    // Vertical that is used as a border around the scrollview
                    EditorGUILayout.BeginVertical(m_Styles.boxBackground);
                    {
                        // The scrollview itself
                        m_Scroll = EditorGUILayout.BeginVerticalScrollView(m_Scroll);
                        {
                            // List
                            Rect r = GUILayoutUtility.GetRect(10, kListElementHeight * m_CustomTimeScripts.Count, GUILayout.ExpandWidth(true));
                            int changed = DragReorderGUI.DragReorder(r, kListElementHeight, m_CustomTimeScripts, DrawElement);
                            if (changed >= 0)
                            {
                                // Give dragged item value in between neighbors
                                SetExecutionOrderAtIndexAccordingToNeighbors(changed, 0);
                                // Update neighbors if needed
                                UpdateOrder(m_CustomTimeScripts[changed]);
                                // Neighbors may have been moved so there's more space around dragged item,
                                // so set order again to get possible rounding benefits
                                SetExecutionOrderAtIndexAccordingToNeighbors(changed, 0);
                            }
                        } EditorGUILayout.EndScrollView();
                    } EditorGUILayout.EndVertical();

                    // The toolbar below the box
                    GUILayout.BeginHorizontal(m_Styles.toolbar);
                    {
                        GUILayout.FlexibleSpace();
                        Rect r2;
                        GUIContent content = m_Styles.iconToolbarPlus;
                        r2 = GUILayoutUtility.GetRect(content, m_Styles.toolbarDropDown);
                        if (EditorGUI.DropdownButton(r2, content, FocusType.Passive, m_Styles.toolbarDropDown))
                            ShowScriptPopup(r2);
                    } GUILayout.EndHorizontal();
                } GUILayout.EndVertical();

                ApplyRevertGUI();
            } GUILayout.EndVertical();

            GUILayout.FlexibleSpace();
        }

        int GetExecutionOrderAtIndex(int idx)
        {
            return GetExecutionOrder(m_CustomTimeScripts[idx]);
        }

        void SetExecutionOrderAtIndex(int idx, int order)
        {
            SetExecutionOrder(m_CustomTimeScripts[idx], order);
        }

        private Rect GetButtonLabelRect(Rect r)
        {
            return new Rect(r.x + 20, r.y + 1, r.width - GetMinusButtonSize().x - 10 - 20 - (kIntFieldWidth + 5), r.height);
        }

        private Rect GetAddRemoveButtonRect(Rect r)
        {
            var buttonSize = GetMinusButtonSize();

            return new Rect(r.xMax - buttonSize.x - 5, r.y + 1, buttonSize.x, buttonSize.y);
        }

        private Rect GetFieldRect(Rect r)
        {
            return new Rect(r.xMax - kIntFieldWidth - GetMinusButtonSize().x - 10, r.y + 2, kIntFieldWidth, r.height - 5);
        }

        private Vector2 GetMinusButtonSize()
        {
            return m_Styles.removeButton.CalcSize(m_Styles.iconToolbarMinus);
        }

        private Rect GetDraggingHandleRect(Rect r)
        {
            return new Rect(r.x + 5, r.y + 7, 10, r.height - 14);
        }

        public void DrawElement(Rect r, object obj, bool dragging)
        {
            MonoScript script = obj as MonoScript;

            if (Event.current.type == EventType.Repaint)
            {
                m_Styles.elementBackground.Draw(r, false, false, false, false);
                m_Styles.draggingHandle.Draw(GetDraggingHandleRect(r), false, false, false, false);
            }

            GUI.Label(GetButtonLabelRect(r), script.GetClass().FullName);

            int oldNr = GetExecutionOrder(script);
            Rect position = GetFieldRect(r);
            // associate control id with script so that removing an element when its text field is active will not potentially cause subsequent element to inherit value when list is reordered
            int id = GUIUtility.GetControlID(script.GetHashCode(), FocusType.Keyboard, position);
            string intStr = EditorGUI.DelayedTextFieldInternal(position, id, GUIContent.none, oldNr.ToString(), "0123456789-", EditorStyles.textField);
            int newNr = oldNr;
            if (System.Int32.TryParse(intStr, out newNr) && newNr != oldNr)
            {
                SetExecutionOrder(script, newNr);
                m_Edited = script;
            }

            if (GUI.Button(GetAddRemoveButtonRect(r), m_Styles.iconToolbarMinus, m_Styles.removeButton))
            {
                SetExecutionOrder(script, 0);
                m_Edited = script;
            }
        }

        class DragReorderGUI
        {
            public delegate void DrawElementDelegate(Rect r, object obj, bool dragging);

            private static int s_ReorderingDraggedElement;
            private static float[] s_ReorderingPositions;
            private static int[] s_ReorderingGoals;
            private static int s_DragReorderGUIHash = "DragReorderGUI".GetHashCode();

            private static bool IsDefaultTimeElement(MonoScript element)
            {
                return (element.name == string.Empty);
            }

            public static int DragReorder(Rect position, int elementHeight, List<MonoScript> elements, DrawElementDelegate drawElementDelegate)
            {
                int id = GUIUtility.GetControlID(s_DragReorderGUIHash, FocusType.Passive);

                Rect elementRect = position;
                elementRect.height = elementHeight;

                int defPos = 0;
                Rect defRect;

                // If we're dragging, draw elements based on their animated reordering positions,
                // but only for repainting. Control event handling does't like to suddenly change order,
                // so things will screw up if we HANDLE the controls in a different order when dragging.
                if (GUIUtility.hotControl == id && Event.current.type == EventType.Repaint)
                {
                    for (int i = 0; i < elements.Count; i++)
                    {
                        // Don't draw the dragged element as part of loop
                        if (i == s_ReorderingDraggedElement)
                            continue;
                        if (IsDefaultTimeElement(elements[i]))
                        {
                            defPos = i;
                            i++;
                            continue;
                        }
                        elementRect.y = position.y + s_ReorderingPositions[i] * elementHeight;
                        drawElementDelegate(elementRect, elements[i], false);
                    }
                    defRect = new Rect(elementRect.x, position.y + s_ReorderingPositions[defPos] * elementHeight, elementRect.width, (s_ReorderingPositions[defPos + 1] - s_ReorderingPositions[defPos] + 1) * elementHeight);
                }
                // For everything else than repainting while dragging,
                // draw controls based on their positions in the array.
                else
                {
                    for (int i = 0; i < elements.Count; i++)
                    {
                        elementRect.y = position.y + i * elementHeight;
                        if (IsDefaultTimeElement(elements[i]))
                        {
                            defPos = i;
                            i++;
                            continue;
                        }
                        drawElementDelegate(elementRect, elements[i], false);
                    }
                    defRect = new Rect(elementRect.x, position.y + defPos * elementHeight, elementRect.width, elementHeight * 2);
                }

                GUI.Label(defRect, m_Styles.defaultTimeContent, m_Styles.defaultTime);

                bool isAddingToDefault = defRect.height > elementHeight * 2.5f;

                if (GUIUtility.hotControl == id)
                {
                    if (isAddingToDefault)
                        GUI.color = new Color(1, 1, 1, 0.5f);
                    // Draw the dragged element after all the other ones
                    elementRect.y = position.y + s_ReorderingPositions[s_ReorderingDraggedElement] * elementHeight;
                    drawElementDelegate(elementRect, elements[s_ReorderingDraggedElement], true);

                    GUI.color = Color.white;
                }

                int changed = -1;
                EventType type = Event.current.GetTypeForControl(id);
                switch (type)
                {
                    case EventType.MouseDown:
                        if (position.Contains(Event.current.mousePosition))
                        {
                            GUIUtility.keyboardControl = 0;
                            EditorGUI.EndEditingActiveTextField();

                            s_ReorderingDraggedElement = Mathf.FloorToInt((Event.current.mousePosition.y - position.y) / elementHeight);
                            if (!IsDefaultTimeElement(elements[s_ReorderingDraggedElement]))
                            {
                                s_ReorderingPositions = new float[elements.Count];
                                s_ReorderingGoals = new int[elements.Count];
                                for (int i = 0; i < elements.Count; i++)
                                {
                                    s_ReorderingGoals[i] = i;
                                    s_ReorderingPositions[i] = i;
                                }
                                GUIUtility.hotControl = id;
                                Event.current.Use();
                            }
                        }
                        break;
                    case EventType.MouseDrag:
                        if (GUIUtility.hotControl != id)
                            break;

                        // Set reordering position of dragged element based on mouse cursor
                        s_ReorderingPositions[s_ReorderingDraggedElement] = (Event.current.mousePosition.y - position.y) / elementHeight - 0.5f;
                        // Clamp to range of list
                        s_ReorderingPositions[s_ReorderingDraggedElement] = Mathf.Clamp(s_ReorderingPositions[s_ReorderingDraggedElement], 0, elements.Count - 1);
                        // Set draggedToPosition based on dragged position
                        int draggedToPosition = Mathf.RoundToInt(s_ReorderingPositions[s_ReorderingDraggedElement]);

                        // if dragged to a new position, re-assign goals
                        if (draggedToPosition != s_ReorderingGoals[s_ReorderingDraggedElement])
                        {
                            // Reset
                            for (int i = 0; i < elements.Count; i++)
                                s_ReorderingGoals[i] = i;
                            // Find direction that other elements must be moved in
                            int direction = (draggedToPosition > s_ReorderingDraggedElement ? -1 : 1);

                            // Move goals of elements to make room for the dragged one
                            for (int i = s_ReorderingDraggedElement; i != draggedToPosition; i -= direction)
                                s_ReorderingGoals[i - direction] = i;

                            // At last, move the goal of the dragged element
                            s_ReorderingGoals[s_ReorderingDraggedElement] = draggedToPosition;
                        }
                        Event.current.Use();
                        break;
                    case EventType.MouseUp:
                        if (GUIUtility.hotControl != id)
                            break;

                        if (s_ReorderingGoals[s_ReorderingDraggedElement] != s_ReorderingDraggedElement)
                        {
                            // Reorder array according to the reordering goals
                            List<MonoScript> reorderedArray = new List<MonoScript>(elements);
                            for (int i = 0; i < elements.Count; i++)
                                elements[s_ReorderingGoals[i]] = reorderedArray[i];

                            // Return which elements was just moved
                            changed = s_ReorderingGoals[s_ReorderingDraggedElement];
                        }

                        // Reset
                        s_ReorderingGoals = null;
                        s_ReorderingPositions = null;
                        s_ReorderingDraggedElement = -1;
                        GUIUtility.hotControl = 0;
                        Event.current.Use();
                        break;
                    case EventType.Repaint:
                        // Animate elements to move towards their goals
                        if (GUIUtility.hotControl == id)
                        {
                            for (int i = 0; i < elements.Count; i++)
                                if (i != s_ReorderingDraggedElement)
                                    s_ReorderingPositions[i] = Mathf.MoveTowards(s_ReorderingPositions[i], s_ReorderingGoals[i], 0.075f);
                            GUIView.current.Repaint();
                        }
                        break;
                }

                return changed;
            }
        }
    }
}
