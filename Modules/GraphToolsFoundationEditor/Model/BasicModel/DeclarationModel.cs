// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// A model that represents a declaration (e.g. a variable) in a graph.
    /// </summary>
    [Serializable]
    [MovedFrom(false, "Unity.GraphToolsFoundation.Editor", "Unity.GraphTools.Foundation.Model")]
    class DeclarationModel : GraphElementModel, IHasTitle, IRenamable
    {
        [FormerlySerializedAs("name")]
        [SerializeField, HideInInspector]
        string m_Name;

        internal static string nameFieldName_Internal = nameof(m_Name);

        /// <inheritdoc />
        public virtual string Title
        {
            get => m_Name;
            set
            {
                if (m_Name == value)
                    return;
                m_Name = value;
                GraphModel?.CurrentGraphChangeDescription?.AddChangedModel(this, ChangeHint.Data);
            }
        }

        /// <inheritdoc />
        public virtual string DisplayTitle => Title.Nicify();

        /// <summary>
        /// Initializes a new instance of the <see cref="DeclarationModel"/> class.
        /// </summary>
        public DeclarationModel()
        {
            m_Capabilities.AddRange(new[]
            {
                Editor.Capabilities.Deletable,
                Editor.Capabilities.Droppable,
                Editor.Capabilities.Copiable,
                Editor.Capabilities.Selectable,
                Editor.Capabilities.Renamable
            });
        }

        /// <inheritdoc />
        public virtual void Rename(string newName)
        {
            if (!this.IsRenamable())
                return;

            Title = newName;
        }
    }
}
