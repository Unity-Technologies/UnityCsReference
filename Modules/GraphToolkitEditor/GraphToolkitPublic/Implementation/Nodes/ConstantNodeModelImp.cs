// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.GraphToolkit.Editor.Implementation
{
    [Serializable]
    class ConstantNodeModelImp : ConstantNodeModel, IConstantNode
    {
        public Type DataType => Value.Type;

        public bool TryGetValue<T>(out T value)
        {
            if (Value == null)
            {
                value = default;
                return false;
            }
            return Value.TryGetValue(out value);
        }
    }
}
