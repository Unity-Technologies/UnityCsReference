// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A reference to a <see cref="VariableDeclarationModelBase"/> in another graph.
    /// </summary>
    [Serializable]
    [UnityRestricted]
    internal class ExternalVariableDeclarationModel : ExternalVariableDeclarationModelBase, IObjectClonedCallbackReceiver
    {
        [SerializeReference, HideInInspector]
        ExternalVariableSource m_VariableSource;

        [SerializeField, HideInInspector]
        Hash128 m_ExternalVariableGuid;

        public ExternalVariableSource VariableSource => m_VariableSource;

        /// <summary>
        /// The identifier of the external variable declaration.
        /// </summary>
        public Hash128 ExternalVariableGuid => m_ExternalVariableGuid;

        /// <inheritdoc />
        protected override void SetSourceDirty()
        {
            m_VariableSource.SetDirty();
        }

        /// <inheritdoc />
        protected override VariableDeclarationModelBase GetExternalVariableDeclaration()
        {
            return m_VariableSource.GetVariableDeclaration(m_ExternalVariableGuid);
        }

        /// <summary>
        /// Sets the reference to the variable declaration.
        /// </summary>
        /// <param name="externalVariableSource">The source object in which the variable is declared.</param>
        /// <param name="variableGuid">The variable guid in the graph object.</param>
        public void SetReference(ExternalVariableSource externalVariableSource, Hash128 variableGuid)
        {
            m_VariableSource = externalVariableSource;
            m_ExternalVariableGuid = variableGuid;
        }

        /// <summary>
        /// Whether this <see cref="ExternalVariableDeclarationModel"/> is referring to the given variable in the given graph.
        /// </summary>
        /// <param name="externalVariableSource">The source object containing the variable.</param>
        /// <param name="variableGuid">The variable guid in the graph object.</param>
        /// <returns>True if this <see cref="ExternalVariableDeclarationModel"/> is referring to the given variable in the given graph, false otherwise.</returns>
        public bool IsReferringTo(ExternalVariableSource externalVariableSource, Hash128 variableGuid)
        {
            return m_VariableSource.IsSame(externalVariableSource) && m_ExternalVariableGuid == variableGuid;
        }

        /// <summary>
        /// Whether this <see cref="ExternalVariableDeclarationModel"/> is referring to the same variable as <paramref name="other"/>.
        /// </summary>
        /// <param name="other">The external variable reference to compare to.</param>
        /// <returns>True if this <see cref="ExternalVariableDeclarationModel"/> is referring to the same variable as <paramref name="other"/>, false otherwise</returns>
        public override bool RefersToSameVariableAs(ExternalVariableDeclarationModelBase other)
        {
            return other is ExternalVariableDeclarationModel externalVariableDeclarationModel &&
                IsReferringTo(externalVariableDeclarationModel.m_VariableSource, externalVariableDeclarationModel.m_ExternalVariableGuid);
        }

        /// <inheritdoc />
        public virtual void CloneAssets(List<Object> clones, Dictionary<Object, Object> originalToCloneMap)
        {
            if (m_VariableSource is IObjectClonedCallbackReceiver assetClonedCallbackReceiver)
            {
                assetClonedCallbackReceiver.CloneAssets(clones, originalToCloneMap);
            }
        }

        /// <inheritdoc />
        public virtual void OnAfterAssetClone(IReadOnlyDictionary<Object, Object> originalToCloneMap)
        {
            if (m_VariableSource is IObjectClonedCallbackReceiver assetClonedCallbackReceiver)
            {
                assetClonedCallbackReceiver.OnAfterAssetClone(originalToCloneMap);
            }
        }
    }
}
