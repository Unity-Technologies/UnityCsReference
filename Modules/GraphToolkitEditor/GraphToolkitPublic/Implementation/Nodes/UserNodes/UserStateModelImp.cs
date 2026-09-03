// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.GraphToolkit.Editor.Implementation
{
    /// <summary>
    /// Internal model that backs a user-defined <see cref="State"/> in a <see cref="StateMachine"/> graph.
    /// </summary>
    [Serializable]
    class UserStateModelImp : StateModel, IUserModelImp, IState
    {
        [SerializeReference]
        State m_Node;

        public State Node => m_Node;

        bool IUserModelImp.IsMissingDefinition => m_Node == null;

        [NonSerialized]
        bool m_OnEnableCalled;
        [NonSerialized]
        string m_CustomTooltip;
        [NonSerialized]
        string m_CustomTitle;
        [NonSerialized]
        string m_CustomSubtitle;

        [NonSerialized]
        Color m_CustomDefaultColor = k_DefaultColor;

        [NonSerialized]
        StateAttribute m_StateAttribute;
        [NonSerialized]
        bool m_StateAttributeResolved;

        public bool OnEnableCalled { get => m_OnEnableCalled; set => m_OnEnableCalled = value; }

        // Resolves the StateAttribute of the backing node type once and caches it to avoid repeated reflection
        // lookups (e.g. during UI repaints, rebuilds, or library searches). The cache is reset whenever m_Node changes.
        StateAttribute StateAttribute
        {
            get
            {
                if (!m_StateAttributeResolved)
                {
                    m_StateAttribute = m_Node?.GetType().GetAttribute<StateAttribute>();
                    m_StateAttributeResolved = true;
                }

                return m_StateAttribute;
            }
        }

        public override string IconPath => StateAttribute?.IconPath ?? base.IconPath;

        public override string CategoryPath => StateAttribute?.CategoryPath ?? base.CategoryPath;

        public override string Tooltip
        {
            get => !string.IsNullOrEmpty(m_CustomTooltip) ? m_CustomTooltip : base.Tooltip;
            set
            {
                if (m_CustomTooltip == value)
                    return;

                m_CustomTooltip = value;
                using var assetDirtyScope = GraphModel?.BlockAssetDirtyScope();
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Style);
            }
        }

        public override string Title
        {
            get
            {
                if (m_Node == null)
                    return "Missing State";

                var title = m_Node.GetType().Name.Nicify();

                // Prioritize editable title label
                if (!string.IsNullOrEmpty(m_Title))
                {
                    title = m_Title;
                }
                else if (!string.IsNullOrEmpty(m_CustomTitle))
                {
                    title = m_CustomTitle;
                }
                else if (StateAttribute?.Title is var attributeTitle && !string.IsNullOrEmpty(attributeTitle))
                {
                    title = attributeTitle;
                }

                return title;
            }
        }

        public void SetCustomTitle(string title)
        {
            if (m_CustomTitle == title)
                return;

            m_CustomTitle = title;
            using var assetDirtyScope = GraphModel?.BlockAssetDirtyScope();
            GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Data);
        }

        // The public IState.Title setter must write the non-serialized custom title so that setting a title from
        // State.OnEnable does not dirty the asset. The Title setter inherited from AbstractNodeModel stays reserved
        // for the serialized rename label written by StateModel.Rename and the inspector.
        string IState.Title
        {
            get => Title;
            set => SetCustomTitle(value);
        }

        public override string Subtitle
        {
            get => !string.IsNullOrEmpty(m_CustomSubtitle) ? m_CustomSubtitle : base.Subtitle;
            set
            {
                if (m_CustomSubtitle == value)
                    return;

                m_CustomSubtitle = value;
                using var assetDirtyScope = GraphModel?.BlockAssetDirtyScope();
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Data);
            }
        }

        public override Color DefaultColor
        {
            get => m_CustomDefaultColor == k_DefaultColor ? base.DefaultColor : m_CustomDefaultColor;
            set
            {
                if (m_CustomDefaultColor == value)
                    return;

                m_CustomDefaultColor = value;
                using var assetDirtyScope = GraphModel?.BlockAssetDirtyScope();
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Style);
            }
        }

        public void InitCustomState(State node)
        {
            m_Node = node;
            m_StateAttributeResolved = false;
            m_Node.SetImplementation(this);
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();

            if (m_Node == null)
                PlaceholderModelHelper.SetPlaceholderCapabilities(this);
            else
                m_Node.SetImplementation(this);
        }

        public override void OnCreateNode()
        {
            CallOnEnable();
            base.OnCreateNode();
        }

        public override void OnDuplicateNode(AbstractNodeModel sourceNode)
        {
            base.OnDuplicateNode(sourceNode);
            CallOnEnable();
        }

        public void CallOnEnable()
        {
            m_Node?.OnEnable();
            OnEnableCalled = true;
        }

        public void CallOnDisable()
        {
            OnEnableCalled = false;
            m_Node?.OnDisable();
        }
    }
}
