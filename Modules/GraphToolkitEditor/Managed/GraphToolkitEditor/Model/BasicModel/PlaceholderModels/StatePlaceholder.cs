// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A model that represents the placeholder of a state.
    /// </summary>
    [Serializable]
    class StatePlaceholder : StateModel, IPlaceholder
    {
        /// <inheritdoc />
        public long ReferenceId { get; set; }

        /// <inheritdoc />
        public override void OnCreateNode()
        {
            base.OnCreateNode();
            PlaceholderModelHelper.SetPlaceholderCapabilities(this);
        }
    }
}
