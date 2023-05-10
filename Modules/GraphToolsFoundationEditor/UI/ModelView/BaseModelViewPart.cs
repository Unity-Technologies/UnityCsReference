// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Abstract base class for UI parts.
    /// </summary>
    abstract class BaseModelViewPart : ModelViewPart
    {
        protected Model m_Model;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseModelViewPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        protected BaseModelViewPart(string name, Model model, ChildView ownerElement, string parentClassName) :
        base(name, ownerElement, parentClassName)
        {
            m_Model = model;
        }
    }
}
