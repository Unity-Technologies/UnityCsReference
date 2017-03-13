// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(OcclusionArea))]
    class OcclusionAreaEditor : Editor
    {
        SerializedObject   m_Object;
        SerializedProperty m_Size;
        SerializedProperty m_Center;

        void OnEnable()
        {
            m_Object = new SerializedObject(target);
            m_Size = serializedObject.FindProperty("m_Size");
            m_Center = serializedObject.FindProperty("m_Center");
        }

        void OnDisable()
        {
            m_Object.Dispose();
            m_Object = null;
        }

        void OnSceneGUI()
        {
            m_Object.Update();

            OcclusionArea area = (OcclusionArea)target;


            Color tempColor = Handles.color;
            Handles.color = new Color(145f, 244f, 139f, 255f) / 255;

            Vector3 offset = area.transform.TransformPoint(m_Center.vector3Value);

            // Get min and max extends from center and size
            Vector3 min = m_Size.vector3Value * 0.5f;
            Vector3 max = m_Size.vector3Value * 0.5f;

            // Yes, it's weird to use lossyScale here, but that's what the occlusion volumes do
            Vector3 scale = area.transform.lossyScale;
            Vector3 inverseScale = new Vector3(1 / scale.x, 1 / scale.y, 1 / scale.z);
            min = Vector3.Scale(min, scale);
            max = Vector3.Scale(max, scale);

            // Handles
            bool temp = GUI.changed;
            min.x = SizeSlider(offset, -Vector3.right,   min.x);
            min.y = SizeSlider(offset, -Vector3.up,      min.y);
            min.z = SizeSlider(offset, -Vector3.forward, min.z);
            max.x = SizeSlider(offset,  Vector3.right,   max.x);
            max.y = SizeSlider(offset,  Vector3.up,      max.y);
            max.z = SizeSlider(offset,  Vector3.forward, max.z);

            // Apply cahnges if there were any
            if (GUI.changed)
            {
                // Occlusion volumes can't be rotated but the center is still affected by rotation, so
                // we need to rotate the offset (and apply the inverse lossyScale AFTER that)
                m_Center.vector3Value = m_Center.vector3Value + Vector3.Scale(Quaternion.Inverse(area.transform.rotation) * (max - min) * 0.5f, inverseScale);
                min = Vector3.Scale(min, inverseScale);
                max = Vector3.Scale(max, inverseScale);
                m_Size.vector3Value = (max + min);

                serializedObject.ApplyModifiedProperties();
            }
            GUI.changed |= temp;

            Handles.color = tempColor;
        }

        float SizeSlider(Vector3 p, Vector3 d, float r)
        {
            Vector3 position = p + d * r;
            Color tempColor = Handles.color;

            if (Vector3.Dot((position - Camera.current.transform.position), d) >= 0)
                Handles.color = new Color(Handles.color.r, Handles.color.g, Handles.color.b, Handles.color.a * Handles.backfaceAlphaMultiplier);

            float size = HandleUtility.GetHandleSize(position);
            bool temp = GUI.changed;
            GUI.changed = false;
            position = Handles.Slider(position, d, size * 0.1f, Handles.CylinderHandleCap, 0f);
            if (GUI.changed)
                r = Vector3.Dot(position - p, d);
            GUI.changed |= temp;

            Handles.color = tempColor;
            return r;
        }
    }
}
