// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Base class for graph assets. Uses Unity serialization by default.
    /// </summary>
    [Serializable]
    abstract class GraphAsset : ScriptableObject, ISerializationCallbackReceiver
    {
        [SerializeReference]
        GraphModel m_GraphModel;

        /// <summary>
        /// The name of the graph.
        /// </summary>
        public string Name
        {
            get => name;
            set => name = value;
        }

        /// <summary>
        /// The graph model stored in the asset.
        /// </summary>
        public virtual GraphModel GraphModel => m_GraphModel;

        /// <summary>
        /// The type of the graph model.
        /// </summary>
        protected abstract Type GraphModelType { get; }

        /// <summary>
        /// The path on disk of the graph asset.
        /// </summary>
        public virtual string FilePath => AssetDatabase.GetAssetPath(this);

        /// <summary>
        /// The dirty state of the asset (true if it needs to be saved)
        /// </summary>
        public virtual bool Dirty
        {
            get => EditorUtility.IsDirty(this);
            set
            {
                if (value)
                {
                    EditorUtility.SetDirty(this);
                }
                else
                {
                    EditorUtility.ClearDirty(this);
                }
            }
        }

        /// <summary>
        /// Version tracking for changes occuring externally.
        /// </summary>
        public uint Version { get; protected set; }

        /// <summary>
        /// Initializes <see cref="GraphModel"/> to a new graph.
        /// </summary>
        /// <param name="stencilType">The type of <see cref="StencilBase"/> associated with the new graph.</param>
        public virtual void CreateGraph(Type stencilType = null)
        {
            Debug.Assert(typeof(GraphModel).IsAssignableFrom(GraphModelType));
            var graphModel = (GraphModel)Activator.CreateInstance(GraphModelType);
            if (graphModel == null)
                return;

            graphModel.StencilType = stencilType ?? graphModel.DefaultStencilType;
            graphModel.Asset = this;
            m_GraphModel = graphModel;

            graphModel.OnEnable();

            Dirty = true;
        }

        /// <summary>
        /// Creates a file to store the asset.
        /// </summary>
        /// <param name="path">The path of the file.</param>
        /// <param name="overwriteIfExists">If there is already a file at <paramref name="path"/>, the file will be overwritten if this parameter is true. If the parameter is false, a unique path will be generated for the file.</param>
        /// <returns>The file path.</returns>
        public virtual string CreateFile(string path, bool overwriteIfExists)
        {
            if (!string.IsNullOrEmpty(path))
            {
                if (!overwriteIfExists)
                {
                    path = AssetDatabase.GenerateUniqueAssetPath(path);
                }

                Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "");
                if (File.Exists(path))
                    AssetDatabase.DeleteAsset(path);
                AssetDatabase.CreateAsset(this, path);
            }

            return path;
        }

        /// <summary>
        /// Saves the asset to the file.
        /// </summary>
        public virtual void Save()
        {
            AssetDatabase.SaveAssetIfDirty(this);
        }

        /// <summary>
        /// Import the asset from the file, returning the imported asset as a result.
        /// </summary>
        /// <remarks>If the import can be done in place, the returned asset is this objet. Otherwise, a new asset object is returned.</remarks>
        /// <returns>The imported asset.</returns>
        public virtual GraphAsset Import()
        {
            return this;
        }

        /// <summary>
        /// Implementation of OnEnable event function.
        /// </summary>
        protected virtual void OnEnable()
        {
            m_GraphModel?.OnEnable();
        }

        /// <summary>
        /// Implementation of OnDisable event function.
        /// </summary>
        protected virtual void OnDisable()
        {
            m_GraphModel?.OnDisable();
        }

        /// <inheritdoc />
        public void OnBeforeSerialize()
        {
        }

        /// <inheritdoc />
        public void OnAfterDeserialize()
        {
            if (m_GraphModel != null)
                m_GraphModel.Asset = this;
        }
    }
}
