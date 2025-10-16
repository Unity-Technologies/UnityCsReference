// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEngine;

namespace Unity.GraphToolkit.Editor.Implementation
{

    [Serializable]
    class GraphObjectImp : GraphObject
    {
        Type m_GraphType;

        static bool s_LoadingGraphObjectFromFileOnDisk;

        public Type GraphType
        {
            get
            {
                if (m_GraphType == null)
                {
                    var filePath = FilePath;
                    if (!string.IsNullOrEmpty(filePath))
                        m_GraphType = PublicGraphFactory.GetGraphTypeByExtension(Path.GetExtension(filePath));
                }
                return m_GraphType;
            }
            internal set
            {
                m_GraphType = value;
            }
        }


        public static GraphObject LoadGraphObjectFromFileOnDisk(string filePath)
        {
            s_LoadingGraphObjectFromFileOnDisk = true;
            try
            {
                return DefaultLoadGraphObjectFromFileOnDisk<GraphObjectImp>(filePath);
            }
            finally
            {
                s_LoadingGraphObjectFromFileOnDisk = false;
            }
        }

        protected override void OnEnable()
        {
            if (s_LoadingGraphObjectFromFileOnDisk)
                return;

            base.OnEnable();
        }

        protected override void OnAfterLoad()
        {
            GraphModel?.OnEnable();
        }
    }
}
