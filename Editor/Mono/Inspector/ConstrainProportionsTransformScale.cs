// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    internal class ConstrainProportionsTransformScale
    {
        bool m_ConstrainProportionsScale;
        Vector3 m_InitialScale;

        internal bool constrainProportionsScale { get => m_ConstrainProportionsScale; set => m_ConstrainProportionsScale = value; }

        internal ConstrainProportionsTransformScale(Vector3 previousScale)
        {
            m_InitialScale = previousScale != Vector3.zero ? previousScale : Vector3.one;
        }

        internal Vector3 DoGUI(Rect rect, GUIContent scaleContent, Vector3 value,  UnityEngine.Object[] targetObjects, ref int axisModified, SerializedProperty property = null, SerializedProperty constrainProportionsProperty = null)
        {
            bool previousIsProportionalScale = m_ConstrainProportionsScale;
            uint mixedValues = property != null ? GetMixedValueFields(property) : 0;
            Vector3 scale = EditorGUI.LinkedVector3Field(rect,
                scaleContent, EditorGUIUtility.TrTextContent("", (constrainProportionsScale ? "Disable" : "Enable") + " constrained proportions"), value,
                ref m_ConstrainProportionsScale, m_InitialScale, mixedValues, ref axisModified, property, constrainProportionsProperty);

            if (previousIsProportionalScale != m_ConstrainProportionsScale)
            {
                // Every time scale becomes proportional, update initial scale value
                if (m_ConstrainProportionsScale)
                {
                    m_InitialScale = value != Vector3.zero ? value : Vector3.one;
                }

                foreach (var obj in targetObjects)
                {
                    var tr = obj as Transform;
                    if (tr == null)
                        continue;
                    Undo.RecordObject(tr, "Proportional Scale Toggle Changed");
                    tr.constrainProportionsScale = m_ConstrainProportionsScale;
                }
            }

            return scale;
        }

        [Shortcut("Transform/Toggle Constrain Proportions for Scale")]
        static void ToggleConstrainProportionsScale()
        {
            GameObject[] selected = Selection.gameObjects;

            if (selected == null || selected.Length == 0)
                return;

            bool isProportionalScale = !Selection.DoAllGOsHaveConstrainProportionsEnabled(selected);
            foreach (var obj in selected)
            {
                Undo.RecordObject(obj.transform, "Proportional Scale Toggle Changed");
                obj.transform.constrainProportionsScale = isProportionalScale;
            }

            // To make sure all inspector windows have a proper greyout if initial values are zero, rebuild.
            EditorUtility.ForceRebuildInspectors();
        }

        internal static Vector3 GetVector3WithRatio(Vector3 vector, float ratio)
        {
            //If there are any fields with the same values, use already precalculated values
            float xValue = vector.x * ratio;
            float yValue = vector.y * ratio;

            return new Vector3(
                xValue,
                Mathf.Approximately(vector.y, vector.x) ? xValue : yValue,
                Mathf.Approximately(vector.z, vector.x) ? xValue : Mathf.Approximately(vector.z, vector.y) ? yValue : vector.z * ratio
            );
        }

        internal static Vector3 DoScaleProportions(Vector3 value, Vector3 previousValue, Vector3 initialScale, ref int axisModified)
        {
            float ratio = 1;

            if (previousValue != value)
            {
                // Check which axis was modified and set locked fields and ratio
                //AxisModified values [-1;2] : [none, x, y, z]
                // X axis
                ratio = SetRatio(value.x, previousValue.x, initialScale.x);
                axisModified = ratio != 1 || !Mathf.Approximately(value.x, previousValue.x) ? 0 : -1;

                // Y axis
                if (axisModified == -1)
                {
                    ratio = SetRatio(value.y, previousValue.y, initialScale.y);
                    axisModified = ratio != 1 || !Mathf.Approximately(value.y, previousValue.y) ? 1 : -1;
                }
                // Z axis

                if (axisModified == -1)
                {
                    ratio = SetRatio(value.z, previousValue.z, initialScale.z);
                    axisModified = ratio != 1 || !Mathf.Approximately(value.z, previousValue.z) ? 2 : -1;
                }

                value = GetVector3WithRatio(initialScale, ratio);
            }
            return value;
        }

        static float SetRatio(float value, float previousValue, float initialValue)
        {
            return Mathf.Approximately(value, previousValue) ? 1 : Mathf.Approximately(initialValue, 0) ? 0 : value / initialValue;
        }

        internal static bool HandleMultiSelectionScaleChanges(Vector3 mScale, Vector3 currentScale, bool constrainProportionsScale, Object[] targetObjects, ref int axisModified)
        {
            bool xModified, yModified, zModified;
            Vector3 goScale;

            if (constrainProportionsScale)
            {
                xModified = axisModified == 0;
                yModified = axisModified == 1;
                zModified = axisModified == 2;

                foreach (var obj in targetObjects)
                {
                    var tr = obj as Transform;
                    if (tr == null)
                        continue;

                    Undo.RecordObject(tr, "Scale changed");
                    goScale = tr.localScale;

                    var ratio = xModified ? currentScale.x / goScale.x : yModified ? currentScale.y / goScale.y :  zModified ? currentScale.z / goScale.z : 1;
                    if (Mathf.Approximately(ratio, 0))
                        ratio = 1;

                    tr.localScale = GetVector3WithRatio(goScale, ratio);
                }

                axisModified = -1;
                return true;
            }
            else
            {
                xModified = !Mathf.Approximately(currentScale.x, mScale.x);
                yModified = !Mathf.Approximately(currentScale.y, mScale.y);
                zModified = !Mathf.Approximately(currentScale.z, mScale.z);

                if (xModified || yModified || zModified)
                {
                    foreach (var obj in targetObjects)
                    {
                        var tr = obj as Transform;
                        if (tr == null)
                            continue;

                        Undo.RecordObject(tr, "Scale changed");
                        goScale = tr.localScale;
                        tr.localScale = new Vector3(
                            xModified ? currentScale.x : goScale.x,
                            yModified ? currentScale.y : goScale.y,
                            zModified ? currentScale.z : goScale.z);
                    }

                    return true;
                }
            }

            return false;
        }

        internal static uint GetMixedValueFields(SerializedProperty property)
        {
            uint mask = 0;
            mask = SetBit(mask, 0, property.FindPropertyRelative("x").hasMultipleDifferentValues);
            mask = SetBit(mask, 1, property.FindPropertyRelative("y").hasMultipleDifferentValues);
            mask = SetBit(mask, 2, property.FindPropertyRelative("z").hasMultipleDifferentValues);

            return mask;
        }

        internal static uint GetMultiSelectionLockedFields(UnityEngine.Object[] objects)
        {
            uint mask = 0;

            // Verify multiselection
            if (objects != null)
            {
                foreach (var obj in objects)
                {
                    var tr = obj as Transform;
                    if (tr == null)
                        continue;

                    var localScale = tr.localScale;
                    if (!IsBit(mask, 0) && localScale.x == 0)
                        mask = SetBit(mask, 0, true);

                    if (!IsBit(mask, 1) && localScale.y == 0)
                        mask = SetBit(mask, 1, true);

                    if (!IsBit(mask, 2) && localScale.z == 0)
                        mask = SetBit(mask, 2, true);

                    // If all axis are set(111), return immediately
                    if (mask == 7)
                    {
                        return mask;
                    }
                }
            }


            return mask;
        }

        internal bool Initialize(UnityEngine.Object[] targetObjects)
        {
            bool isGameObjectSelected = targetObjects.Length > 0;
            constrainProportionsScale = isGameObjectSelected && Selection.DoAllGOsHaveConstrainProportionsEnabled(targetObjects);
            return isGameObjectSelected;
        }

        internal static bool IsBit(uint mask, int index)
        {
            return (mask & (1u << index)) != 0;
        }

        internal static uint SetBit(uint mask, int index, bool value)
        {
            uint bitmask = 1u << index;
            if (value)
                return mask | bitmask;
            else
                return mask & (~bitmask);
        }
    }
}
