// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    class MaterialSelectedEvent : EventBase<MaterialSelectedEvent>
    {
        public Material material;
    }

    class MaterialDefinitionChangedEvent : EventBase<MaterialDefinitionChangedEvent>
    {
        public MaterialDefinition newMaterialDefinition;
        public bool refreshField;
    }
}
