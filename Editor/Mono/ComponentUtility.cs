// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditorInternal
{
    public partial class ComponentUtility
    {
        static private bool CompareComponentOrderAndTypes(List<Component> srcComponents, List<Component> dstComponents)
        {
            if (srcComponents.Count != dstComponents.Count)
                return false;

            for (int i = 0; i != srcComponents.Count; i++)
            {
                if (srcComponents[i].GetType() != dstComponents[i].GetType())
                    return false;
            }

            return true;
        }

        private static void DestroyComponents(List<Component> components)
        {
            // Delete in reverse order (to avoid errors when RequireComponent is used)
            for (int i = components.Count - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(components[i]);
        }

        public delegate bool IsDesiredComponent(Component c);

        public static void DestroyComponentsMatching(GameObject dst, IsDesiredComponent componentFilter)
        {
            var dstComponents = new List<Component>();
            dst.GetComponents(dstComponents);
            dstComponents.RemoveAll(x => !componentFilter(x));
            DestroyComponents(dstComponents);
        }

        public static void ReplaceComponentsIfDifferent(GameObject src, GameObject dst, IsDesiredComponent componentFilter)
        {
            var srcComponents = new List<Component>();
            src.GetComponents(srcComponents);
            srcComponents.RemoveAll(x => !componentFilter(x));

            var dstComponents = new List<Component>();
            dst.GetComponents(dstComponents);
            dstComponents.RemoveAll(x => !componentFilter(x));

            // Generate components
            if (!CompareComponentOrderAndTypes(srcComponents, dstComponents))
            {
                DestroyComponents(dstComponents);

                // Add src components to dst
                dstComponents.Clear();
                for (int i = 0; i != srcComponents.Count; i++)
                {
                    Component com = dst.AddComponent(srcComponents[i].GetType());
                    dstComponents.Add(com);
                }
            }

            // Copy Data to components
            for (int i = 0; i != srcComponents.Count; i++)
                UnityEditor.EditorUtility.CopySerializedIfDifferent(srcComponents[i], dstComponents[i]);
        }
    }
}
