// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.U2D.Physics.Editor
{
    /// <summary>
    /// Shared layout for every joint definition, turning a flat run of serialized fields into a small number of collapsible groups.
    /// A concrete drawer supplies only its own group table; this class owns the drawing, the group titles and the field rules, in both UI Toolkit and IMGUI.
    /// Serialized order is preserved: each group covers a contiguous run of fields, and only the fields within one group may be reordered.
    /// </summary>
    abstract class PhysicsJointDefinitionPropertyDrawer : PropertyDrawer
    {
        /// <summary>
        /// One row of a definition's layout.
        /// A title makes the group a collapsible foldout; no title draws its fields loose at the panel's own level.
        /// A state field names the toggle that switches the group on: it draws first and the remaining fields disable while it is off.
        /// </summary>
        protected readonly struct Group
        {
            public Group(string title, string stateField, params string[] fields)
            {
                this.title = title;
                this.stateField = stateField;
                this.fields = fields;
                stateHides = false;
                anchors = false;
            }

            Group(string title, string stateField, string[] fields, bool stateHides, bool anchors)
            {
                this.title = title;
                this.stateField = stateField;
                this.fields = fields;
                this.stateHides = stateHides;
                this.anchors = anchors;
            }

            /// <summary>
            /// A flag and the fields it replaces, which are hidden rather than disabled while it is set.
            /// Used where the engine computes the value itself and the stored one is not used, so showing it disabled would imply it is still in effect.
            /// </summary>
            public static Group HiddenWhenSet(string stateField, params string[] fields) => new Group(null, stateField, fields, true, false);

            /// <summary>
            /// The anchors group, in which each anchor is drawn as its own foldout carrying its auto flag as the first row.
            /// The flag belongs to the definition rather than to the anchor, so the group builds those foldouts itself.
            /// </summary>
            public static Group Anchors(string localAnchorA, string localAnchorB) => new Group(k_AnchorsTitle, null, new[] { localAnchorA, localAnchorB }, false, true);

            public readonly string title;
            public readonly string stateField;
            public readonly string[] fields;
            public readonly bool stateHides;
            public readonly bool anchors;
        }

        /// <summary>
        /// The layout for this definition, in serialized order.
        /// </summary>
        protected abstract Group[] groups { get; }

        /// <summary>
        /// The serialized names of the two anchor fields and their auto flags, or null when the definition has no anchors.
        /// The anchors group pairs each flag with the anchor it resolves, so the names cannot be inferred from the group table alone.
        /// </summary>
        protected virtual (string localAnchorA, string autoAnchorA, string localAnchorB, string autoAnchorB)? anchorFields => null;

        /// <summary>
        /// The serialized name of the auto-axis flag and whether the joint reads anchor B's authored rotation, or null for a joint with no slide axis.
        /// While the flag is set the engine derives the affected rotations, so their rows hide: anchor A's always, anchor B's when the joint reads it (the slider bakes the axis into both frames).
        /// A joint that never reads anchor B's rotation at all (the wheel, which spins freely) never shows that row.
        /// </summary>
        protected virtual (string autoAxisField, bool usesRotationB)? axisFields => null;

        #region UITK

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();

            // The definition is a property like any other here, so it carries its own name and collapses, matching every other struct drawn in an inspector.
            // An inspector that has already named it embeds the fields instead (see PhysicsJointDefinitionInspector).
            var foldout = new Foldout
            {
                text = property.displayName,
                value = false,
                viewDataKey = GetType().ToString()
            };
            root.Add(foldout);
            foldout.Add(CreateFields(property));

            return root;
        }

        /// <summary>
        /// Create the grouped fields for this definition with no wrapping foldout, for an inspector that supplies its own heading.
        /// </summary>
        internal VisualElement CreateFields(SerializedProperty property)
        {
            VerifyCoverage(property);

            var body = new VisualElement();

            foreach (var group in groups)
                body.Add(CreateGroup(property, group));

            return body;
        }

        VisualElement CreateGroup(SerializedProperty property, Group group)
        {
            if (group.anchors)
                return CreateAnchors(property, group);

            var titled = !string.IsNullOrEmpty(group.title);
            var container = titled ? CreateFoldout(group.title) : new VisualElement();

            var stateProperty = string.IsNullOrEmpty(group.stateField) ? null : property.FindPropertyRelative(group.stateField);
            if (stateProperty != null)
                container.Add(new PropertyField(stateProperty));

            // The fields the state toggle switches on, held together so they disable as one without touching the toggle above them.
            var dependents = new VisualElement();
            container.Add(dependents);

            foreach (var fieldName in group.fields)
            {
                var fieldProperty = property.FindPropertyRelative(fieldName);
                if (fieldProperty == null)
                    continue;

                dependents.Add(new PropertyField(fieldProperty));
            }

            if (stateProperty != null)
            {
                if (group.stateHides)
                {
                    dependents.style.display = Hides(stateProperty) ? DisplayStyle.None : DisplayStyle.Flex;
                    dependents.TrackPropertyValue(stateProperty, changed => dependents.style.display = Hides(changed) ? DisplayStyle.None : DisplayStyle.Flex);
                }
                else
                {
                    dependents.SetEnabled(Enables(stateProperty));
                    dependents.TrackPropertyValue(stateProperty, changed => dependents.SetEnabled(Enables(changed)));
                }
            }

            return container;
        }

        VisualElement CreateAnchors(SerializedProperty property, Group group)
        {
            var container = CreateFoldout(group.title);

            foreach (var anchorField in group.fields)
            {
                var anchor = CreateAnchor(property, anchorField);
                if (anchor != null)
                    container.Add(anchor);
            }

            return container;
        }

        // One anchor: its own foldout with the auto flag as the first row, and the anchor's own fields below it.
        // Those fields hide while the flag is set because the engine recomputes the anchor when the joint is created and the stored values are not used.
        // The rotation row additionally hides while the joint does not read this frame's authored rotation (see axisFields).
        VisualElement CreateAnchor(SerializedProperty property, string anchorField)
        {
            var anchorProperty = property.FindPropertyRelative(anchorField);
            var autoProperty = property.FindPropertyRelative(AutoAnchorFor(anchorField));
            if (anchorProperty == null || autoProperty == null)
                return null;

            var foldout = new Foldout
            {
                text = anchorProperty.displayName,
                value = false,
                tooltip = L10n.Tr(Tooltips.anchor, null),
                viewDataKey = GetType() + "." + anchorField
            };

            // Labelled without the A or B because the anchor's own foldout already says which one this is.
            foldout.Add(new PropertyField(autoProperty, L10n.Tr("Auto", null)));

            var body = new VisualElement();
            foldout.Add(body);

            var axis = axisFields;
            var isA = anchorField == anchorFields.Value.localAnchorA;
            var rotationUnused = axis.HasValue && !isA && !axis.Value.usesRotationB;
            var autoAxisProperty = axis.HasValue ? property.FindPropertyRelative(axis.Value.autoAxisField) : null;
            var rotationDerived = autoAxisProperty != null && (isA || axis.Value.usesRotationB);

            foreach (var child in Children(anchorProperty))
            {
                if (child.name == k_RotationField && rotationUnused)
                    continue;

                var childField = new PropertyField(child);
                body.Add(childField);

                if (child.name == k_RotationField && rotationDerived)
                {
                    childField.style.display = Hides(autoAxisProperty) ? DisplayStyle.None : DisplayStyle.Flex;
                    childField.TrackPropertyValue(autoAxisProperty, changed => childField.style.display = Hides(changed) ? DisplayStyle.None : DisplayStyle.Flex);
                }
            }

            body.style.display = Hides(autoProperty) ? DisplayStyle.None : DisplayStyle.Flex;
            body.TrackPropertyValue(autoProperty, changed => body.style.display = Hides(changed) ? DisplayStyle.None : DisplayStyle.Flex);

            return foldout;
        }

        Foldout CreateFoldout(string title)
        {
            return new Foldout
            {
                text = L10n.Tr(title, null),
                value = false,
                tooltip = Tooltips.For(title),
                viewDataKey = GetType() + "." + title
            };
        }

        #endregion

        #region IMGUI

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
                return EditorGUIUtility.singleLineHeight;

            return LineHeight + Layout(property, Rect.zero, false);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label = EditorGUI.BeginProperty(position, label, property);

            var header = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(header, property.isExpanded, label, true);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                Layout(property, new Rect(position.x, position.y + LineHeight, position.width, position.height - LineHeight), true);
                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        // The single IMGUI pass, used both to measure and to draw so the two can never describe different layouts.
        // A group hangs its expansion on the first property it owns, so nothing is stored outside the serialized data.
        float Layout(SerializedProperty property, Rect position, bool draw)
        {
            VerifyCoverage(property);

            var y = position.y;

            foreach (var group in groups)
            {
                if (group.anchors)
                {
                    y += LayoutAnchors(property, position, y, group, draw);
                    continue;
                }

                var stateProperty = string.IsNullOrEmpty(group.stateField) ? null : property.FindPropertyRelative(group.stateField);
                var titled = !string.IsNullOrEmpty(group.title);

                if (titled)
                {
                    var expansionProperty = stateProperty ?? property.FindPropertyRelative(group.fields[0]);
                    if (expansionProperty == null)
                        continue;

                    if (draw)
                        expansionProperty.isExpanded = EditorGUI.Foldout(Line(position, y), expansionProperty.isExpanded, new GUIContent(L10n.Tr(group.title, null), Tooltips.For(group.title)), true);

                    y += LineHeight;

                    if (!expansionProperty.isExpanded)
                        continue;

                    if (draw)
                        EditorGUI.indentLevel++;
                }

                if (stateProperty != null)
                {
                    if (draw)
                        EditorGUI.PropertyField(Row(position, y, stateProperty), stateProperty, true);

                    y += EditorGUI.GetPropertyHeight(stateProperty, true) + EditorGUIUtility.standardVerticalSpacing;
                }

                // The fields a hiding flag replaces are left out entirely while it is set, so they take no space either.
                var hidden = group.stateHides && stateProperty != null && Hides(stateProperty);
                var disabled = !group.stateHides && stateProperty != null && !Enables(stateProperty);

                if (draw && disabled)
                    EditorGUI.BeginDisabledGroup(true);

                foreach (var fieldName in group.fields)
                {
                    if (hidden)
                        continue;

                    var fieldProperty = property.FindPropertyRelative(fieldName);
                    if (fieldProperty == null)
                        continue;

                    if (draw)
                        EditorGUI.PropertyField(Row(position, y, fieldProperty), fieldProperty, true);

                    y += EditorGUI.GetPropertyHeight(fieldProperty, true) + EditorGUIUtility.standardVerticalSpacing;
                }

                if (draw)
                {
                    if (disabled)
                        EditorGUI.EndDisabledGroup();

                    if (titled)
                        EditorGUI.indentLevel--;
                }
            }

            return y - position.y;
        }

        float LayoutAnchors(SerializedProperty property, Rect position, float top, Group group, bool draw)
        {
            var y = top;
            var expansionProperty = property.FindPropertyRelative(group.fields[0]);
            if (expansionProperty == null)
                return 0f;

            // The group and the first anchor both need an expansion flag and the group owns no property of its own, so the group takes the auto flag's.
            var groupExpansionProperty = property.FindPropertyRelative(AutoAnchorFor(group.fields[0]));
            if (groupExpansionProperty == null)
                return 0f;

            if (draw)
                groupExpansionProperty.isExpanded = EditorGUI.Foldout(Line(position, y), groupExpansionProperty.isExpanded, new GUIContent(L10n.Tr(group.title, null), Tooltips.For(group.title)), true);

            y += LineHeight;

            if (!groupExpansionProperty.isExpanded)
                return y - top;

            if (draw)
                EditorGUI.indentLevel++;

            foreach (var anchorField in group.fields)
            {
                var anchorProperty = property.FindPropertyRelative(anchorField);
                var autoProperty = property.FindPropertyRelative(AutoAnchorFor(anchorField));
                if (anchorProperty == null || autoProperty == null)
                    continue;

                if (draw)
                    anchorProperty.isExpanded = EditorGUI.Foldout(Line(position, y), anchorProperty.isExpanded, new GUIContent(anchorProperty.displayName, L10n.Tr(Tooltips.anchor, null)), true);

                y += LineHeight;

                if (!anchorProperty.isExpanded)
                    continue;

                if (draw)
                {
                    EditorGUI.indentLevel++;
                    EditorGUI.PropertyField(Row(position, y, autoProperty), autoProperty, new GUIContent(L10n.Tr("Auto", null)), true);
                }

                y += EditorGUI.GetPropertyHeight(autoProperty, true) + EditorGUIUtility.standardVerticalSpacing;

                if (!Hides(autoProperty))
                {
                    foreach (var child in Children(anchorProperty))
                    {
                        if (child.name == k_RotationField && RotationRowHidden(property, anchorField))
                            continue;

                        if (draw)
                            EditorGUI.PropertyField(Row(position, y, child), child, true);

                        y += EditorGUI.GetPropertyHeight(child, true) + EditorGUIUtility.standardVerticalSpacing;
                    }
                }

                if (draw)
                    EditorGUI.indentLevel--;
            }

            if (draw)
                EditorGUI.indentLevel--;

            return y - top;
        }

        static Rect Line(Rect position, float y)
        {
            return new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight);
        }

        static Rect Row(Rect position, float y, SerializedProperty property)
        {
            return new Rect(position.x, y, position.width, EditorGUI.GetPropertyHeight(property, true));
        }

        static float LineHeight
        {
            get { return EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing; }
        }

        #endregion

        #region Shared

        /// <summary>
        /// The layout order every joint definition shares: the loose rows first, then the foldouts, each keeping the order the concrete drawer declared them in.
        /// A loose row is a field the joint is defined by, so it reads before any group rather than stranded below them.
        /// </summary>
        protected static Group[] Order(params Group[] all)
        {
            var ordered = new List<Group>(all.Length);

            foreach (var group in all)
            {
                if (string.IsNullOrEmpty(group.title))
                    ordered.Add(group);
            }

            foreach (var group in all)
            {
                if (!string.IsNullOrEmpty(group.title))
                    ordered.Add(group);
            }

            return ordered.ToArray();
        }

        // A flag that differs across a multi-object selection answers for none of them, so the fields it governs stay shown and editable until the selection agrees.
        // Hiding or disabling on the resolved value would take a field away from the objects whose flag is clear.
        static bool Hides(SerializedProperty stateProperty)
        {
            return stateProperty.boolValue && !stateProperty.hasMultipleDifferentValues;
        }

        static bool Enables(SerializedProperty stateProperty)
        {
            return stateProperty.boolValue || stateProperty.hasMultipleDifferentValues;
        }

        string AutoAnchorFor(string anchorField)
        {
            var fields = anchorFields;
            if (!fields.HasValue)
                return null;

            return anchorField == fields.Value.localAnchorA ? fields.Value.autoAnchorA : fields.Value.autoAnchorB;
        }

        // Whether this anchor's rotation row is hidden: the joint never reads it (anchor B on a joint whose usesRotationB is false),
        // or the engine derives it while the auto-axis flag is set (see axisFields).
        bool RotationRowHidden(SerializedProperty property, string anchorField)
        {
            var axis = axisFields;
            if (!axis.HasValue)
                return false;

            var isA = anchorField == anchorFields.Value.localAnchorA;
            if (!isA && !axis.Value.usesRotationB)
                return true;

            var autoAxisProperty = property.FindPropertyRelative(axis.Value.autoAxisField);
            return autoAxisProperty != null && Hides(autoAxisProperty);
        }

        // The visible children of a serialized struct, which for an anchor are its position and rotation.
        static IEnumerable<SerializedProperty> Children(SerializedProperty property)
        {
            var iterator = property.Copy();
            var end = iterator.GetEndProperty();

            if (!iterator.NextVisible(true))
                yield break;

            while (!SerializedProperty.EqualContents(iterator, end))
            {
                yield return iterator.Copy();

                if (!iterator.NextVisible(false))
                    yield break;
            }
        }

        // Report any serialized field the group table does not place, so a definition that gains or renames a field fails loudly instead of quietly dropping a row from the inspector.
        void VerifyCoverage(SerializedProperty property)
        {
            if (m_Verified)
                return;

            m_Verified = true;

            var placed = new HashSet<string>();
            foreach (var group in groups)
            {
                if (!string.IsNullOrEmpty(group.stateField))
                    placed.Add(group.stateField);

                foreach (var field in group.fields)
                {
                    placed.Add(field);

                    if (group.anchors)
                        placed.Add(AutoAnchorFor(field));
                }
            }

            var missing = new List<string>();
            foreach (var child in Children(property))
            {
                if (!placed.Contains(child.name))
                    missing.Add(child.name);
            }

            if (missing.Count > 0)
                Debug.LogError(GetType().Name + " does not place " + string.Join(", ", missing) + ", so those fields are missing from the inspector.");
        }

        // The serialized name of a PhysicsTransform's rotation child, matched when deciding whether an anchor's rotation row is shown.
        const string k_RotationField = "rotation";

        /// <summary>
        /// Group titles, shared so the tooltips and the concrete drawers cannot drift apart.
        /// </summary>
        protected const string k_AnchorsTitle = "Anchors";
        protected const string k_SpringTitle = "Spring";
        protected const string k_MotorTitle = "Motor";
        protected const string k_LimitTitle = "Limit";
        protected const string k_ThresholdsTitle = "Thresholds";
        protected const string k_TuningTitle = "Tuning";
        protected const string k_DrawingTitle = "Drawing";

        // One description per group, identical on every joint, so the same title always means the same thing.
        // Worded for any joint, so the limit text names neither a distance nor an angle.
        static class Tooltips
        {
            public const string anchors = "Where the joint attaches on each body, in that body's own space.";
            public const string anchor = "This body's attachment point and frame. With Auto set, the engine computes it when the joint is created and the stored values are not used.";
            public const string spring = "Adds a spring to the joint, pulling it toward its target at a frequency and damping you set.";
            public const string motor = "Drives the joint toward a speed, up to a maximum effort.";
            public const string limit = "Restricts how far the joint can travel from its zero.";
            public const string thresholds = "The force and torque above which the joint reports an event.";
            public const string tuning = "The frequency and damping the solver uses to hold the joint together.";
            public const string drawing = "How this joint appears when the world is drawn.";

            public static string For(string title)
            {
                switch (title)
                {
                    case k_AnchorsTitle: return L10n.Tr(anchors, null);
                    case k_SpringTitle: return L10n.Tr(spring, null);
                    case k_MotorTitle: return L10n.Tr(motor, null);
                    case k_LimitTitle: return L10n.Tr(limit, null);
                    case k_ThresholdsTitle: return L10n.Tr(thresholds, null);
                    case k_TuningTitle: return L10n.Tr(tuning, null);
                    case k_DrawingTitle: return L10n.Tr(drawing, null);
                    default: return string.Empty;
                }
            }
        }

        bool m_Verified;

        #endregion
    }
}
