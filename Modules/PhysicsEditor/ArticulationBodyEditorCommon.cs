// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    class ArticulationBodyEditorCommon
    {
        public static ArticulationBody FindEnabledParentArticulationBody(ArticulationBody body)
        {
            if (body.isRoot)
                return body;
            ArticulationBody parent = body.transform.parent.GetComponentInParent<ArticulationBody>();
            while (parent && !parent.enabled)
            {
                parent = parent.transform.parent.GetComponentInParent<ArticulationBody>();
            }
            return parent;
        }
    }
}
