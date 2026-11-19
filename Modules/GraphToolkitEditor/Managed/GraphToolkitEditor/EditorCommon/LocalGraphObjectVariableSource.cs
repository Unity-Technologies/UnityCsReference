// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A source of variables that is a <see cref="GraphObject"/> stored as a subasset in the file
    /// of the <see cref="GraphObject"/> that is importing the variables.
    /// </summary>
    [Serializable]
    [MovedFrom(false, "", "", "LocalGraphAssetVariableSource")]
    [UnityRestricted]
    internal class LocalGraphObjectVariableSource : ExternalVariableSource, IObjectClonedCallbackReceiver
    {
        [FormerlySerializedAs("m_GraphAsset")]
        [SerializeReference]
        GraphObject m_GraphObject;

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalGraphObjectVariableSource"/> class.
        /// </summary>
        /// <param name="graphObject">The <see cref="GraphObject"/> that provides the variables.</param>
        public LocalGraphObjectVariableSource(GraphObject graphObject)
        {
            m_GraphObject = graphObject;
        }

        /// <inheritdoc />
        public override bool IsSame(ExternalVariableSource other)
        {
            return other is LocalGraphObjectVariableSource otherLocalPropertiesSource &&
                m_GraphObject == otherLocalPropertiesSource.m_GraphObject;
        }

        /// <inheritdoc />
        public override void GetVariableDeclarations(List<VariableDeclarationModelBase> outList)
        {
            outList.Clear();

            if (m_GraphObject == null || m_GraphObject.GraphModel == null)
                return;

            foreach (var variableDeclarationModel in m_GraphObject.GraphModel.VariableDeclarations)
            {
                if (variableDeclarationModel is ExternalVariableDeclarationModelBase)
                    continue;

                outList.Add(variableDeclarationModel);
            }
        }

        /// <inheritdoc />
        public override VariableDeclarationModelBase GetVariableDeclaration(UnityEngine.Hash128 variableGuid)
        {
            if (m_GraphObject != null && m_GraphObject.GraphModel != null &&
                m_GraphObject.GraphModel.TryGetModelFromGuid(variableGuid, out var model) &&
                model is VariableDeclarationModelBase declaration)
            {
                return declaration;
            }

            return null;
        }

        /// <inheritdoc />
        public override void SetDirty()
        {
            if (m_GraphObject == null)
                return;

            m_GraphObject.Dirty = true;
        }

        /// <inheritdoc />
        public virtual void CloneAssets(List<Object> clones, Dictionary<Object, Object> originalToCloneMap)
        {
        }

        /// <inheritdoc />
        public virtual void OnAfterAssetClone(IReadOnlyDictionary<Object, Object> originalToCloneMap)
        {
            if (originalToCloneMap.TryGetValue(m_GraphObject, out var clonedSourceObject))
            {
                m_GraphObject = clonedSourceObject as GraphObject;
            }
        }
    }
}
