// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityObject = UnityEngine.Object;
using System.Collections.Generic;

namespace UnityEditor.EditorTools
{
    // Specific to tool editors.
    interface IEditor
    {
        // Should be implemented publicly
        UnityObject target { get; }
        IEnumerable<UnityObject> targets { get; }

        // Should be implemented explicitly
        void SetTarget(UnityObject value);
        void SetTargets(UnityObject[] value);
    }
}
