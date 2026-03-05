// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

// The setup information for a scene in the SceneManager.

namespace UnityEditor.SceneManagement
{
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    [RequiredByNativeCode]
    [NativeHeader("Editor/Src/SceneManager/SceneManagerSetup.h")]
    [NativeAsStruct]
    public class SceneSetup
    {
        [SerializeField]
        [NativeName("path")]
        private string m_Path = null;
        [SerializeField]
        [NativeName("isLoaded")]
        private bool m_IsLoaded = false;
        [SerializeField]
        [NativeName("isActive")]
        private bool m_IsActive = false;
        [SerializeField]
        [NativeName("isSubScene")]
        private bool m_IsSubScene = false;

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

        public bool isSubScene
        {
            get { return m_IsSubScene; }
            set { m_IsSubScene = value; }
        }
    }
}
