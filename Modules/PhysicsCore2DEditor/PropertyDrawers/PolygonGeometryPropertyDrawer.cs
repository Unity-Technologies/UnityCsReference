// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

using UnityEditor.UIElements;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.U2D.Physics.Editor
{
    /// <summary>
    /// Builds the UI Toolkit fields for a <see cref="PolygonGeometry"/> serialized property.
    /// The computed normals and centroid are shown read-only because they are derived from the vertices.
    /// Add the returned element directly to embed the fields inline, or wrap it in a <see cref="Foldout"/> for a collapsible section.
    /// </summary>
    public static class PolygonGeometryInspector
    {
        /// <summary>
        /// Create the editable fields for a <see cref="PolygonGeometry"/> property, with no wrapping foldout.
        /// </summary>
        /// <param name="property">The serialized <see cref="PolygonGeometry"/> property to build the fields for.</param>
        /// <returns>A container with the polygon fields; the derived normals and centroid are shown read-only.</returns>
        public static VisualElement CreateFields(SerializedProperty property)
        {
            var body = new VisualElement();

            // The fields are locals captured by the callbacks below, never stored elsewhere.
            // One drawer instance creates the UI for every property of this type in a list, so stored fields would only refer to the last one created.
            // The drop-down lists only the shapes that can be chosen, but still shows Custom as its text when the vertices match none of them.
            var shapeField = new PopupField<Shape>(shapeLabel, new List<Shape>(shapeChoices), Shape.Box) { tooltip = Tooltips.shape };
            shapeField.AddToClassList(PopupField<Shape>.alignedFieldUssClassName);

            var boxSizeField = new Vector2Field(sizeLabel) { tooltip = Tooltips.size };
            boxSizeField.AddToClassList(Vector2Field.alignedFieldUssClassName);

            var regularSizeField = new FloatField(sizeLabel) { tooltip = Tooltips.size };
            regularSizeField.AddToClassList(FloatField.alignedFieldUssClassName);

            var offsetField = new Vector2Field(offsetLabel) { tooltip = Tooltips.offset };
            offsetField.AddToClassList(Vector2Field.alignedFieldUssClassName);

            body.Add(shapeField);
            body.Add(boxSizeField);
            body.Add(regularSizeField);
            body.Add(offsetField);
            body.Add(CreateSeparator());

            body.Add(new PropertyField(property.FindPropertyRelative(nameof(PolygonGeometry.m_Radius))));
            body.Add(new PropertyField(property.FindPropertyRelative(nameof(PolygonGeometry.m_Count))));
            body.Add(new PropertyField(property.FindPropertyRelative(nameof(PolygonGeometry.vertices))));
            body.Add(CreateSeparator());

            // The normals and centroid are computed from the vertices, so they are shown but never editable.
            var readonlyElement = new VisualElement { enabledSelf = false };
            readonlyElement.Add(new PropertyField(property.FindPropertyRelative(nameof(PolygonGeometry.normals))));
            readonlyElement.Add(new PropertyField(property.FindPropertyRelative(nameof(PolygonGeometry.m_Centroid))));
            body.Add(readonlyElement);

            Refresh();

            // The shape, size and offset are never stored, so they are worked out again from the vertices whenever the geometry changes, including by undo.
            body.TrackPropertyValue(property, _ => Refresh());
            shapeField.RegisterValueChangedCallback(evt => WriteGeometry(CreateShape(evt.newValue, ReadGeometry(property))));
            boxSizeField.RegisterValueChangedCallback(evt => WriteGeometry(RebuildShape(ReadGeometry(property), evt.newValue, null)));
            regularSizeField.RegisterValueChangedCallback(evt => WriteGeometry(RebuildShape(ReadGeometry(property), evt.newValue * Vector2.one, null)));
            offsetField.RegisterValueChangedCallback(evt => WriteGeometry(RebuildShape(ReadGeometry(property), null, evt.newValue)));

            return body;

            #region Locals

            // Shows the shape found from the current vertices, and the size and offset fields that match it.
            void Refresh()
            {
                var shape = DetectShape(ReadGeometry(property), out var size, out var transform);

                // A change rebuilds the first selected object's geometry and writes it to every selected object, so it is only allowed when they all have the same geometry.
                // When they differ, the shape shows the mixed-value dash and cannot be changed, and Size and Offset are hidden since no single shape applies to them all.
                var mixed = property.hasMultipleDifferentValues;
                var showSizeAndOffset = !mixed && shape != Shape.Custom;

                shapeField.SetValueWithoutNotify(shape);
                shapeField.showMixedValue = mixed;
                shapeField.SetEnabled(!mixed);
                boxSizeField.SetValueWithoutNotify(LimitPrecision(size));
                regularSizeField.SetValueWithoutNotify(LimitPrecision(size.x));
                offsetField.SetValueWithoutNotify(LimitPrecision(transform.position));

                boxSizeField.style.display = showSizeAndOffset && shape == Shape.Box ? DisplayStyle.Flex : DisplayStyle.None;
                regularSizeField.style.display = showSizeAndOffset && shape != Shape.Box ? DisplayStyle.Flex : DisplayStyle.None;
                offsetField.style.display = showSizeAndOffset ? DisplayStyle.Flex : DisplayStyle.None;
            }

            // Writes the geometry to the property, or leaves the property alone when the geometry is not a known shape.
            void WriteGeometry(PolygonGeometry geometry)
            {
                if (IsKnownShape(geometry))
                {
                    property.boxedValue = geometry;
                    property.serializedObject.ApplyModifiedProperties();
                }

                Refresh();
            }

            #endregion
        }

        // Creates a line that separates the shape fields, the editable geometry fields and the read-only ones.
        private static VisualElement CreateSeparator()
        {
            var separator = new VisualElement();
            separator.style.height = separatorHeight;
            separator.style.marginTop = separatorMargin;
            separator.style.marginBottom = separatorMargin;
            separator.style.backgroundColor = separatorColor;
            return separator;
        }

        // Rounds a shown size or offset to a fixed number of decimal places.
        // Working the values out from the vertices leaves a small float error, such as an offset of 1e-8 for a polygon rebuilt at zero, which would otherwise show in scientific notation.
        // Only the shown values are rounded, so a rebuild still uses the exact values and nothing drifts.
        internal static float LimitPrecision(float value) => (float)Math.Round(value, displayDecimalPlaces);

        // Rounds both components of a shown size or offset. See the float overload.
        internal static Vector2 LimitPrecision(Vector2 value) => new Vector2(LimitPrecision(value.x), LimitPrecision(value.y));

        // Reads the whole geometry from the property.
        internal static PolygonGeometry ReadGeometry(SerializedProperty property) => (PolygonGeometry)property.boxedValue;

        // Works out which shape the geometry is from its vertices, returning its size and the transform that places it.
        // Four vertices are read as a box and any other count as a regular polygon, then the shape is accepted only when every vertex lies within PhysicsWorld.linearSlop of where that shape puts it.
        // A box returns its full width and height, a regular polygon returns the distance from its center to each vertex in both components, and Custom returns zero.
        // A shape smaller than the shape fields allow is Custom, which also keeps the size above the smallest one that creating a regular polygon accepts.
        internal static Shape DetectShape(PolygonGeometry geometry, out Vector2 size, out PhysicsTransform transform)
        {
            size = Vector2.zero;
            transform = PhysicsTransform.identity;

            // An empty geometry, such as a rejected change, has no vertices, and reading them as a span would throw.
            if (geometry.count < 3)
                return Shape.Custom;

            var vertexSpan = geometry.AsReadOnlySpan();

            var center = Vector2.zero;

            for (var i = 0; i < vertexSpan.Length; ++i)
                center += vertexSpan[i];

            center /= vertexSpan.Length;

            var minimumSize = minimumSizeScale * PhysicsWorld.linearSlop;
            var shape = (Shape)vertexSpan.Length;
            Vector2 shapeSize;
            float rotation;

            if (shape == Shape.Box)
            {
                var widthEdge = vertexSpan[1] - vertexSpan[0];
                shapeSize = new Vector2(widthEdge.magnitude, (vertexSpan[2] - vertexSpan[1]).magnitude);
                rotation = PhysicsMath.Atan2(widthEdge.y, widthEdge.x);

                // Turn the box a quarter turn at a time until its rotation is within 45 degrees either way, swapping the width and height on each turn.
                // A box that is not turned then reports its width as X and its height as Y, whichever vertex comes first.
                var quarterTurn = 0.5f * PhysicsMath.PI;

                while (rotation > 0.5f * quarterTurn)
                {
                    rotation -= quarterTurn;
                    shapeSize = new Vector2(shapeSize.y, shapeSize.x);
                }

                while (rotation <= -0.5f * quarterTurn)
                {
                    rotation += quarterTurn;
                    shapeSize = new Vector2(shapeSize.y, shapeSize.x);
                }

                if (Mathf.Min(shapeSize.x, shapeSize.y) < minimumSize)
                    return Shape.Custom;
            }
            else
            {
                var firstOffset = vertexSpan[0] - center;
                shapeSize = firstOffset.magnitude * Vector2.one;

                if (shapeSize.x < minimumSize)
                    return Shape.Custom;

                // Every vertex of a regular polygon lies on one of its evenly spaced vertex angles, so the rotation is the angle of the first vertex less the angle of any vertex of an unturned one.
                var unturnedVertex = PolygonGeometry.CreateRegularPolygon(vertexSpan.Length, shapeSize.x, 0.0f, PhysicsTransform.identity).AsReadOnlySpan()[0];
                rotation = PhysicsMath.Atan2(firstOffset.y, firstOffset.x) - PhysicsMath.Atan2(unturnedVertex.y, unturnedVertex.x);
            }

            var shapeTransform = new PhysicsTransform(center, PhysicsRotate.FromRadians(rotation));

            if (!HaveMatchingVertices(geometry, ResizeShape(shape, shapeSize, 0.0f, shapeTransform)))
                return Shape.Custom;

            size = shapeSize;
            transform = shapeTransform;
            return shape;
        }

        // Whether the geometry is detected as one of the shapes, which every change the shape fields make must be before it is written.
        // Checking the result rather than the input means a change can never write vertices that then show as Custom, even through rounding at the smallest size.
        internal static bool IsKnownShape(PolygonGeometry geometry) => DetectShape(geometry, out _, out _) != Shape.Custom;

        // Rebuilds the detected shape with a new size or a new offset, keeping its rotation, its radius and whichever of the two is not given.
        // Returns an empty geometry that is not valid when the geometry matches no shape.
        internal static PolygonGeometry RebuildShape(PolygonGeometry geometry, Vector2? newSize, Vector2? newOffset)
        {
            var shape = DetectShape(geometry, out var size, out var transform);

            return shape == Shape.Custom ? default : ResizeShape(shape, newSize ?? size, geometry.radius, new PhysicsTransform(newOffset ?? transform.position, transform.rotation));
        }

        // Whether both geometries have the same vertex count and every vertex of the first lies within PhysicsWorld.linearSlop of a vertex of the second.
        // The vertices are matched in any order, because the same polygon can list its vertices starting from any of them.
        private static bool HaveMatchingVertices(PolygonGeometry geometry, PolygonGeometry otherGeometry)
        {
            if (otherGeometry.count != geometry.count)
                return false;

            var vertexSpan = geometry.AsReadOnlySpan();
            var otherVertexSpan = otherGeometry.AsReadOnlySpan();
            var toleranceSqr = PhysicsWorld.linearSlop * PhysicsWorld.linearSlop;

            for (var i = 0; i < vertexSpan.Length; ++i)
            {
                var found = false;

                for (var j = 0; j < otherVertexSpan.Length && !found; ++j)
                    found = (vertexSpan[i] - otherVertexSpan[j]).sqrMagnitude <= toleranceSqr;

                if (!found)
                    return false;
            }

            return true;
        }

        // Creates the chosen shape to fit the bounds of the current vertices, so the polygon stays about the same size and in the same place.
        // A box fills the bounds, and a regular polygon takes half the larger side of the bounds as the distance from its center to each vertex.
        internal static PolygonGeometry CreateShape(Shape shape, PolygonGeometry geometry)
        {
            // An empty geometry has no vertices to read, so it uses a unit box as its bounds.
            var vertexSpan = geometry.count > 0 ? geometry.AsReadOnlySpan() : ReadOnlySpan<Vector2>.Empty;
            var boundsMin = vertexSpan.Length > 0 ? vertexSpan[0] : -0.5f * Vector2.one;
            var boundsMax = vertexSpan.Length > 0 ? vertexSpan[0] : 0.5f * Vector2.one;

            for (var i = 1; i < vertexSpan.Length; ++i)
            {
                boundsMin = Vector2.Min(boundsMin, vertexSpan[i]);
                boundsMax = Vector2.Max(boundsMax, vertexSpan[i]);
            }

            // The fitted size is raised to the smallest size the shape fields allow, so tiny vertices still give a shape that can be chosen.
            var boundsSize = Vector2.Max(boundsMax - boundsMin, minimumSizeScale * PhysicsWorld.linearSlop * Vector2.one);
            var transform = new PhysicsTransform(0.5f * (boundsMin + boundsMax));

            return ResizeShape(shape, shape == Shape.Box ? boundsSize : 0.5f * Mathf.Max(boundsSize.x, boundsSize.y) * Vector2.one, geometry.radius, transform);
        }

        // Creates the shape at the given size and placement.
        // A box reads both components of the size, and a regular polygon reads only X as the distance from its center to each vertex.
        // A size that is not finite or is below the smallest size the shape fields allow, or a transform that is not finite, returns an empty geometry that is never written.
        internal static PolygonGeometry ResizeShape(Shape shape, Vector2 size, float radius, PhysicsTransform transform)
        {
            var minimumSize = minimumSizeScale * PhysicsWorld.linearSlop;
            var sizeIsAllowed = size.x >= minimumSize && (shape != Shape.Box || size.y >= minimumSize);

            // The size comparisons are false for NaN, so a NaN size is rejected along with a small one.
            if (!sizeIsAllowed || float.IsInfinity(size.x) || float.IsInfinity(size.y) || !transform.isFinite)
                return default;

            return shape == Shape.Box
                ? PolygonGeometry.CreateBox(size, radius, transform)
                : PolygonGeometry.CreateRegularPolygon((int)shape, size.x, radius, transform);
        }

        // The color of the line between the shape fields and the vertex fields, matched to the editor skin.
        internal static Color separatorColor => EditorGUIUtility.isProSkin ? darkSkinSeparatorColor : lightSkinSeparatorColor;

        // The shapes the Shape drop-down shows, each numbered by its vertex count.
        // Custom is any geometry that matches none of the others, has no size or offset to edit, and is shown but never listed as a choice.
        internal enum Shape
        {
            Custom = 0,
            Triangle = 3,
            Box = 4,
            Pentagon = 5,
            Hexagon = 6,
            Heptagon = 7,
            Octagon = 8
        }

        // The shape field tooltips, shared by the UI Toolkit and IMGUI drawing.
        internal static class Tooltips
        {
            public const string shape = "Detected from the current vertices, and not stored. Choosing a shape replaces the vertices, sized to fit the current ones. Editing the vertices so they no longer match a shape shows Custom. Not available when the selected objects have different geometry.";
            public const string size = "Calculated from the current vertices, and not stored. Changing it rebuilds the vertices, keeping their center and rotation. For a box this is its full width and height, and for other shapes the distance from the center to each vertex.";
            public const string offset = "The center of the shape relative to the body. Calculated from the current vertices, and not stored. Changing it moves the vertices, keeping their size and rotation. A position set on the component that uses this geometry, such as a physics area's Transformation, is applied on top of this offset.";
        }

        #region Internal

        // The shapes listed in the Shape drop-down, which leaves out Custom because choosing it would create nothing.
        internal static readonly Shape[] shapeChoices = { Shape.Triangle, Shape.Box, Shape.Pentagon, Shape.Hexagon, Shape.Heptagon, Shape.Octagon };

        // The labels and layout of the shape fields and the separator line under them.
        internal const string shapeLabel = "Shape";
        internal const string sizeLabel = "Size";
        internal const string offsetLabel = "Offset";

        // The Shape drop-down's text when the selected objects have different geometry, matching the dash Unity shows in any field with mixed values.
        internal static readonly GUIContent mixedShapeContent = new GUIContent("—", "Mixed Values");
        internal const float separatorHeight = 1.0f;
        internal const float separatorMargin = 4.0f;

        // The decimal places the size and offset fields show, enough to hide the float error left by working them out from the vertices.
        private const int displayDecimalPlaces = 5;

        // The smallest size the shape fields allow, as a multiple of the linear slop, which keeps every vertex of an octagon far enough from its neighbors to survive.
        private const float minimumSizeScale = 10.0f;

        // The separator line colors for the dark and light editor skins.
        private static readonly Color darkSkinSeparatorColor = new Color(0.1f, 0.1f, 0.1f);
        private static readonly Color lightSkinSeparatorColor = new Color(0.6f, 0.6f, 0.6f);

        #endregion
    }

    [CustomPropertyDrawer(typeof(PolygonGeometry))]
    sealed class PolygonGeometryPropertyDrawer : PropertyDrawer
    {
        #region UITK

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();
            var foldout = new Foldout { text = property.displayName, value = false, viewDataKey = typeof(PolygonGeometryPropertyDrawer).ToString() };
            root.Add(foldout);
            foldout.Add(PolygonGeometryInspector.CreateFields(property));
            return root;
        }

        #endregion

        #region IMGUI

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
                return EditorGUIUtility.singleLineHeight;

            var verticesProperty = property.FindPropertyRelative(nameof(PolygonGeometry.vertices));
            var normalsProperty = property.FindPropertyRelative(nameof(PolygonGeometry.normals));
            var shape = PolygonGeometryInspector.DetectShape(PolygonGeometryInspector.ReadGeometry(property), out _, out _);
            var shapeLineCount = shape == PolygonGeometryInspector.Shape.Custom || property.hasMultipleDifferentValues ? 1 : 3;

            // Measured in wide mode, the same as they are drawn, so every Vector2 takes a single line.
            var wideMode = EditorGUIUtility.wideMode;
            EditorGUIUtility.wideMode = true;
            var verticesHeight = EditorGUI.GetPropertyHeight(verticesProperty, true);
            var normalsHeight = EditorGUI.GetPropertyHeight(normalsProperty, true);
            EditorGUIUtility.wideMode = wideMode;

            return EditorGUIUtility.singleLineHeight
                + (EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight) * shapeLineCount
                + EditorGUIUtility.standardVerticalSpacing + PolygonGeometryInspector.separatorHeight + PolygonGeometryInspector.separatorMargin * 2.0f
                + (EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight) * 2
                + EditorGUIUtility.standardVerticalSpacing + verticesHeight
                + EditorGUIUtility.standardVerticalSpacing + PolygonGeometryInspector.separatorHeight + PolygonGeometryInspector.separatorMargin * 2.0f
                + EditorGUIUtility.standardVerticalSpacing + normalsHeight
                + EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Every Vector2 is drawn in wide mode, keeping its X and Y on the same line as its label whatever the width, as the UI Toolkit fields do.
            var wideMode = EditorGUIUtility.wideMode;
            EditorGUIUtility.wideMode = true;

            label = EditorGUI.BeginProperty(position, label, property);

            var foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;

                var verticesProperty = property.FindPropertyRelative(nameof(PolygonGeometry.vertices));
                var countProperty = property.FindPropertyRelative(nameof(PolygonGeometry.m_Count));
                var radiusProperty = property.FindPropertyRelative(nameof(PolygonGeometry.m_Radius));
                var normalsProperty = property.FindPropertyRelative(nameof(PolygonGeometry.normals));
                var centroidProperty = property.FindPropertyRelative(nameof(PolygonGeometry.m_Centroid));

                float y = foldoutRect.yMax + EditorGUIUtility.standardVerticalSpacing;
                var lineHeight = EditorGUIUtility.singleLineHeight;
                var spacing = EditorGUIUtility.standardVerticalSpacing;

                // The shape, size and offset are never stored, so they are worked out from the vertices on every draw.
                var geometry = PolygonGeometryInspector.ReadGeometry(property);
                var shape = PolygonGeometryInspector.DetectShape(geometry, out var size, out var transform);
                var shownSize = PolygonGeometryInspector.LimitPrecision(size);
                var shownOffset = PolygonGeometryInspector.LimitPrecision(transform.position);

                // A change rebuilds the first selected object's geometry and writes it to every selected object, so it is only allowed when they all have the same geometry.
                // When they differ, the shape shows the mixed-value dash and cannot be changed, and Size and Offset are hidden since no single shape applies to them all.
                var mixed = property.hasMultipleDifferentValues;
                EditorGUI.BeginDisabledGroup(mixed);

                // The drop-down shows Custom as its text when the vertices match no shape, but its menu lists only the shapes that can be chosen.
                var shapeRect = EditorGUI.PrefixLabel(new Rect(position.x, y, position.width, lineHeight), new GUIContent(PolygonGeometryInspector.shapeLabel, PolygonGeometryInspector.Tooltips.shape));
                if (EditorGUI.DropdownButton(shapeRect, mixed ? PolygonGeometryInspector.mixedShapeContent : new GUIContent(shape.ToString()), FocusType.Keyboard))
                {
                    var serializedObject = property.serializedObject;
                    var propertyPath = property.propertyPath;
                    var shapeMenu = new GenericMenu();

                    foreach (var choice in PolygonGeometryInspector.shapeChoices)
                        shapeMenu.AddItem(new GUIContent(choice.ToString()), choice == shape, () => ChooseShape(serializedObject, propertyPath, choice));

                    shapeMenu.DropDown(shapeRect);
                }

                y += lineHeight + spacing;

                if (!mixed && shape != PolygonGeometryInspector.Shape.Custom)
                {
                    var sizeRect = new Rect(position.x, y, position.width, lineHeight);
                    var sizeContent = new GUIContent(PolygonGeometryInspector.sizeLabel, PolygonGeometryInspector.Tooltips.size);

                    EditorGUI.BeginChangeCheck();
                    var newSize = shape == PolygonGeometryInspector.Shape.Box ? EditorGUI.Vector2Field(sizeRect, sizeContent, shownSize) : EditorGUI.FloatField(sizeRect, sizeContent, shownSize.x) * Vector2.one;
                    if (EditorGUI.EndChangeCheck())
                        WriteGeometry(PolygonGeometryInspector.ResizeShape(shape, newSize, geometry.radius, transform));

                    y += lineHeight + spacing;

                    var offsetRect = new Rect(position.x, y, position.width, lineHeight);
                    var offsetContent = new GUIContent(PolygonGeometryInspector.offsetLabel, PolygonGeometryInspector.Tooltips.offset);

                    EditorGUI.BeginChangeCheck();
                    var newOffset = EditorGUI.Vector2Field(offsetRect, offsetContent, shownOffset);
                    if (EditorGUI.EndChangeCheck())
                        WriteGeometry(PolygonGeometryInspector.ResizeShape(shape, size, geometry.radius, new PhysicsTransform(newOffset, transform.rotation)));

                    y += lineHeight + spacing;
                }

                EditorGUI.EndDisabledGroup();

                y = DrawSeparator(y);

                EditorGUI.PropertyField(new Rect(position.x, y, position.width, lineHeight), radiusProperty, false);
                y += lineHeight + spacing;

                EditorGUI.PropertyField(new Rect(position.x, y, position.width, lineHeight), countProperty, false);
                y += lineHeight + spacing;

                var verticesHeight = EditorGUI.GetPropertyHeight(verticesProperty, true);
                EditorGUI.PropertyField(new Rect(position.x, y, position.width, verticesHeight), verticesProperty, true);
                y += verticesHeight + spacing;

                y = DrawSeparator(y);

                EditorGUI.BeginDisabledGroup(true);

                var normalsHeight = EditorGUI.GetPropertyHeight(normalsProperty, true);
                EditorGUI.PropertyField(new Rect(position.x, y, position.width, normalsHeight), normalsProperty, true);
                y += normalsHeight + spacing;

                EditorGUI.PropertyField(new Rect(position.x, y, position.width, lineHeight), centroidProperty, false);

                EditorGUI.EndDisabledGroup();

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();

            EditorGUIUtility.wideMode = wideMode;

            #region Locals

            // Writes the geometry to the property, or leaves the property alone when the geometry is not a known shape.
            void WriteGeometry(PolygonGeometry newGeometry)
            {
                if (PolygonGeometryInspector.IsKnownShape(newGeometry))
                    property.boxedValue = newGeometry;
            }

            // Draws a separator line at the given height and returns the height of the next row below it.
            float DrawSeparator(float separatorY)
            {
                separatorY += PolygonGeometryInspector.separatorMargin;
                EditorGUI.DrawRect(EditorGUI.IndentedRect(new Rect(position.x, separatorY, position.width, PolygonGeometryInspector.separatorHeight)), PolygonGeometryInspector.separatorColor);
                return separatorY + PolygonGeometryInspector.separatorHeight + PolygonGeometryInspector.separatorMargin + EditorGUIUtility.standardVerticalSpacing;
            }

            // Replaces the vertices with the shape chosen from the menu.
            // The menu calls this after the draw has finished, so the property is found again and the change applied here rather than by the Inspector.
            static void ChooseShape(SerializedObject serializedObject, string propertyPath, PolygonGeometryInspector.Shape choice)
            {
                serializedObject.Update();

                var shapeProperty = serializedObject.FindProperty(propertyPath);
                var newGeometry = PolygonGeometryInspector.CreateShape(choice, PolygonGeometryInspector.ReadGeometry(shapeProperty));

                if (PolygonGeometryInspector.IsKnownShape(newGeometry))
                {
                    shapeProperty.boxedValue = newGeometry;
                    serializedObject.ApplyModifiedProperties();
                }
            }

            #endregion
        }

        #endregion
    }
}
