// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    internal class ParticleSystemClipboard
    {
        static AnimationCurve m_AnimationCurve1;
        static AnimationCurve m_AnimationCurve2;
        static float m_AnimationCurveScalar;

        public static bool HasSingleAnimationCurve()
        {
            return m_AnimationCurve1 != null && m_AnimationCurve2 == null;
        }

        public static bool HasDoubleAnimationCurve()
        {
            return m_AnimationCurve1 != null && m_AnimationCurve2 != null;
        }

        public static void CopyAnimationCurves(AnimationCurve animCurve, AnimationCurve animCurve2, float scalar)
        {
            m_AnimationCurve1 = animCurve;
            m_AnimationCurve2 = animCurve2;
            m_AnimationCurveScalar = scalar;
        }

        static void ClampCurve(SerializedProperty animCurveProperty, Rect curveRanges)
        {
            AnimationCurve clampedCurve = animCurveProperty.animationCurveValue;
            Keyframe[] keys = clampedCurve.keys;
            for (int i = 0; i < keys.Length; ++i)
            {
                keys[i].time = Mathf.Clamp(keys[i].time, curveRanges.xMin, curveRanges.xMax);
                keys[i].value = Mathf.Clamp(keys[i].value, curveRanges.yMin, curveRanges.yMax);
            }
            clampedCurve.keys = keys;
            animCurveProperty.animationCurveValue = clampedCurve;
        }

        public static void PasteAnimationCurves(SerializedProperty animCurveProperty, SerializedProperty animCurveProperty2, SerializedProperty scalarProperty, Rect curveRanges, ParticleSystemCurveEditor particleSystemCurveEditor)
        {
            if (animCurveProperty != null && m_AnimationCurve1 != null)
            {
                animCurveProperty.animationCurveValue = m_AnimationCurve1;
                ClampCurve(animCurveProperty, curveRanges);
            }

            if (animCurveProperty2 != null && m_AnimationCurve2 != null)
            {
                animCurveProperty2.animationCurveValue = m_AnimationCurve2;
                ClampCurve(animCurveProperty2, curveRanges);
            }

            if (scalarProperty != null)
                scalarProperty.floatValue = m_AnimationCurveScalar;

            // Ensure refresh of systems that uses curves
            if (particleSystemCurveEditor != null)
                particleSystemCurveEditor.Refresh();
        }
    }


    internal class GradientContextMenu
    {
        readonly SerializedProperty m_Property;

        internal static void Show(SerializedProperty prop)
        {
            GUIContent copy = EditorGUIUtility.TrTextContent("Copy");
            GUIContent paste = EditorGUIUtility.TrTextContent("Paste");

            GenericMenu menu = new GenericMenu();
            var gradientMenu = new GradientContextMenu(prop);
            menu.AddItem(copy, false, gradientMenu.Copy);
            if (Clipboard.hasGradient)
                menu.AddItem(paste, false, gradientMenu.Paste);
            else
                menu.AddDisabledItem(paste);
            menu.ShowAsContext();
            Event.current.Use();
        }

        GradientContextMenu(SerializedProperty prop)
        {
            m_Property = prop;
        }

        void Copy()
        {
            Clipboard.gradientValue = m_Property?.gradientValue;
        }

        void Paste()
        {
            if (m_Property == null) return;
            var grad = Clipboard.gradientValue;
            if (grad == null) return;
            m_Property.gradientValue = grad;
            m_Property.serializedObject.ApplyModifiedProperties();
            UnityEditorInternal.GradientPreviewCache.ClearCache();
        }
    }


    internal class AnimationCurveContextMenu
    {
        readonly SerializedProperty m_Prop1;
        readonly SerializedProperty m_Prop2;
        readonly SerializedProperty m_Scalar;
        readonly ParticleSystemCurveEditor m_ParticleSystemCurveEditor;
        readonly Rect m_CurveRanges;

        static internal void Show(Rect position, SerializedProperty property, SerializedProperty property2, SerializedProperty scalar, Rect curveRanges, ParticleSystemCurveEditor curveEditor)
        {
            // Curve context menu
            GUIContent copy = EditorGUIUtility.TrTextContent("Copy");
            GUIContent paste = EditorGUIUtility.TrTextContent("Paste");

            GenericMenu menu = new GenericMenu();

            bool isRegion = property != null && property2 != null;
            bool validPaste = (isRegion && ParticleSystemClipboard.HasDoubleAnimationCurve()) || (!isRegion && ParticleSystemClipboard.HasSingleAnimationCurve());

            AnimationCurveContextMenu obj = new AnimationCurveContextMenu(property, property2, scalar, curveRanges, curveEditor);
            menu.AddItem(copy, false, obj.Copy);
            if (validPaste)
                menu.AddItem(paste, false, obj.Paste);
            else
                menu.AddDisabledItem(paste);

            menu.DropDown(position);
        }

        private AnimationCurveContextMenu(SerializedProperty prop1, SerializedProperty prop2, SerializedProperty scalar, Rect curveRanges, ParticleSystemCurveEditor owner)
        {
            m_Prop1 = prop1;
            m_Prop2 = prop2;
            m_Scalar = scalar;
            m_ParticleSystemCurveEditor = owner;
            m_CurveRanges = curveRanges;
        }

        private void Copy()
        {
            AnimationCurve animCurve1 = m_Prop1 != null ? m_Prop1.animationCurveValue : null;
            AnimationCurve animCurve2 = m_Prop2 != null ? m_Prop2.animationCurveValue : null;
            float scalar = m_Scalar != null ? m_Scalar.floatValue : 1.0f;
            ParticleSystemClipboard.CopyAnimationCurves(animCurve1, animCurve2, scalar);
        }

        private void Paste()
        {
            ParticleSystemClipboard.PasteAnimationCurves(m_Prop1, m_Prop2, m_Scalar, m_CurveRanges, m_ParticleSystemCurveEditor);
            if (m_Prop1 != null)
                m_Prop1.serializedObject.ApplyModifiedProperties();
            if (m_Prop2 != null)
                m_Prop2.serializedObject.ApplyModifiedProperties();
            if (m_Scalar != null)
                m_Scalar.serializedObject.ApplyModifiedProperties();
        }
    }
} // namespace UnityEditor
