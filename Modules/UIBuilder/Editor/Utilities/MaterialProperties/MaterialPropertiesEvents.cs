// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    class MaterialSelectedEvent : EventBase<MaterialSelectedEvent>
    {
        public Material material;
    }

    class MaterialPropertyAddedEvent : EventBase<MaterialPropertyAddedEvent>
    {
        public MaterialPropertyValue materialPropertyValue;
    }

    class MaterialPropertyChangedEvent : EventBase<MaterialPropertyChangedEvent>
    {
        public MaterialPropertyValue materialPropertyValue;
        public int propertyIndex;
    }

    class MaterialPropertyRemovedEvent : EventBase<MaterialPropertyRemovedEvent>
    {
        public List<int> indices;
    }
}
