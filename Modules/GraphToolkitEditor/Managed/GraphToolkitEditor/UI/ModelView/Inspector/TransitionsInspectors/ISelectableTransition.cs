// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Marker interface for elements that can be selected in the transitions inspector through the
    /// <see cref="TransitionSelectionManager{T}"/>, distinguishing them from other <see cref="ISelectableElement"/>s
    /// such as condition views.
    /// </summary>
    internal interface ISelectableTransition : ISelectableElement
    {
    }
}
