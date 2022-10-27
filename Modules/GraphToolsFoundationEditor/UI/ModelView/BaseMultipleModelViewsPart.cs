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

        public RootView RootView { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseModelViewPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="rootView">The root view.</param>
        /// <param name="models">The models displayed in this part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        protected BaseMultipleModelViewsPart(string name, IEnumerable<Model> models, RootView rootView, string parentClassName) :
            base(name, parentClassName)
        {
            RootView = rootView;
            m_Models = models.ToList();
        }

        /// <inheritdoc />
        protected override void PartOwnerAddedToView()
        {
            if( RootView is IMultipleModelPartContainer container)
                container.Register(this);

        }

        /// <inheritdoc />
        protected override void PartOwnerRemovedFromView()
        {
            if( RootView is IMultipleModelPartContainer container)
                container.Unregister(this);
        }
    }
}
