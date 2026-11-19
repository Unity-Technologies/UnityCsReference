// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace Unity.Animations
{
    static class TransformUtility
    {
        /// <summary>
        /// Compute relative path between root transform and target transform
        /// </summary>
        /// <param name="target">Target Transform</param>
        /// <param name="root">Root Transform</param>
        /// <returns>Returns relative path</returns>
        public static string ComputeRelativePath(Transform target, Transform root)
        {
            if (target == root)
                return string.Empty;

            var res = target.name;
            var cur = target.parent;

            while (cur != root && cur != null)
            {
                res = $"{cur.name}/{res}";
                cur = cur.parent;
            }

            return res;
        }
    }
}

