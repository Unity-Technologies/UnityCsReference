// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor
{
    public static class GizmoUtility
    {
        const float k_MinFadeGizmoSize = 1f;
        const float k_MaxFadeGizmoSize = 10f;

        static void GetAnnotationIdAndClass(Type type, out int id, out string klass)
        {
            var unityType = UnityType.FindTypeByName(type.Name);
            id = unityType?.persistentTypeID ?? 0;
            // In AnnotationManager, if script name is null or empty the persistent ID is used. If not, the type is
            // assumed to be a built-in type.
            klass = unityType == null ? type.Name : null;
        }

        public static bool TryGetGizmoInfo(Type type, out GizmoInfo info)
        {
            GetAnnotationIdAndClass(type, out var id, out var name);
            var annotation = AnnotationUtility.GetAnnotation(id, name);

            if (annotation.gizmoEnabled == -1 && annotation.iconEnabled == -1 && annotation.flags == -1)
            {
                info = new GizmoInfo();
                return false;
            }

            info = new GizmoInfo(annotation);
            return true;
        }

        public static GizmoInfo[] GetGizmoInfo()
        {
            var annotations = AnnotationUtility.GetAnnotations();
            return annotations.Select(x => new GizmoInfo(x)).ToArray();
        }

        public static void ApplyGizmoInfo(GizmoInfo info, bool addToRecentlyChanged = true)
        {
            int gizmoEnabled = info.gizmoEnabled ? 1 : 0;
            AnnotationUtility.SetGizmoEnabled(info.classID, info.scriptClass, gizmoEnabled, addToRecentlyChanged);
            AnnotationUtility.SetIconEnabled(info.classID, info.scriptClass, info.iconEnabled ? 1 : 0);
        }

        public static void SetGizmoEnabled(Type type, bool enabled, bool addToRecentlyChanged = true)
        {
            GetAnnotationIdAndClass(type, out var id, out var name);
            AnnotationUtility.SetGizmoEnabled(id, name, enabled ? 1 : 0, addToRecentlyChanged);
        }

        public static void SetIconEnabled(Type type, bool enabled)
        {
            GetAnnotationIdAndClass(type, out var id, out var name);
            AnnotationUtility.SetIconEnabled(id, name, enabled ? 1 : 0);
        }

        public static float iconSize
        {
            get => AnnotationUtility.iconSize;
            set => AnnotationUtility.iconSize = Mathf.Clamp01(value);
        }

        public static bool use3dIcons
        {
            get => AnnotationUtility.use3dGizmos;
            set => AnnotationUtility.use3dGizmos = value;
        }
    }
}
