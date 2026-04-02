// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal class MaterialSelectedEvent : EventBase<MaterialSelectedEvent>
    {
        public Material material;
    }

    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal class MaterialDefinitionChangedEvent : EventBase<MaterialDefinitionChangedEvent>
    {
        public MaterialDefinition newMaterialDefinition;
        public bool refreshField;
    }
}
