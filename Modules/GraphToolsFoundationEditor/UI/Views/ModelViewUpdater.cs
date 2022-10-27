// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.CommandStateObserver;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Observer that updates a <see cref="BaseModelView"/>.
    /// </summary>
    class ModelViewUpdater : StateObserver
    {
        BaseModelView m_View;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelViewUpdater" /> class.
        /// </summary>
        /// <param name="view">The <see cref="BaseModelView"/> to update.</param>
        /// <param name="observedStateComponents">The state components that can cause the view to be updated.</param>
        public ModelViewUpdater(BaseModelView view, params IStateComponent[] observedStateComponents) :
            base(observedStateComponents)
        {
            m_View = view;
        }

        /// <inheritdoc/>
        public override void Observe()
        {
            m_View?.UpdateFromModel();
        }
    }
}
