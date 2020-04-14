// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEngine.SceneManagement
{
    [System.Serializable]
    public partial struct Scene
    {
        internal enum LoadingState
        {
            NotLoaded = 0,
            Loading = 1,
            Loaded = 2,
            Unloading = 3
        }

        [SerializeField]
        [HideInInspector]
        private int m_Handle;

        public int handle { get { return m_Handle; } }

        internal Scene.LoadingState loadingState
        {
            get { return GetLoadingStateInternal(handle); }
        }

        internal string guid
        {
            get { return GetGUIDInternal(handle); }
        }

        public bool IsValid()
        {
            return IsValidInternal(handle);
        }

        public string path
        {
            get { return GetPathInternal(handle); }
        }

        public string name
        {
            get { return GetNameInternal(handle); }
            set { SetNameInternal(handle, value); }
        }

        public bool isLoaded
        {
            get { return GetIsLoadedInternal(handle); }
        }

        public int buildIndex
        {
            get { return GetBuildIndexInternal(handle); }
        }

        public bool isDirty
        {
            get { return GetIsDirtyInternal(handle); }
        }

        internal int dirtyID
        {
            get { return GetDirtyID(handle); }
        }

        public int rootCount
        {
            get { return GetRootCountInternal(handle); }
        }

        public bool isSubScene
        {
            get { return IsSubScene(handle); }
            set { SetIsSubScene(handle, value); }
        }

        public GameObject[] GetRootGameObjects()
        {
            var rootGameObjects = new List<GameObject>(rootCount);
            GetRootGameObjects(rootGameObjects);

            return rootGameObjects.ToArray();
        }

        public void GetRootGameObjects(List<GameObject> rootGameObjects)
        {
            if (rootGameObjects.Capacity < rootCount)
                rootGameObjects.Capacity = rootCount;

            rootGameObjects.Clear();

            if (!IsValid())
                throw new System.ArgumentException("The scene is invalid.");

            if (!Application.isPlaying && !isLoaded)
                throw new System.ArgumentException("The scene is not loaded.");

            if (rootCount == 0)
                return;

            GetRootGameObjectsInternal(handle, rootGameObjects);
        }

        public static bool operator==(Scene lhs, Scene rhs)
        {
            return lhs.handle == rhs.handle;
        }

        public static bool operator!=(Scene lhs, Scene rhs)
        {
            return lhs.handle != rhs.handle;
        }

        public override int GetHashCode()
        {
            return m_Handle;
        }

        public override bool Equals(object other)
        {
            if (!(other is Scene))
                return false;

            Scene rhs = (Scene)other;
            return handle == rhs.handle;
        }
    }
}
