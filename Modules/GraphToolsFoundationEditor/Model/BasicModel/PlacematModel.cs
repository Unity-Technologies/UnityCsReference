// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// A model that represents a placemat in a graph.
    /// </summary>
    [Serializable]
    [MovedFrom(false, "Unity.GraphToolsFoundation.Editor", "Unity.GraphTools.Foundation.Model")]
    class PlacematModel : GraphElementModel, IHasTitle, IMovable, ICollapsible, IResizable, IRenamable
    {
        const string k_DefaultPlacematName = "Placemat";

        [SerializeField]
        string m_Title;

        [SerializeField]
        Rect m_Position;

        [SerializeField]
        bool m_Collapsed;

        [SerializeField]
        List<string> m_HiddenElements;

        List<GraphElementModel> m_CachedHiddenElementModels;

        public override Color DefaultColor => new Color(74.0f/255.0f, 88.0f/255.0f, 91.0f / 255.0f);

        /// <inheritdoc />
        public override bool UseColorAlpha => false;

        /// <inheritdoc />
        public virtual string Title
        {
            get => m_Title;
            set => m_Title = value;
        }

        /// <inheritdoc />
        public virtual string DisplayTitle => Title;

        /// <inheritdoc />
        public virtual Rect PositionAndSize
        {
            get => m_Position;
            set
            {
                var r = value;
                if (!this.IsResizable())
                    r.size = m_Position.size;

                if (!this.IsMovable())
                    r.position = m_Position.position;

                m_Position = r;
            }
        }

        /// <inheritdoc />
        public virtual Vector2 Position
        {
            get => PositionAndSize.position;
            set => PositionAndSize = new Rect(value, PositionAndSize.size);
        }

        /// <inheritdoc />
        public virtual bool Collapsed
        {
            get => m_Collapsed;
            set
            {
                if (!this.IsCollapsible())
                    return;

                m_Collapsed = value;
                this.SetCapability(Editor.Capabilities.Resizable, !m_Collapsed);
            }
        }

        public List<string> HiddenElementsGuid
        {
            get => m_HiddenElements;
            set
            {
                m_HiddenElements = value;
                m_CachedHiddenElementModels = null;
            }
        }

        /// <summary>
        /// Whether the object was deleted from the graph.
        /// </summary>
        public bool Destroyed { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlacematModel"/> class.
        /// </summary>
        public PlacematModel()
        {
            m_Capabilities.AddRange(new[]
            {
                Editor.Capabilities.Deletable,
                Editor.Capabilities.Copiable,
                Editor.Capabilities.Selectable,
                Editor.Capabilities.Renamable,
                Editor.Capabilities.Movable,
                Editor.Capabilities.Resizable,
                Editor.Capabilities.Collapsible,
                Editor.Capabilities.Colorable,
                Editor.Capabilities.Ascendable
            });

            m_Title = k_DefaultPlacematName;
        }

        /// <summary>
        /// Marks the object as being deleted from the graph.
        /// </summary>
        public virtual void Destroy() => Destroyed = true;

        /// <inheritdoc />
        public virtual void Move(Vector2 delta)
        {
            if (!this.IsMovable())
                return;

            PositionAndSize = new Rect(PositionAndSize.position + delta, PositionAndSize.size);
        }

        /// <inheritdoc />
        public virtual void Rename(string newName)
        {
            if (!this.IsRenamable())
                return;

            Title = newName;
        }

        /// <summary>
        /// Elements hidden in the placemat.
        /// </summary>
        public virtual IEnumerable<GraphElementModel> HiddenElements
        {
            get
            {
                if (m_CachedHiddenElementModels == null)
                {
                    if (HiddenElementsGuid != null)
                    {
                        m_CachedHiddenElementModels = new List<GraphElementModel>();
                        foreach (var elementModelGuid in HiddenElementsGuid)
                        {
                            foreach (var node in GraphModel.NodeModels)
                            {
                                if (node != null && node.Guid.ToString() == elementModelGuid)
                                {
                                    m_CachedHiddenElementModels.Add(node);
                                }
                            }

                            foreach (var sticky in GraphModel.StickyNoteModels)
                            {
                                if (sticky.Guid.ToString() == elementModelGuid)
                                {
                                    m_CachedHiddenElementModels.Add(sticky);
                                }
                            }

                            foreach (var placemat in GraphModel.PlacematModels)
                            {
                                if (placemat.Guid.ToString() == elementModelGuid)
                                {
                                    m_CachedHiddenElementModels.Add(placemat);
                                }
                            }
                        }
                    }
                }

                return m_CachedHiddenElementModels ?? Enumerable.Empty<GraphElementModel>();
            }
            set
            {
                if (value == null)
                {
                    m_HiddenElements = null;
                }
                else
                {
                    m_HiddenElements = new List<string>(value.Select(e => e.Guid.ToString()));
                }

                m_CachedHiddenElementModels = null;
            }
        }

        /// <summary>
        /// Returns the Z-order of the placemat in the graph.
        /// </summary>
        /// <returns>The Z-order of the placemat in the graph.</returns>
        public virtual int GetZOrder() => GraphModel.PlacematModels.IndexOf_Internal(this);

        /// <inheritdoc />
        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();

            m_CachedHiddenElementModels = null;

            if (Version <= SerializationVersion.GTF_V_0_8_2)
            {
                if (DefaultColor != SerializedColor_Internal)
                {
                    // sets HasUserColor properly
                    Color = SerializedColor_Internal;
                }
            }
        }
    }
}
