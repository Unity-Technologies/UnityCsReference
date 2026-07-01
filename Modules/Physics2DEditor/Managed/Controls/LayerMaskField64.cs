// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text;

using UnityEngine;
using UnityEngine.LowLevelPhysics2D;

namespace UnityEditor.LowLevelPhysics2D
{
    // IMGUI 64-bit ("ulong") sibling of EditorGUI.LayerMaskField, used by the property drawers when
    // Physics2D is showing 64-bit layers (no native Mask64Field exists for IMGUI).
    //
    // Public Editor API only — no UnityEditor internals beyond what the drawers already use. Operates
    // directly on the PhysicsMask.bitMask SerializedProperty so undo and multi-object editing work.
    // Choices come from PhysicsLayers, exactly as the UITK Mask64Field path selects them:
    //   showAsPhysicsMask -> GetBitNamesAndMasks  (raw bits "0".."63")
    //   otherwise         -> GetLayerNamesAndMasks (named layers, sparse)
    internal static class LayerMaskField64
    {
        const string k_Nothing = "Nothing";
        const string k_Everything = "Everything";
        const string k_Mixed = "Mixed...";

        // Reusable buffers for the per-frame summary; the popup takes its own copies.
        static readonly List<string> s_Names = new List<string>(64);
        static readonly List<ulong> s_Masks = new List<ulong>(64);

        internal static void Draw(Rect position, GUIContent label, SerializedProperty bitMaskProperty, bool showAsPhysicsMask)
        {
            GetNamesAndMasks(showAsPhysicsMask, s_Names, s_Masks);

            var controlId = GUIUtility.GetControlID(FocusType.Keyboard, position);
            var fieldRect = EditorGUI.PrefixLabel(position, controlId, label);

            GUIContent content;
            if (bitMaskProperty.hasMultipleDifferentValues)
                content = EditorGUI.mixedValueContent;
            else
                content = new GUIContent(BuildSummary(bitMaskProperty.ulongValue, s_Names, s_Masks, fieldRect.width, EditorStyles.popup));

            if (EditorGUI.DropdownButton(fieldRect, content, FocusType.Keyboard, EditorStyles.popup))
                PopupWindow.Show(fieldRect, new LayerMaskField64Popup(bitMaskProperty, s_Names, s_Masks));
        }

        internal static void GetNamesAndMasks(bool showAsPhysicsMask, List<string> names, List<ulong> masks)
        {
            if (showAsPhysicsMask)
                PhysicsLayers.GetBitNamesAndMasks(names, masks);
            else
                PhysicsLayers.GetLayerNamesAndMasks(names, masks);
        }

        // OR of all per-choice masks (excludes the ~0 "everything" sentinel). Mirrors Mask64Field.m_FullChoiceMask:
        // "everything" is the union of the choices, not necessarily ~0.
        internal static ulong FullMask(IList<ulong> masks)
        {
            ulong full = 0;
            for (var i = 0; i < masks.Count; i++)
            {
                var m = masks[i];
                if (m != ~0UL)
                    full |= m;
            }
            return full;
        }

        // Collapse to ~0 when every choice bit is set, otherwise clamp to the valid choice bits.
        internal static ulong Normalize(ulong value, ulong fullMask)
        {
            if (fullMask == 0)
                return value;
            return (value & fullMask) == fullMask ? ~0UL : value & fullMask;
        }

        // Button summary: Nothing / Everything / single-name / comma-joined, falling back to "Mixed..."
        // when the joined names won't fit the field. Widened from MaskFieldGUI.GetMaskButtonValue + Mask64Field.GetMixedString.
        static string BuildSummary(ulong value, IList<string> names, IList<ulong> masks, float width, GUIStyle style)
        {
            var full = FullMask(masks);
            var intermediate = value & full;

            if (intermediate == 0)
                return k_Nothing;
            if (full != 0 && intermediate == full)
                return k_Everything;

            string single = null;
            var count = 0;
            var sb = new StringBuilder();
            for (var i = 0; i < masks.Count; i++)
            {
                var m = masks[i];
                if (m == 0 || m == ~0UL || (value & m) != m)
                    continue;

                if (count == 0)
                    single = names[i];
                if (sb.Length > 0)
                    sb.Append(", ");
                sb.Append(names[i]);
                count++;
            }

            if (count == 0)
                return k_Mixed;
            if (count == 1)
                return single;

            var joined = sb.ToString();
            // Fall back to "Mixed..." only when the joined names genuinely overflow the field
            // (popup style's CalcSize already accounts for its padding + dropdown arrow).
            if (style != null && style.CalcSize(new GUIContent(joined)).x > width)
                return k_Mixed;
            return joined;
        }
    }

    internal sealed class LayerMaskField64Popup : PopupWindowContent
    {
        static class Styles
        {
            internal static readonly GUIStyle menuItem = new GUIStyle("MenuItem");
        }

