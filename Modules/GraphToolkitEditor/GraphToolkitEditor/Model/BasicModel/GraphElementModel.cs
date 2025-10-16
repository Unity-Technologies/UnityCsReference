// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.GraphToolkit.Editor.ContextualMenuItems;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Base class for a model that represents an element in a graph.
    /// </summary>
    [Serializable]
    [UnityRestricted]
    internal abstract partial class GraphElementModel : Model, ICopyPasteCallbackReceiver, IHasContextualMenuItems
    {
        [SerializeField, HideInInspector]
        SerializationVersion m_Version;

        GraphModel m_GraphModel;

        /// <summary>
        /// Serialized version, used for backward compatibility
        /// </summary>
        public SerializationVersion Version => m_Version;

        /// <summary>
        /// The graph model to which the element belongs.
        /// </summary>
        public virtual GraphModel GraphModel
        {
            get => m_GraphModel;
            set
            {
                m_GraphModel = value;

                foreach (var subModel in DependentModels.Where(m => m != null))
                {
                    subModel.GraphModel = value;
                }
            }
        }

        /// <summary>
        /// The dependent models for this model (for example, ports on a node, blocks in context node).
        /// </summary>
        public virtual IEnumerable<GraphElementModel> DependentModels => Enumerable.Empty<GraphElementModel>();

        /// <summary>
        /// The container for this graph element.
        /// </summary>
        public virtual IGraphElementContainer Container => GraphModel;

        /// <summary>
        /// Version number for serialization.
        /// </summary>
        /// <remarks>
        /// Useful for models backward compatibility
        /// </remarks>
        [UnityRestricted]
        internal enum SerializationVersion
        {
            // Use package release number as the name of the version.

            // ReSharper disable once InconsistentNaming
            GTF_V_0_8_2 = 0,

            // ReSharper disable once InconsistentNaming
            GTF_V_0_13_0 = 1,

            /// <summary>
            /// Keep Latest as the highest value in this enum
            /// </summary>
            Latest
        }

        /// <inheritdoc />
        public override void AssignNewGuidRecursively()
        {
            base.AssignNewGuidRecursively();

            foreach (var model in DependentModels)
            {
                model.AssignNewGuidRecursively();
            }
        }

        /// <inheritdoc />
        public override void OnBeforeSerialize()
        {
            base.OnBeforeSerialize();
            m_Version = SerializationVersion.Latest;
        }

        /// <inheritdoc />
        public virtual void OnBeforeCopy()
        {
            foreach (var callbackReceiver in DependentModels.OfType<ICopyPasteCallbackReceiver>())
            {
                callbackReceiver.OnBeforeCopy();
            }
        }

        /// <inheritdoc />
        public virtual void OnAfterCopy()
        {
            foreach (var callbackReceiver in DependentModels.OfType<ICopyPasteCallbackReceiver>())
            {
                callbackReceiver.OnAfterCopy();
            }
        }

        /// <inheritdoc />
        public virtual void OnAfterPaste()
        {
            foreach (var callbackReceiver in DependentModels.OfType<ICopyPasteCallbackReceiver>())
            {
                callbackReceiver.OnAfterPaste();
            }
        }

        /// <inheritdoc />
        public virtual IReadOnlyList<ContextualMenuItem> ContextualMenuItems => k_CommonGraphElementMenuItems;

        static readonly List<ContextualMenuItem> k_CommonGraphElementMenuItems = new() {
            ContextualMenuHelpers.createPlacematItem,
            ContextualMenuHelpers.createLocalSubgraphFromSelectionItem,
            ContextualMenuHelpers.cutItem,
            ContextualMenuHelpers.copyItem,
            ContextualMenuHelpers.pasteItem,
            ContextualMenuHelpers.pasteAsNewMenuItem,
            ContextualMenuHelpers.renameItem,
            ContextualMenuHelpers.duplicateItem,
            ContextualMenuHelpers.deleteItem,
            ContextualMenuHelpers.frameSelectionItem,
            ContextualMenuHelpers.colorItem,
            ContextualMenuHelpers.alignAndDistributeElementsItem
        };
    }
}
