// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A model that represents a declaration (e.g. a variable) in a graph.
    /// </summary>
    [Serializable]
    [UnityRestricted]
    internal class DeclarationModel : GraphElementModel, IHasTitle, IRenamable
    {
        [FormerlySerializedAs("name")]
        [SerializeField, HideInInspector]
        string m_Name;

        internal static string nameFieldName = nameof(m_Name);

        /// <inheritdoc />
        /// <remarks>Setter implementations should set the <see cref="ChangeHint.Data"/> change hint.</remarks>
        public virtual string Title
        {
            get => m_Name;
            set
            {
                if (m_Name == value)
                    return;
                m_Name = value;
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Data);
            }
        }

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
                Editor.Capabilities.Renamable,
                Editor.Capabilities.Editable
            });
        }

        /// <inheritdoc />
        public virtual void Rename(string newName)
        {
            if (!IsRenamable())
                return;

            Title = newName;
        }
    }
}
