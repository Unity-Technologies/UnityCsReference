// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;

// The setup information for a scene in the SceneManager.

namespace UnityEditor.SceneManagement
{
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    public class SceneSetup
    {
        [SerializeField]
        private string m_Path = null;
        [SerializeField]
        private bool m_IsLoaded = false;
        [SerializeField]
        private bool m_IsActive = false;

        public string path
        {
            get { return m_Path; }
            set { m_Path = value; }
        }

        public bool isLoaded
        {
            get { return m_IsLoaded; }
            set { m_IsLoaded = value; }
        }

        public bool isActive
        {
            get { return m_IsActive; }
            set { m_IsActive = value; }
        }
    }
}
