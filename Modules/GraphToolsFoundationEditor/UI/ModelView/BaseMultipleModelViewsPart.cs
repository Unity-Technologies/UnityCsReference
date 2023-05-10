// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;

namespace Unity.GraphToolsFoundation.Editor
{
    abstract class BaseMultipleModelViewsPart : ModelViewPart
    {
        protected List<Model> m_Models;

        protected RootView OwnerRootView => m_OwnerElement?.RootView;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseModelViewPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="models">The models displayed in this part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        protected BaseMultipleModelViewsPart(string name, IEnumerable<Model> models, ChildView ownerElement, string parentClassName) :
            base(name, ownerElement, parentClassName)
        {
            m_Models = models.ToList();
        }
    }
}
