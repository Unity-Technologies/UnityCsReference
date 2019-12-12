// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.Experimental.GraphView
{
    public interface ICollectibleElement
    {
        void CollectElements(HashSet<GraphElement> collectedElementSet, Func<GraphElement, bool> conditionFunc);
    }
}
