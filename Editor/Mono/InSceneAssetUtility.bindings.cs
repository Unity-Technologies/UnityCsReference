// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.SceneManagement;

namespace UnityEditor
{
    [NativeHeader("Editor/Src/InSceneAssetUtility.h")]
    public struct InSceneAssetInformation
    {
        [NativeName("inSceneAsset")]
        Object m_InSceneAsset;

        [NativeName("referencingObjects")]
        Object[] m_ReferencingObjects;

        public Object inSceneAsset
        {
            get { return m_InSceneAsset; }
            set { m_InSceneAsset = value; }
        }

        public Object[] referencingObjects
        {
            get { return m_ReferencingObjects; }
            set { m_ReferencingObjects = value; }
        }
    }

    [NativeHeader("Editor/Src/InSceneAssetUtility.h")]
    public static class InSceneAssetUtility
    {
        [FreeFunction("CollectInSceneAssetsFromGameObjectsInternal")]
        static extern InSceneAssetInformation[] CollectInSceneAssetsFromGameObjectsInternal(GameObject[] gameObjects);

        [FreeFunction("CollectInSceneAssetsFromSceneInternal")]
        static extern InSceneAssetInformation[] CollectInSceneAssetsFromSceneInternal(Scene scene);

        [FreeFunction("IsInSceneAsset")]
        public static extern bool IsInSceneAsset(Object sourceObject);

        public static InSceneAssetInformation[] CollectInSceneAssets(GameObject[] gameObjects)
        {
            return CollectInSceneAssetsFromGameObjectsInternal(gameObjects);
        }

        public static InSceneAssetInformation[] CollectInSceneAssets(Scene scene)
        {
            return CollectInSceneAssetsFromSceneInternal(scene);
        }

        [FreeFunction("CreateAssetFromInSceneAsset")]
        public static extern bool CreateAssetFromInSceneAsset(Object inSceneAsset, string filePath);

        [FreeFunction("CreateInSceneAssetFromAsset")]
        public static extern Object CreateInSceneAssetFromAsset(Object asset, GameObject gameObjectReferencingAsset);
    }
}
