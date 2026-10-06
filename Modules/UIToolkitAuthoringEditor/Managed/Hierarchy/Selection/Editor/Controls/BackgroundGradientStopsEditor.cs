// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIToolkitAuthoringFramework not yet converted
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    // Gradient Editor popup content: preview bar, draggable stop handles, and the color stops
    // as an editable list (position + color + hex per row). Stops can bind their color or
    // position to a USS variable (see StopData); resolved values are always shown. Shared
    // between the UI Builder gradient popup and (later) the VisualElement inspector.
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    [MovedFrom("Unity.UI.Builder")]
    internal class BackgroundGradientStopsEditor : VisualElement
    {
        const int k_MinStops = 2;
        const int k_MaxStops = UnmanagedBackgroundGradient.MaxStops;

        const float k_RowHeight = 22f;

        const string k_UssPath = "UIToolkitAuthoring/Inspector/Controls/BackgroundGradientStopsEditor.uss";
        const string k_VariableEditingUssPath = "UIToolkitAuthoring/Inspector/Controls/VariableEditing.uss";

        const float k_HandleWidth = 12f;

        const string k_ClassName = "unity-background-gradient-stops-editor";
        const string k_DarkClassName = k_ClassName + "--dark";
        const string k_HandlesClassName = k_ClassName + "__handles";
        const string k_HandleClassName = k_ClassName + "__handle";
        const string k_HandleSelectedClassName = k_HandleClassName + "--selected";
        const string k_PreviewClassName = k_ClassName + "__preview";
        const string k_HeaderClassName = k_ClassName + "__header";
        const string k_HeaderTitleClassName = k_ClassName + "__header-title";
        const string k_RowClassName = k_ClassName + "__row";
        const string k_RowAffordanceClassName = k_ClassName + "__row-affordance";
        const string k_RowAffordanceColorClassName = k_RowAffordanceClassName + "--color";
        const string k_RowPositionClassName = k_ClassName + "__row-position";
        const string k_RowPositionVarClassName = k_RowPositionClassName + "--var";
        const string k_RowUnitClassName = k_ClassName + "__row-unit";
        const string k_RowColorClassName = k_ClassName + "__row-color";
        const string k_RowHexClassName = k_ClassName + "__row-hex";
        const string k_RowHexVarClassName = k_RowHexClassName + "--var";

        // A gradient stop plus the var() names bound to its color/position (null = unbound).
        // The stop always carries resolved values so the preview and handles can render.
        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal struct StopData
        {
            public BackgroundGradientStop stop;
            public string colorVarName;
            public string positionVarName;
        }

        readonly VisualElement m_HandlesTrack;
        readonly VisualElement m_PreviewBar;
        readonly ListView m_ListView;

        readonly List<StopHandle> m_Handles = new();
        readonly List<StopData> m_Stops = new();
        readonly StopsVariableContext m_VariableContext;

        public event Action<StopData[]> stopsChanged;

        // Host hook: maps a bound var name ("--name") to the StyleSheet that defines it, or
        // null when unresolved. Drives the per-row variable affordance icon and tooltip.
        public Func<string, StyleSheet> resolveVariableSheet { get; set; }

        // Host hook: resolves a bound var name to its current color value, or null when the
        // variable is undefined or not a color.
        public Func<string, Color?> resolveVariableColor { get; set; }

        // Host hooks for set-variable editing: the element whose resolved styles scope the
        // available variables, and whether editor-extension variables are offered. When the
        // element hook is unset the set/edit variable menu items are not shown.
        public Func<VisualElement> getCurrentVisualElement { get; set; }
        public Func<bool> getEditorExtensionMode { get; set; }

        public BackgroundGradientStopsEditor()
        {
            AddToClassList(k_ClassName);
            if (EditorGUIUtility.isProSkin)
                AddToClassList(k_DarkClassName);
            styleSheets.Add(EditorGUIUtility.Load(k_UssPath) as StyleSheet);
            // The variable field swap relies on .unity-inspector-hidden from VariableEditing.uss.
            styleSheets.Add(EditorGUIUtility.Load(k_VariableEditingUssPath) as StyleSheet);
            m_VariableContext = new StopsVariableContext(this);

            m_HandlesTrack = new VisualElement();
            m_HandlesTrack.AddToClassList(k_HandlesClassName);
            m_HandlesTrack.RegisterCallback<GeometryChangedEvent>(_ => PositionHandles());
            m_HandlesTrack.RegisterCallback<PointerDownEvent>(OnTrackPointerDown);
            Add(m_HandlesTrack);

            m_PreviewBar = new VisualElement();
            m_PreviewBar.AddToClassList(k_PreviewClassName);
            Add(m_PreviewBar);

            var headerRow = new VisualElement();
            headerRow.AddToClassList(k_HeaderClassName);

            var headerLabel = new Label($"Color stops (max {k_MaxStops})");
            headerLabel.AddToClassList(k_HeaderTitleClassName);
            headerRow.Add(headerLabel);

            Add(headerRow);

            m_ListView = new ListView
            {
                itemsSource = m_Stops,
                showAddRemoveFooter = true,
                reorderable = true,
                reorderMode = ListViewReorderMode.Animated,
                virtualizationMethod = CollectionVirtualizationMethod.FixedHeight,
                fixedItemHeight = k_RowHeight,
                selectionType = SelectionType.Single,
                showBorder = true,
                makeItem = MakeRow,
                bindItem = BindRow,
            };
            m_ListView.style.flexGrow = 1;
            m_ListView.overridingAddButtonBehavior = (_, _) => AddStop();
            m_ListView.onRemove = OnRemove;
            m_ListView.itemIndexChanged += OnItemIndexChanged;
            m_ListView.selectionChanged += _ => UpdateHandleSelection();
            var scrollView = m_ListView.Q<ScrollView>();
            if (scrollView != null)
                scrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            Add(m_ListView);
            style.flexGrow = 1;
        }

        public void SetStops(StopData[] stops)
        {
            m_Stops.Clear();
            if (stops != null)
                m_Stops.AddRange(stops);
            SortStopsIfNeeded();
            m_ListView.RefreshItems();
            RefreshPreview();
        }

        void SortStopsIfNeeded()
        {
            for (int i = 1; i < m_Stops.Count; ++i)
            {
                if (GetSortKey(m_Stops[i].stop) < GetSortKey(m_Stops[i - 1].stop))
                {
                    m_Stops.Sort((a, b) => GetSortKey(a.stop).CompareTo(GetSortKey(b.stop)));
                    return;
                }
            }
        }

        float GetSortKey(in BackgroundGradientStop stop)
        {
            if (stop.positionIsPercent)
                return stop.position;
            var trackWidth = m_HandlesTrack.layout.width;
            return trackWidth > 0 ? stop.position / trackWidth : stop.position;
        }

        // Re-inserts the stop at its sorted position and returns the new index. The matching
        // handle element follows the stop so a pointer capture held during a drag stays bound
        // to the stop being dragged.
        int MoveStopToSortedIndex(int index)
        {
            var data = m_Stops[index];
            var key = GetSortKey(data.stop);
            m_Stops.RemoveAt(index);

            var newIndex = 0;
            while (newIndex < m_Stops.Count && GetSortKey(m_Stops[newIndex].stop) <= key)
                ++newIndex;
            m_Stops.Insert(newIndex, data);

            if (newIndex != index && index < m_Handles.Count)
            {
                var handle = m_Handles[index];
                m_Handles.RemoveAt(index);
                m_Handles.Insert(newIndex, handle);
            }
            return newIndex;
        }

        VisualElement MakeRow()
        {
            var row = new StopRow();
            row.stopChanged += OnStopChanged;
            row.resolveVariableSheet = name => resolveVariableSheet?.Invoke(name);
            row.resolveVariableColor = name => resolveVariableColor?.Invoke(name);
            row.variableEditingContext = m_VariableContext;
            return row;
        }

        void BindRow(VisualElement element, int index)
        {
            var row = (StopRow)element;
            row.index = index;
            row.SetData(m_Stops[index]);
        }

        void OnStopChanged(int index, StopData data)
        {
            if (index < 0 || index >= m_Stops.Count)
                return;
            m_Stops[index] = data;

            var newIndex = MoveStopToSortedIndex(index);
            if (newIndex != index)
            {
                m_ListView.RefreshItems();
                if (m_ListView.selectedIndex == index)
                    m_ListView.selectedIndex = newIndex;
            }
            NotifyStopsChanged();
        }

        void AddStop()
        {
            if (m_Stops.Count >= k_MaxStops)
                return;

            var lastColor = m_Stops.Count > 0 ? m_Stops[m_Stops.Count - 1].stop.color : Color.white;
            m_Stops.Add(new StopData { stop = BackgroundGradientStop.Percent(lastColor, 1f) });

            m_ListView.RefreshItems();
            NotifyStopsChanged();
        }

        void OnRemove(BaseListView listView)
        {
            if (m_Stops.Count <= k_MinStops)
                return;

            var target = listView.selectedIndex;
            if (target < 0 || target >= m_Stops.Count)
                target = m_Stops.Count - 1;

            m_Stops.RemoveAt(target);

            m_ListView.RefreshItems();
            NotifyStopsChanged();
        }

        void OnItemIndexChanged(int previousIndex, int newIndex)
        {
            // ListView reordered m_Stops in place. Colors (and their var bindings) follow the
            // drag; positions (and theirs) stay in ascending visual order — the dropped stop
            // takes the position of the slot it landed in, and shifted stops carry their old
            // positions with them.
            var slots = new (float position, bool isPercent, string varName)[m_Stops.Count];
            for (int i = 0; i < m_Stops.Count; ++i)
                slots[i] = (m_Stops[i].stop.position, m_Stops[i].stop.positionIsPercent, m_Stops[i].positionVarName);
            Array.Sort(slots, (a, b) => a.position.CompareTo(b.position));
            for (int i = 0; i < m_Stops.Count; ++i)
            {
                var data = m_Stops[i];
                data.stop.position = slots[i].position;
                data.stop.positionIsPercent = slots[i].isPercent;
                data.positionVarName = slots[i].varName;
                m_Stops[i] = data;
            }
            m_ListView.RefreshItems();
            NotifyStopsChanged();
        }

        void NotifyStopsChanged()
        {
            RefreshPreview();
            stopsChanged?.Invoke(m_Stops.ToArray());
        }

        void RefreshPreview()
        {
            if (m_Stops.Count >= k_MinStops)
            {
                var stops = new BackgroundGradientStop[m_Stops.Count];
                for (int i = 0; i < m_Stops.Count; ++i)
                    stops[i] = m_Stops[i].stop;
                m_PreviewBar.style.backgroundImage = Background.FromGradient(BackgroundGradient.Linear(Mathf.PI / 2f, stops));
            }
            else
            {
                m_PreviewBar.style.backgroundImage = new StyleBackground(StyleKeyword.Null);
            }

            // Sync by count instead of rebuilding, so a handle being dragged keeps its pointer capture.
            while (m_Handles.Count < m_Stops.Count)
            {
                var handle = new StopHandle();
                handle.selected += OnHandleSelected;
                handle.dragged += OnHandleDragged;
                m_HandlesTrack.Add(handle);
                m_Handles.Add(handle);
            }
            while (m_Handles.Count > m_Stops.Count)
            {
                m_Handles[m_Handles.Count - 1].RemoveFromHierarchy();
                m_Handles.RemoveAt(m_Handles.Count - 1);
            }

            for (int i = 0; i < m_Handles.Count; ++i)
            {
                m_Handles[i].index = i;
                var c = m_Stops[i].stop.color;
                m_Handles[i].style.backgroundColor = new Color(c.r, c.g, c.b, 1f);
                m_Handles[i].MarkDirtyRepaint();
            }

            UpdateHandleSelection();
            PositionHandles();
        }

        void PositionHandles()
        {
            var trackWidth = m_HandlesTrack.layout.width;
            if (float.IsNaN(trackWidth) || trackWidth <= 0)
                return;

            var usable = Mathf.Max(0, trackWidth - k_HandleWidth);
            for (int i = 0; i < m_Handles.Count; ++i)
                m_Handles[i].style.left = GetStopFraction(m_Stops[i].stop, trackWidth) * usable;
        }

        static float GetStopFraction(in BackgroundGradientStop stop, float trackWidth)
        {
            var fraction = stop.positionIsPercent ? stop.position : stop.position / trackWidth;
            return Mathf.Clamp01(fraction);
        }

        void UpdateHandleSelection()
        {
            for (int i = 0; i < m_Handles.Count; ++i)
            {
                m_Handles[i].EnableInClassList(k_HandleSelectedClassName, i == m_ListView.selectedIndex);
                m_Handles[i].MarkDirtyRepaint();
            }
        }

        void OnHandleSelected(int index)
        {
            m_ListView.selectedIndex = index;
        }

        void OnHandleDragged(int index, float fraction)
        {
            if (index < 0 || index >= m_Stops.Count)
                return;

            var data = m_Stops[index];
            if (data.stop.positionIsPercent)
                data.stop.position = Mathf.Round(fraction * 100f) / 100f;
            else
                data.stop.position = Mathf.Round(fraction * m_HandlesTrack.layout.width);
            // A manual drag overrides whatever variable drove the position.
            data.positionVarName = null;
            m_Stops[index] = data;

            var newIndex = MoveStopToSortedIndex(index);
            if (newIndex != index)
            {
                m_ListView.RefreshItems();
                m_ListView.selectedIndex = newIndex;
            }
            else
            {
                m_ListView.RefreshItem(index);
            }
            NotifyStopsChanged();
        }

        void OnTrackPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0 || m_Stops.Count >= k_MaxStops)
                return;

            var usable = Mathf.Max(1f, m_HandlesTrack.layout.width - k_HandleWidth);
            var fraction = Mathf.Clamp01(((float)evt.localPosition.x - k_HandleWidth / 2f) / usable);
            fraction = Mathf.Round(fraction * 100f) / 100f;

            var newStop = new StopData { stop = BackgroundGradientStop.Percent(SampleColor(fraction), fraction) };
            var newIndex = 0;
            while (newIndex < m_Stops.Count && GetSortKey(m_Stops[newIndex].stop) <= fraction)
                ++newIndex;
            m_Stops.Insert(newIndex, newStop);

            m_ListView.RefreshItems();
            m_ListView.selectedIndex = newIndex;
            NotifyStopsChanged();
        }

        // Approximates the gradient color at the given fraction by lerping between the two
        // neighboring stops, to seed a newly inserted stop with an unobtrusive color.
        Color SampleColor(float fraction)
        {
            if (m_Stops.Count == 0)
                return Color.white;

            if (fraction <= GetSortKey(m_Stops[0].stop))
                return m_Stops[0].stop.color;

            for (int i = 1; i < m_Stops.Count; ++i)
            {
                var nextKey = GetSortKey(m_Stops[i].stop);
                if (fraction <= nextKey)
                {
                    var previousKey = GetSortKey(m_Stops[i - 1].stop);
                    var t = nextKey > previousKey ? (fraction - previousKey) / (nextKey - previousKey) : 0f;
                    return Color.Lerp(m_Stops[i - 1].stop.color, m_Stops[i].stop.color, t);
                }
            }
            return m_Stops[m_Stops.Count - 1].stop.color;
        }

        // Accepts "--name" or "var(--name)" and returns the "--name" form.
        internal static bool TryParseVarName(string text, out string varName)
        {
            varName = null;
            if (string.IsNullOrWhiteSpace(text))
                return false;

            var s = text.Trim();
            if (s.StartsWith("var(") && s.EndsWith(")"))
                s = s.Substring(4, s.Length - 5).Trim();

            if (s.Length <= 2 || !s.StartsWith("--"))
                return false;

            for (int i = 2; i < s.Length; ++i)
            {
                var c = s[i];
                if (!char.IsLetterOrDigit(c) && c != '-' && c != '_')
                    return false;
            }

            varName = s;
            return true;
        }

        internal static string ColorToHex(Color color) => "#" + ColorUtility.ToHtmlStringRGB(color);

        internal static bool TryParseHex(string text, out Color color)
        {
            color = Color.white;
            if (string.IsNullOrWhiteSpace(text))
                return false;

            // Users often type hex without the leading '#' that TryParseHtmlString requires.
            var s = text.Trim();
            return ColorUtility.TryParseHtmlString(s, out color)
                || ColorUtility.TryParseHtmlString("#" + s, out color);
        }

        sealed class StopHandle : VisualElement
        {
            float m_DragDelta;

            public int index { get; set; } = -1;

            public event Action<int> selected;
            public event Action<int, float> dragged;

            public StopHandle()
            {
                AddToClassList(k_HandleClassName);
                RegisterCallback<PointerDownEvent>(OnPointerDown);
                RegisterCallback<PointerMoveEvent>(OnPointerMove);
                RegisterCallback<PointerUpEvent>(OnPointerUp);
                generateVisualContent += OnGenerateVisualContent;
            }

            void OnGenerateVisualContent(MeshGenerationContext mgc)
            {
                const float arrowHalfWidth = 4f;
                const float arrowHeight = 4f;

                var centerX = layout.width / 2f;
                // Start slightly above the bottom edge so the fill tucks under the square's border.
                var top = layout.height - 1f;

                var painter = mgc.painter2D;
                painter.fillColor = resolvedStyle.backgroundColor;
                painter.strokeColor = resolvedStyle.borderBottomColor;
                painter.lineWidth = 1f;

                painter.BeginPath();
                painter.MoveTo(new Vector2(centerX - arrowHalfWidth, top));
                painter.LineTo(new Vector2(centerX, top + arrowHeight));
                painter.LineTo(new Vector2(centerX + arrowHalfWidth, top));
                painter.Fill();
                // The path is left open on purpose: only the two diagonal edges get stroked,
                // avoiding a horizontal line where the arrow meets the square.
                painter.Stroke();
            }

            void OnPointerDown(PointerDownEvent evt)
            {
                var trackLocal = parent.WorldToLocal((Vector2)evt.position);
                m_DragDelta = trackLocal.x - (layout.x + k_HandleWidth / 2f);
                this.CapturePointer(evt.pointerId);
                selected?.Invoke(index);
                evt.StopPropagation();
            }

            void OnPointerMove(PointerMoveEvent evt)
            {
                if (!this.HasPointerCapture(evt.pointerId))
                    return;

                var trackLocal = parent.WorldToLocal((Vector2)evt.position);
                var usable = Mathf.Max(1f, parent.layout.width - k_HandleWidth);
                var centerX = trackLocal.x - m_DragDelta;
                var fraction = Mathf.Clamp01((centerX - k_HandleWidth / 2f) / usable);
                dragged?.Invoke(index, fraction);
                evt.StopPropagation();
            }

            void OnPointerUp(PointerUpEvent evt)
            {
                if (this.HasPointerCapture(evt.pointerId))
                    this.ReleasePointer(evt.pointerId);
            }
        }

        sealed class StopRow : VisualElement
        {
            readonly FieldAffordanceElement m_PositionAffordance;
            readonly FloatField m_PositionField;
            readonly Label m_UnitLabel;
            readonly FieldAffordanceElement m_ColorAffordance;
            readonly ColorField m_ColorField;
            readonly TextField m_HexField;

            StopData m_Data;
            bool m_Suppress;
            VariableEditingHandler m_ColorVarHandler;

            public int index { get; set; } = -1;

            public Func<string, StyleSheet> resolveVariableSheet { get; set; }

            public Func<string, Color?> resolveVariableColor { get; set; }

            public IVariableEditingContext variableEditingContext { get; set; }

            public event Action<int, StopData> stopChanged;

            public StopRow()
            {
                AddToClassList(k_RowClassName);

                m_PositionAffordance = MakeAffordance(isColor: false);
                Add(m_PositionAffordance);

                m_PositionField = new FloatField { isDelayed = true };
                m_PositionField.AddToClassList(k_RowPositionClassName);
                m_PositionField.RegisterValueChangedCallback(OnPositionChanged);
                Add(m_PositionField);

                m_UnitLabel = new Label();
                m_UnitLabel.AddToClassList(k_RowUnitClassName);
                Add(m_UnitLabel);

                m_ColorAffordance = MakeAffordance(isColor: true);
                m_ColorAffordance.AddToClassList(k_RowAffordanceColorClassName);
                Add(m_ColorAffordance);

                m_ColorField = new ColorField { showAlpha = true };
                m_ColorField.AddToClassList(k_RowColorClassName);
                m_ColorField.RegisterValueChangedCallback(OnColorChanged);
                Add(m_ColorField);

                m_HexField = new TextField { isDelayed = true };
                m_HexField.AddToClassList(k_RowHexClassName);
                m_HexField.RegisterValueChangedCallback(OnHexChanged);
                Add(m_HexField);
            }

            FieldAffordanceElement MakeAffordance(bool isColor)
            {
                var affordance = new FieldAffordanceElement();
                affordance.AddToClassList(k_RowAffordanceClassName);
                affordance.fieldAffordanceData.type = FieldAffordanceDataType.USSProperty;
                affordance.populateMenuItems = menu => PopulateVarMenu(menu, isColor);
                return affordance;
            }

            void PopulateVarMenu(DropdownMenu menu, bool isColor)
            {
                var varName = isColor ? m_Data.colorVarName : m_Data.positionVarName;

                if (isColor && variableEditingContext?.CurrentVisualElement != null)
                {
                    if (varName == null)
                    {
                        menu.AppendAction("Set variable", _ => ShowColorVariableField());
                    }
                    else
                    {
                        menu.AppendAction("Edit variable", _ => ShowColorVariableField());
                        menu.AppendAction("Remove variable", _ => UnsetVariable(isColor: true));
                    }
                    return;
                }

                if (varName == null)
                    return;
                menu.AppendAction($"Remove variable ({varName})", _ => UnsetVariable(isColor));
            }

            // Swaps the hex field for the standard variable field with name completion; on
            // commit the context routes back to BindColorVariable/UnsetColorVariable.
            void ShowColorVariableField()
            {
                if (variableEditingContext == null)
                    return;

                m_ColorVarHandler ??= new VariableEditingHandler(m_HexField, variableEditingContext) { styleName = "color" };
                m_ColorVarHandler.index = index;
                m_ColorVarHandler.ShowVariableField();
            }

            internal void BindColorVariable(string variableName)
            {
                if (!TryParseVarName(variableName, out var varName))
                    return;
                m_Data.colorVarName = varName;
                ApplyResolvedVariableColor();
                UpdateHexDisplay();
                stopChanged?.Invoke(index, m_Data);
            }

            // Mirrors the variable's current color into the stop so the swatch and preview
            // update immediately; the written USS keeps the var reference, not this color.
            void ApplyResolvedVariableColor()
            {
                var resolved = resolveVariableColor?.Invoke(m_Data.colorVarName);
                if (resolved == null)
                    return;
                m_Data.stop.color = resolved.Value;
                m_ColorField.SetValueWithoutNotify(resolved.Value);
            }

            internal void UnsetColorVariable()
            {
                UnsetVariable(isColor: true);
            }

            void UnsetVariable(bool isColor)
            {
                if (isColor)
                {
                    m_Data.colorVarName = null;
                    UpdateHexDisplay();
                }
                else
                {
                    m_Data.positionVarName = null;
                    UpdatePositionVarDisplay();
                }
                stopChanged?.Invoke(index, m_Data);
            }

            void UpdateAffordance(FieldAffordanceElement affordance, string varName)
            {
                var data = affordance.fieldAffordanceData;
                if (varName != null)
                {
                    data.inlineValue = varName;
                    data.variableSheet = resolveVariableSheet?.Invoke(varName);
                    data.sourceTypeInfo = FieldAffordanceSourceInfoType.USSVariable;
                }
                else
                {
                    data.inlineValue = null;
                    data.variableSheet = null;
                    data.sourceTypeInfo = FieldAffordanceSourceInfoType.Default;
                }
            }

            public void SetData(StopData data)
            {
                m_Data = data;
                m_Suppress = true;
                try
                {
                    var stop = data.stop;
                    m_PositionField.SetValueWithoutNotify(stop.positionIsPercent ? stop.position * 100f : stop.position);
                    m_UnitLabel.text = stop.positionIsPercent ? "%" : "px";
                    m_ColorField.SetValueWithoutNotify(stop.color);
                    UpdatePositionVarDisplay();
                    UpdateHexDisplay();
                }
                finally
                {
                    m_Suppress = false;
                }
            }

            void UpdatePositionVarDisplay()
            {
                bool bound = m_Data.positionVarName != null;
                m_PositionField.EnableInClassList(k_RowPositionVarClassName, bound);
                m_PositionField.tooltip = bound ? $"Bound to var({m_Data.positionVarName})" : null;
                // No set-variable UI for positions, so an unbound affordance has no menu;
                // collapse it instead of opening an empty dropdown.
                m_PositionAffordance.style.display = bound ? DisplayStyle.Flex : DisplayStyle.None;
                UpdateAffordance(m_PositionAffordance, m_Data.positionVarName);
            }

            void UpdateHexDisplay()
            {
                bool bound = m_Data.colorVarName != null;
                // Match inspector style fields: a resolved variable makes the inputs read-only
                // and edits go through the affordance menu; an unresolved one stays editable.
                bool resolved = bound && resolveVariableSheet?.Invoke(m_Data.colorVarName) != null;
                m_HexField.SetValueWithoutNotify(ColorToHex(m_Data.stop.color));
                m_HexField.EnableInClassList(k_RowHexVarClassName, bound);
                m_HexField.SetEnabled(!resolved);
                m_ColorField.SetEnabled(!resolved);
                m_HexField.tooltip = bound
                    ? $"Bound to var({m_Data.colorVarName})"
                    : "Hex color, or --name / var(--name) to bind a variable";
                m_ColorAffordance.visible = bound || variableEditingContext?.CurrentVisualElement != null;
                UpdateAffordance(m_ColorAffordance, m_Data.colorVarName);
            }

            void OnPositionChanged(ChangeEvent<float> evt)
            {
                if (m_Suppress)
                    return;

                if (m_Data.stop.positionIsPercent)
                {
                    var percent = Mathf.Clamp(evt.newValue, 0f, 100f);
                    m_PositionField.SetValueWithoutNotify(percent);
                    m_Data.stop.position = percent / 100f;
                }
                else
                {
                    m_Data.stop.position = evt.newValue;
                }
                m_Data.positionVarName = null;
                UpdatePositionVarDisplay();
                stopChanged?.Invoke(index, m_Data);
            }

            void OnColorChanged(ChangeEvent<Color> evt)
            {
                if (m_Suppress)
                    return;

                m_Data.stop.color = evt.newValue;
                m_Data.colorVarName = null;
                UpdateHexDisplay();
                stopChanged?.Invoke(index, m_Data);
            }

            void OnHexChanged(ChangeEvent<string> evt)
            {
                if (m_Suppress)
                    return;

                if (TryParseVarName(evt.newValue, out var varName))
                {
                    m_Data.colorVarName = varName;
                    ApplyResolvedVariableColor();
                    UpdateHexDisplay();
                    stopChanged?.Invoke(index, m_Data);
                }
                else if (TryParseHex(evt.newValue, out var parsed))
                {
                    // Preserve the existing alpha; the hex field is RGB-only.
                    parsed.a = m_Data.stop.color.a;
                    m_Data.stop.color = parsed;
                    m_Data.colorVarName = null;
                    m_ColorField.SetValueWithoutNotify(parsed);
                    UpdateHexDisplay();
                    stopChanged?.Invoke(index, m_Data);
                }
                else
                {
                    UpdateHexDisplay();
                }
            }
        }

        // Adapts the shared variable editing stack (VariableEditingHandler + VariableCompleter)
        // to stop rows: the edited "property" is a stop's color var binding stored in StopData,
        // not a style rule, so rule/sheet accessors return null and writes route to the row.
        sealed class StopsVariableContext : IVariableEditingContext
        {
            readonly BackgroundGradientStopsEditor m_Editor;

            public StopsVariableContext(BackgroundGradientStopsEditor editor)
            {
                m_Editor = editor;
            }

            public VisualElement CurrentVisualElement => m_Editor.getCurrentVisualElement?.Invoke();
            public StyleRule CurrentRule => null;
            public StyleSheet CurrentStyleSheet => null;
            public bool EditorExtensionMode => m_Editor.getEditorExtensionMode?.Invoke() ?? false;
            public bool IsSelectorElement => false;
            public VisualElement TooltipRoot => m_Editor;

            public void SetVariable(string variableName, BindableElement field, string styleName, int index)
            {
                field.GetFirstAncestorOfType<StopRow>()?.BindColorVariable(variableName);
            }

            public void UnsetVariable(BindableElement field, string styleName)
            {
                field.GetFirstAncestorOfType<StopRow>()?.UnsetColorVariable();
            }

            public void RefreshUI()
            {
                // Rows refresh themselves when their binding changes.
            }

            public string GetBoundVariableNameFromCurrentRule(string styleName, int index)
            {
                return index >= 0 && index < m_Editor.m_Stops.Count ? m_Editor.m_Stops[index].colorVarName : null;
            }

            public string GetBoundVariableNameFromMatchedRules(string styleName, int index)
            {
                return null;
            }
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
