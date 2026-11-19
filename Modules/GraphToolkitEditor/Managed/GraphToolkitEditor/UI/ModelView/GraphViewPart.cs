// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Base class for all parts displayed in a <see cref="GraphView"/>.
    /// </summary>
    [UnityRestricted]
    internal abstract class GraphViewPart : BaseModelViewPart
    {
        /// <summary>
        /// Current culling state of the part.
        /// </summary>
        protected GraphViewCullingState m_CullingState = GraphViewCullingState.Disabled;

        GraphViewPartPositionInfo m_PositionInfo = GraphViewPartPositionInfo.Invalid;

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphViewPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        protected GraphViewPart(string name, Model model, ChildView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) { }

        /// <summary>
        /// Sets the culling state of the part.
        /// </summary>
        /// <param name="cullingState">The new culling state.</param>
        public virtual void SetCullingState(GraphViewCullingState cullingState)
        {
            // When culling is not supported, do not remove the part, but make sure it keeps it position
            // by setting the correct styling. When culling is disabled, the GraphElement is cleared so the part
            // needs to be reinserted.
            var supportsCulling = SupportsCulling();
            if (cullingState == GraphViewCullingState.Enabled)
            {
                switch (supportsCulling)
                {
                    case false when (!m_PositionInfo.Valid && Root.hierarchy.parent != null):
                        m_PositionInfo = new GraphViewPartPositionInfo(Root, m_OwnerElement);
                        m_PositionInfo.ApplyLayoutInOwnerSpace();
                        break;
                    case true:
                        Root.RemoveFromHierarchy();
                        break;
                }
            }
            else
            {
                if (m_PositionInfo.Valid)
                {
                    m_PositionInfo.RevertPositionInOwnerSpace();
                    m_PositionInfo = GraphViewPartPositionInfo.Invalid;
                }

                if (!m_OwnerElement.Contains(Root))
                    m_OwnerElement.Add(Root);
            }

            m_CullingState = supportsCulling ? cullingState : GraphViewCullingState.Disabled;
        }

        /// <summary>
        /// Returns whether the part supports culling.
        /// </summary>
        public virtual bool SupportsCulling() => true;

        /// <summary>
        /// Returns whether the part is culled.
        /// </summary>
        /// <returns>True if the part is culled, false otherwise.</returns>
        public virtual bool IsCulled() => m_CullingState == GraphViewCullingState.Enabled;
    }
}
