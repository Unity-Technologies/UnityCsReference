// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq.Expressions;
using System.Reflection;
using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor
{
    internal class UniformBoxBoundsHandle : SphereBoundsHandle
    {
        internal UniformBoxBoundsHandle() : base() {}

        protected override void DrawWireframe()
        {
            Handles.DrawWireCube(center, GetSize());
        }
    }

    [CustomEditor(typeof(ParticleSystemForceField))]
    [CanEditMultipleObjects]
    internal class ParticleSystemForceFieldInspector : Editor
    {
        private static readonly SphereBoundsHandle s_SphereBoundsHandle = new SphereBoundsHandle();
        private static readonly UniformBoxBoundsHandle s_BoxBoundsHandle = new UniformBoxBoundsHandle();

        private static PrefColor s_GizmoColor = new PrefColor("Particle System/Force Field Gizmos", 148f / 255f, 229f / 255f, 1f, 0.9f);
        private static readonly Color s_GizmoFocusTint = new Color(0.7f, 0.7f, 0.7f, 1.0f);

        private static PropertyInfo s_StartRangeProperty = typeof(ParticleSystemForceField).GetProperty("startRange");
        private static PropertyInfo s_EndRangeProperty = typeof(ParticleSystemForceField).GetProperty("endRange");
        private static PropertyInfo s_GravityFocusProperty = typeof(ParticleSystemForceField).GetProperty("gravityFocus");
        private static PropertyInfo s_LengthProperty = typeof(ParticleSystemForceField).GetProperty("length");

        private SerializedProperty m_Shape;
        private SerializedProperty m_StartRange;
        private SerializedProperty m_EndRange;
        private SerializedProperty m_Length;
        private SerializedProperty m_DirectionX;
        private SerializedProperty m_DirectionY;
        private SerializedProperty m_DirectionZ;
        private SerializedProperty m_Gravity;
        private SerializedProperty m_GravityFocus;
        private SerializedProperty m_RotationSpeed;
        private SerializedProperty m_RotationAttraction;
        private SerializedProperty m_RotationRandomness;
        private SerializedProperty m_Drag;
        private SerializedProperty m_MultiplyDragByParticleSize;
        private SerializedProperty m_MultiplyDragByParticleVelocity;
        private SerializedProperty m_VectorField;
        private SerializedProperty m_VectorFieldSpeed;
        private SerializedProperty m_VectorFieldAttraction;

        MinMaxCurvePropertyDrawer m_DirectionDrawerX;
        MinMaxCurvePropertyDrawer m_DirectionDrawerY;
        MinMaxCurvePropertyDrawer m_DirectionDrawerZ;
        MinMaxCurvePropertyDrawer m_GravityDrawer;
        MinMaxCurvePropertyDrawer m_RotationSpeedDrawer;
        MinMaxCurvePropertyDrawer m_RotationAttractionDrawer;
        MinMaxCurvePropertyDrawer m_DragDrawer;
        MinMaxCurvePropertyDrawer m_VectorFieldSpeedDrawer;
        MinMaxCurvePropertyDrawer m_VectorFieldAttractionDrawer;

        private class Styles
        {
            public static GUIContent shape = EditorGUIUtility.TrTextContent("Shape", "The bounding shape that forces are applied inside.");
            public static GUIContent startRange = EditorGUIUtility.TrTextContent("Start Range", "The inner extent of the bounding shape.");
            public static GUIContent endRange = EditorGUIUtility.TrTextContent("End Range", "The outer extent of the bounding shape.");
            public static GUIContent length = EditorGUIUtility.TrTextContent("Length", "The length of the cylinder.");
            public static GUIContent directionX = EditorGUIUtility.TrTextContent("X", "The force to apply along the X axis.");
            public static GUIContent directionY = EditorGUIUtility.TrTextContent("Y", "The force to apply along the Y axis.");
            public static GUIContent directionZ = EditorGUIUtility.TrTextContent("Z", "The force to apply along the Z axis.");
            public static GUIContent gravity = EditorGUIUtility.TrTextContent("Strength", "The strength of the gravity effect.");
            public static GUIContent gravityFocus = EditorGUIUtility.TrTextContent("Gravity Focus", "Choose a band within the volume that particles will be attracted towards.");
            public static GUIContent rotationSpeed = EditorGUIUtility.TrTextContent("Speed", "The speed at which particles are propelled around the vortex.");
            public static GUIContent rotationAttraction = EditorGUIUtility.TrTextContent("Attraction", "Controls how strongly particles are dragged into the vortex motion.");
            public static GUIContent rotationRandomness = EditorGUIUtility.TrTextContent("Rotation Randomness", "Propel particles around random axes of the shape.");
            public static GUIContent drag = EditorGUIUtility.TrTextContent("Strength", "The strength of the drag effect.");
            public static GUIContent multiplyDragByParticleSize = EditorGUIUtility.TrTextContent("Multiply by Size", "Adjust the drag based on the size of the particles.");
            public static GUIContent multiplyDragByParticleVelocity = EditorGUIUtility.TrTextContent("Multiply by Velocity", "Adjust the drag based on the velocity of the particles.");
            public static GUIContent vectorField = EditorGUIUtility.TrTextContent("Volume Texture", "The texture used for the vector field.");
            public static GUIContent vectorFieldSpeed = EditorGUIUtility.TrTextContent("Speed", "The speed multiplier applied to particles traveling through the vector field.");
            public static GUIContent vectorFieldAttraction = EditorGUIUtility.TrTextContent("Attraction", "Controls how strongly particles are dragged into the vector field motion.");

            public static GUIContent[] shapeOptions =
            {
                EditorGUIUtility.TrTextContent("Sphere"),
                EditorGUIUtility.TrTextContent("Hemisphere"),
                EditorGUIUtility.TrTextContent("Cylinder"),
                EditorGUIUtility.TrTextContent("Box")
            };

            public static GUIContent shapeHeading = EditorGUIUtility.TrTextContent("Shape");
            public static GUIContent directionHeading = EditorGUIUtility.TrTextContent("Direction");
            public static GUIContent gravityHeading = EditorGUIUtility.TrTextContent("Gravity");
            public static GUIContent rotationHeading = EditorGUIUtility.TrTextContent("Rotation");
            public static GUIContent dragHeading = EditorGUIUtility.TrTextContent("Drag");
            public static GUIContent vectorFieldHeading = EditorGUIUtility.TrTextContent("Vector Field");
        }

        void OnEnable()
        {
            m_Shape = serializedObject.FindProperty("m_Parameters.m_Shape");
            m_StartRange = serializedObject.FindProperty("m_Parameters.m_StartRange");
            m_EndRange = serializedObject.FindProperty("m_Parameters.m_EndRange");
            m_Length = serializedObject.FindProperty("m_Parameters.m_Length");
            m_DirectionX = serializedObject.FindProperty("m_Parameters.m_DirectionCurveX");
            m_DirectionY = serializedObject.FindProperty("m_Parameters.m_DirectionCurveY");
            m_DirectionZ = serializedObject.FindProperty("m_Parameters.m_DirectionCurveZ");
            m_Gravity = serializedObject.FindProperty("m_Parameters.m_GravityCurve");
            m_GravityFocus = serializedObject.FindProperty("m_Parameters.m_GravityFocus");
            m_RotationSpeed = serializedObject.FindProperty("m_Parameters.m_RotationSpeedCurve");
            m_RotationAttraction = serializedObject.FindProperty("m_Parameters.m_RotationAttractionCurve");
            m_RotationRandomness = serializedObject.FindProperty("m_Parameters.m_RotationRandomness");
            m_Drag = serializedObject.FindProperty("m_Parameters.m_DragCurve");
            m_MultiplyDragByParticleSize = serializedObject.FindProperty("m_Parameters.m_MultiplyDragByParticleSize");
            m_MultiplyDragByParticleVelocity = serializedObject.FindProperty("m_Parameters.m_MultiplyDragByParticleVelocity");
            m_VectorField = serializedObject.FindProperty("m_Parameters.m_VectorField");
            m_VectorFieldSpeed = serializedObject.FindProperty("m_Parameters.m_VectorFieldSpeedCurve");
            m_VectorFieldAttraction = serializedObject.FindProperty("m_Parameters.m_VectorFieldAttractionCurve");

            m_DirectionDrawerX = new MinMaxCurvePropertyDrawer() { isNativeProperty = true };
            m_DirectionDrawerY = new MinMaxCurvePropertyDrawer() { isNativeProperty = true };
            m_DirectionDrawerZ = new MinMaxCurvePropertyDrawer() { isNativeProperty = true };
            m_GravityDrawer = new MinMaxCurvePropertyDrawer() { isNativeProperty = true };
            m_RotationSpeedDrawer = new MinMaxCurvePropertyDrawer() { isNativeProperty = true };
            m_RotationAttractionDrawer = new MinMaxCurvePropertyDrawer() { isNativeProperty = true };
            m_DragDrawer = new MinMaxCurvePropertyDrawer() { isNativeProperty = true };
            m_VectorFieldSpeedDrawer = new MinMaxCurvePropertyDrawer() { isNativeProperty = true };
            m_VectorFieldAttractionDrawer = new MinMaxCurvePropertyDrawer() { isNativeProperty = true };
        }

        static void DrawMinMaxCurveField(SerializedProperty property, MinMaxCurvePropertyDrawer drawer, GUIContent label)
        {
            var rect = EditorGUILayout.GetControlRect(false, drawer.GetPropertyHeight(property, label));
            EditorGUI.BeginProperty(rect, label, property);
            drawer.OnGUI(rect, property, label);
            EditorGUI.EndProperty();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            GUILayout.Label(Styles.shapeHeading, EditorStyles.boldLabel);

            EditorGUI.showMixedValue = m_Shape.hasMultipleDifferentValues;
            var shapeRect = EditorGUILayout.GetControlRect(true);
            EditorGUI.Popup(shapeRect, m_Shape, Styles.shapeOptions, Styles.shape);

            EditorGUILayout.PropertyField(m_StartRange, Styles.startRange);
            EditorGUILayout.PropertyField(m_EndRange, Styles.endRange);
            if (m_Shape.intValue == (int)ParticleSystemForceFieldShape.Cylinder)
                EditorGUILayout.PropertyField(m_Length, Styles.length);

            EditorGUILayout.Space();

            GUILayout.Label(Styles.directionHeading, EditorStyles.boldLabel);
            DrawMinMaxCurveField(m_DirectionX, m_DirectionDrawerX, Styles.directionX);
            DrawMinMaxCurveField(m_DirectionY, m_DirectionDrawerY, Styles.directionY);
            DrawMinMaxCurveField(m_DirectionZ, m_DirectionDrawerZ, Styles.directionZ);
            EditorGUILayout.Space();

            EditorGUILayout.Space();

            GUILayout.Label(Styles.gravityHeading, EditorStyles.boldLabel);
            DrawMinMaxCurveField(m_Gravity, m_GravityDrawer, Styles.gravity);
            EditorGUILayout.PropertyField(m_GravityFocus, Styles.gravityFocus);
            EditorGUILayout.Space();

            GUILayout.Label(Styles.rotationHeading, EditorStyles.boldLabel);
            DrawMinMaxCurveField(m_RotationSpeed, m_RotationSpeedDrawer, Styles.rotationSpeed);
            DrawMinMaxCurveField(m_RotationAttraction, m_RotationAttractionDrawer, Styles.rotationAttraction);
            EditorGUILayout.PropertyField(m_RotationRandomness, Styles.rotationRandomness);
            EditorGUILayout.Space();

            GUILayout.Label(Styles.dragHeading, EditorStyles.boldLabel);
            DrawMinMaxCurveField(m_Drag, m_DragDrawer, Styles.drag);
            EditorGUILayout.PropertyField(m_MultiplyDragByParticleSize, Styles.multiplyDragByParticleSize);
            EditorGUILayout.PropertyField(m_MultiplyDragByParticleVelocity, Styles.multiplyDragByParticleVelocity);
            EditorGUILayout.Space();

            GUILayout.Label(Styles.vectorFieldHeading, EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_VectorField, Styles.vectorField);
            DrawMinMaxCurveField(m_VectorFieldSpeed, m_VectorFieldSpeedDrawer, Styles.vectorFieldSpeed);
            DrawMinMaxCurveField(m_VectorFieldAttraction, m_VectorFieldAttractionDrawer, Styles.vectorFieldAttraction);
            EditorGUILayout.Space();

            serializedObject.ApplyModifiedProperties();
        }

        void OnSceneGUI()
        {
            EditorGUI.BeginChangeCheck();

            ParticleSystemForceField ff = (ParticleSystemForceField)target;
            DrawHandle(ff);

            if (EditorGUI.EndChangeCheck())
                Repaint();
        }

        public static void DrawHandle(ParticleSystemForceField ff)
        {
            using (new Handles.DrawingScope(s_GizmoColor, ff.transform.localToWorldMatrix))
            {
                ParticleSystemForceFieldShape forceShape = ff.shape;
                if (forceShape == ParticleSystemForceFieldShape.Sphere)
                {
                    DrawSphere(s_EndRangeProperty, ff);

                    if (ff.startRange > 0.0f)
                        DrawSphere(s_StartRangeProperty, ff);

                    if (ff.gravityFocus > 0.0f)
                    {
                        using (new Handles.DrawingScope(s_GizmoColor * s_GizmoFocusTint))
                            DrawSphere(s_GravityFocusProperty, ff, ff.endRange);
                    }
                }
                else if (forceShape == ParticleSystemForceFieldShape.Hemisphere)
                {
                    DrawHemisphere(s_EndRangeProperty, ff);

                    if (ff.startRange > 0.0f)
                        DrawHemisphere(s_StartRangeProperty, ff);

                    if (ff.gravityFocus > 0.0f)
                    {
                        using (new Handles.DrawingScope(s_GizmoColor * s_GizmoFocusTint))
                            DrawHemisphere(s_GravityFocusProperty, ff, ff.endRange);
                    }
                }
                else if (forceShape == ParticleSystemForceFieldShape.Cylinder)
                {
                    DrawCylinder(s_EndRangeProperty, s_LengthProperty, ff);

                    if (ff.startRange > 0.0f)
                        DrawCylinder(s_StartRangeProperty, s_LengthProperty, ff);

                    if (ff.gravityFocus > 0.0f)
                    {
                        using (new Handles.DrawingScope(s_GizmoColor * s_GizmoFocusTint))
                            DrawCylinder(s_GravityFocusProperty, s_LengthProperty, ff, ff.endRange);
                    }
                }
                else if (forceShape == ParticleSystemForceFieldShape.Box)
                {
                    DrawBox(s_EndRangeProperty, ff);

                    if (ff.startRange > 0.0f)
                        DrawBox(s_StartRangeProperty, ff);

                    if (ff.gravityFocus > 0.0f)
                    {
                        using (new Handles.DrawingScope(s_GizmoColor * s_GizmoFocusTint))
                            DrawBox(s_GravityFocusProperty, ff, ff.endRange);
                    }
                }
            }
        }

        private static void DrawSphere(PropertyInfo radiusProp, ParticleSystemForceField target, float multiplyByRadius = 1.0f)
        {
            s_SphereBoundsHandle.axes = PrimitiveBoundsHandle.Axes.All;
            s_SphereBoundsHandle.center = Vector3.zero;
            s_SphereBoundsHandle.radius = (float)radiusProp.GetValue(target, null) * multiplyByRadius;

            EditorGUI.BeginChangeCheck();
            s_SphereBoundsHandle.DrawHandle();
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, string.Format("Modify {0}", ObjectNames.NicifyVariableName(target.GetType().Name)));
                radiusProp.SetValue(target, s_SphereBoundsHandle.radius / multiplyByRadius, null);
            }
        }

        private static void DrawHemisphere(PropertyInfo radiusProp, ParticleSystemForceField target, float multiplyByRadius = 1.0f)
        {
            EditorGUI.BeginChangeCheck();

            float oldRadius = (float)radiusProp.GetValue(target, null) * multiplyByRadius;
            float newRadius = Handles.DoSimpleRadiusHandle(Quaternion.Euler(-90, 0, 0), Vector3.zero, oldRadius, true);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, string.Format("Modify {0}", ObjectNames.NicifyVariableName(target.GetType().Name)));
                radiusProp.SetValue(target, newRadius / multiplyByRadius, null);
            }
        }

        private static void DrawCylinder(PropertyInfo radiusProp, PropertyInfo lengthProp, ParticleSystemForceField target, float multiplyByRadius = 1.0f)
        {
            float lengthHalf = (float)lengthProp.GetValue(target, null) * 0.5f;

            // Circle at each end
            for (int i = 0; i < 2; i++)
            {
                s_SphereBoundsHandle.axes = PrimitiveBoundsHandle.Axes.X | PrimitiveBoundsHandle.Axes.Z;
                s_SphereBoundsHandle.center = new Vector3(0.0f, (i > 0) ? -lengthHalf : lengthHalf, 0.0f);
                s_SphereBoundsHandle.radius = (float)radiusProp.GetValue(target, null) * multiplyByRadius;

                EditorGUI.BeginChangeCheck();
                s_SphereBoundsHandle.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(target, string.Format("Modify {0}", ObjectNames.NicifyVariableName(target.GetType().Name)));
                    radiusProp.SetValue(target, s_SphereBoundsHandle.radius / multiplyByRadius,  null);
                }
            }

            // Handle at each end for controlling the length
            EditorGUI.BeginChangeCheck();
            lengthHalf = Handles.SizeSlider(Vector3.zero, Vector3.up, lengthHalf);
            lengthHalf = Handles.SizeSlider(Vector3.zero, -Vector3.up, lengthHalf);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, string.Format("Modify {0}", ObjectNames.NicifyVariableName(target.GetType().Name)));
                lengthProp.SetValue(target, Mathf.Max(0.0f, lengthHalf * 2.0f), null);
            }

            // Connecting lines
            float lineRadius = (float)radiusProp.GetValue(target, null) * multiplyByRadius;
            Handles.DrawLine(new Vector3(lineRadius, lengthHalf, 0.0f), new Vector3(lineRadius, -lengthHalf, 0.0f));
            Handles.DrawLine(new Vector3(-lineRadius, lengthHalf, 0.0f), new Vector3(-lineRadius, -lengthHalf, 0.0f));
            Handles.DrawLine(new Vector3(0.0f, lengthHalf, lineRadius), new Vector3(0.0f, -lengthHalf, lineRadius));
            Handles.DrawLine(new Vector3(0.0f, lengthHalf, -lineRadius), new Vector3(0.0f, -lengthHalf, -lineRadius));
        }

        private static void DrawBox(PropertyInfo extentProp, ParticleSystemForceField target, float multiplyByRadius = 1.0f)
        {
            s_BoxBoundsHandle.axes = PrimitiveBoundsHandle.Axes.All;
            s_BoxBoundsHandle.center = Vector3.zero;
            s_BoxBoundsHandle.radius = (float)extentProp.GetValue(target, null) * multiplyByRadius;

            EditorGUI.BeginChangeCheck();
            s_BoxBoundsHandle.DrawHandle();
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, string.Format("Modify {0}", ObjectNames.NicifyVariableName(target.GetType().Name)));
                extentProp.SetValue(target, s_BoxBoundsHandle.radius / multiplyByRadius, null);
            }
        }
    }
}