        const float k_VerticalPadding = 4f;
        const float k_ScrollBarWidth = 14f;
        // Max rows shown before scrolling — 32 layers + Nothing + Everything, matching the native MaskField.
        // A fixed cap (rather than a screen-height fraction) keeps the popup short enough to always anchor
        // below/above the field; only the dense 64-bit bit-mode list scrolls past this.
        const int k_MaxVisibleRows = 34;

        // Keep targets + path so the property can be re-resolved if it goes invalid while open (UUM-72761 pattern).
        SerializedProperty m_Property;
        readonly UnityEngine.Object[] m_Targets;
        readonly string m_PropertyPath;

        readonly List<string> m_Names;
        readonly List<ulong> m_Masks;
        readonly ulong m_FullMask;

        readonly float m_Width;
        readonly float m_Height;
        readonly bool m_Scrolls;
        Vector2 m_Scroll;

        const string k_Nothing = "Nothing";
        const string k_Everything = "Everything";

        static float RowHeight => EditorGUIUtility.singleLineHeight + 2f;
        int RowCount => m_Names.Count + 2; // Nothing + Everything + per-choice.

        public LayerMaskField64Popup(SerializedProperty bitMaskProperty, IList<string> names, IList<ulong> masks)
        {
            m_Property = bitMaskProperty;
            m_Targets = bitMaskProperty.serializedObject.targetObjects;
            m_PropertyPath = bitMaskProperty.propertyPath;

            m_Names = new List<string>(names);
            m_Masks = new List<ulong>(masks);
            m_FullMask = LayerMaskField64.FullMask(m_Masks);

            // Show every row until the list exceeds the cap, then scroll. Width matches MaskFieldDropDown:
            // widest entry, clamped [100, 95% screen], plus the scrollbar when scrolling.
            m_Scrolls = RowCount > k_MaxVisibleRows;
            var visibleRows = Mathf.Min(RowCount, k_MaxVisibleRows);
            m_Height = RowHeight * visibleRows + k_VerticalPadding * 2f;

            var w = Styles.menuItem.CalcSize(new GUIContent(k_Everything)).x;
            for (var i = 0; i < m_Names.Count; i++)
                w = Mathf.Max(w, Styles.menuItem.CalcSize(new GUIContent(m_Names[i])).x);
            if (m_Scrolls)
                w += k_ScrollBarWidth;
            m_Width = Mathf.Clamp(w, 100f, Screen.currentResolution.width * 0.95f);
        }

        public override Vector2 GetWindowSize() => new Vector2(m_Width, m_Height);

        public override void OnGUI(Rect rect)
        {
            var e = Event.current;
            if (e.type == EventType.MouseMove)
                e.Use();
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                editorWindow.Close();
                GUIUtility.ExitGUI();
            }

            var value = CurrentValue;

            var contentWidth = rect.width - (m_Scrolls ? k_ScrollBarWidth : 0f);
            var viewRect = new Rect(0f, 0f, contentWidth, RowHeight * RowCount);
            var clipRect = new Rect(rect.x, rect.y + k_VerticalPadding, rect.width, rect.height - k_VerticalPadding * 2f);

            // Never show a horizontal scrollbar; vertical only when the list overflows.
            m_Scroll = GUI.BeginScrollView(clipRect, m_Scroll, viewRect, GUIStyle.none, m_Scrolls ? GUI.skin.verticalScrollbar : GUIStyle.none);

            var y = 0f;
            var intermediate = value & m_FullMask;
            DrawRow(ref y, contentWidth, k_Nothing, intermediate == 0, () => SetValue(0));
            DrawRow(ref y, contentWidth, k_Everything, m_FullMask != 0 && intermediate == m_FullMask, () => SetValue(~0UL));

            for (var i = 0; i < m_Names.Count; i++)
            {
                var bit = m_Masks[i];
                var isOn = bit != 0 && (value & bit) == bit;
                DrawRow(ref y, contentWidth, m_Names[i], isOn, () =>
                {
                    var v = CurrentValue;
                    if ((v & bit) == bit)
                        v &= ~bit;
                    else
                        v |= bit;
                    SetValue(LayerMaskField64.Normalize(v, m_FullMask));
                });
            }

            GUI.EndScrollView();
        }

        void DrawRow(ref float y, float width, string label, bool isOn, Action onToggle)
        {
            var rowRect = new Rect(0f, y, width, EditorGUIUtility.singleLineHeight);
            EditorGUI.BeginChangeCheck();
            GUI.Toggle(rowRect, isOn, label, Styles.menuItem);
            if (EditorGUI.EndChangeCheck())
                onToggle();
            y += RowHeight;
        }

        ulong CurrentValue
        {
            get
            {
                EnsureValid();
                return m_Property.ulongValue;
            }
        }

        void SetValue(ulong value)
        {
            EnsureValid();
            m_Property.ulongValue = value;
            m_Property.serializedObject.ApplyModifiedProperties();
            editorWindow?.Repaint();
        }

        void EnsureValid()
        {
            if (m_Property != null && m_Property.isValid)
                return;

            var serializedObject = new SerializedObject(m_Targets);
            m_Property = serializedObject.FindProperty(m_PropertyPath);
        }
    }
}
