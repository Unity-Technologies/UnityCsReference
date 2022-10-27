// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Base class for parts that might have a different look based on the zoom.
    /// </summary>
    abstract class GraphElementPart : BaseModelViewPart
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseModelViewPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        protected GraphElementPart(string name, Model model, ModelView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName)
        {
        }

        /// <summary>
        /// Sets the level of detail appearance of the part based on the current zoom.
        /// </summary>
        /// <param name="zoom"></param>
        /// <param name="newZoomMode">The <see cref="GraphViewZoomMode"/> that will be active from now.</param>
        /// <param name="oldZoomMode">The <see cref="GraphViewZoomMode"/> that was active before this call.</param>
        public virtual void SetLevelOfDetail(float zoom, GraphViewZoomMode newZoomMode, GraphViewZoomMode oldZoomMode) { }
    }
}
