// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.Entities
{
    /// <summary>
    /// Add this tag component to an Entity to hide it in the Hierarchy window. <br />
    /// <b>Note:</b> The tag component is only available in the editor.
    /// Make sure to strip any use of it out with <b>#if UNITY_EDITOR</b> to prevent build compilation errors. 
    /// </summary>
    internal struct HideInHierarchy : IComponentData { }
}
