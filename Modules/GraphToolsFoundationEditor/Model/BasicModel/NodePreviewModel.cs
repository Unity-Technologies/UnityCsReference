// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// The status of the preview.
    /// </summary>
    enum NodePreviewStatus
    {
        Updated,
        Processing,
        Failure
    }

    /// <summary>
    /// Base model that represents the preview of a node.
    /// </summary>
    [Serializable]
    class NodePreviewModel : GraphElementModel
    {
        static readonly Vector2 k_DefaultSize = new(180, 180);

        [SerializeReference]
        AbstractNodeModel m_NodeModel;

        [SerializeField]
        bool m_ShowNodePreview;

        [SerializeField]
        Vector2 m_Size;

        [SerializeField]
        NodePreviewStatus m_PreviewStatus;

        [SerializeReference]
        object m_PreviewObject;

        /// <summary>
        /// The preview object.
        /// </summary>
        public virtual object PreviewObject
        {
            get => m_PreviewObject;
            protected set
            {
                m_PreviewObject = value;
                GraphModel?.CurrentGraphChangeDescription?.AddChangedModel(this, ChangeHint.Data);
            }
        }

        /// <summary>
        /// The status of the preview.
        /// </summary>
        public NodePreviewStatus PreviewStatus
        {
            get => m_PreviewStatus;
            protected set
            {
                m_PreviewStatus = value;
                GraphModel?.CurrentGraphChangeDescription?.AddChangedModel(this, ChangeHint.Data);
            }
        }

        /// <summary>
        /// The size of the preview.
        /// </summary>
        public Vector2 Size
        {
            get => m_Size;
            set
            {
                if (value != m_Size)
                {
                    m_Size = value;
                    GraphModel?.CurrentGraphChangeDescription?.AddChangedModel(this, ChangeHint.Layout);
                }
            }
        }

        /// <summary>
        /// The node to which the preview is attached.
        /// </summary>
        public AbstractNodeModel NodeModel => m_NodeModel;

        /// <summary>
        /// Whether the preview should be shown or not.
        /// </summary>
        public bool ShowNodePreview
        {
            get => m_ShowNodePreview;
            set
            {
                m_ShowNodePreview = value;
                GraphModel?.CurrentGraphChangeDescription?.AddChangedModels(new GraphElementModel[] { this, NodeModel }, ChangeHint.Data);
            }
        }

        /// <summary>
        /// Performs tasks that need to be done when the preview is updated.
        /// </summary>
        /// <param name="newPreviewObject">The updated preview object.</param>
        public virtual void OnPreviewUpdated(object newPreviewObject)
        {
            PreviewStatus = NodePreviewStatus.Updated;
            PreviewObject = newPreviewObject;
        }

        /// <summary>
        /// Performs tasks that need to be done when the preview is processing.
        /// </summary>
        public virtual void OnPreviewProcessing()
        {
            PreviewStatus = NodePreviewStatus.Processing;
        }

        /// <summary>
        /// Performs tasks that need to be done when the preview has failed to be processed.
        /// </summary>
        public virtual void OnPreviewFailure()
        {
            PreviewStatus = NodePreviewStatus.Failure;
        }

        /// <summary>
        /// Called on duplication of the node preview.
        /// </summary>
        /// <param name="sourceNodePreview">Model of the duplicated node preview.</param>
        public virtual void OnDuplicateNodePreview(NodePreviewModel sourceNodePreview)
        {
            ShowNodePreview = sourceNodePreview.ShowNodePreview;
            Size = sourceNodePreview.Size;
        }

        /// <summary>
        /// Called on creation of the node preview.
        /// </summary>
        /// <param name="parent">The parent of the node preview.</param>
        public virtual void OnCreateNodePreview(AbstractNodeModel parent)
        {
            m_NodeModel = parent;
            GraphModel = parent.GraphModel;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NodePreviewModel"/> class.
        /// </summary>
        public NodePreviewModel()
        {
            m_PreviewStatus = NodePreviewStatus.Updated;
            m_Size = k_DefaultSize;
            m_Capabilities.AddRange(new[]
            {
                Editor.Capabilities.Selectable,
                Editor.Capabilities.Resizable,
                Editor.Capabilities.Movable
            });
        }
    }
}
