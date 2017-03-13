// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityEditor
{
    internal class GameObjectTreeViewItem : TreeViewItem
    {
        int m_ColorCode;
        Object m_ObjectPPTR;
        bool m_ShouldDisplay;
        Scene m_UnityScene;

        public GameObjectTreeViewItem(int id, int depth, TreeViewItem parent, string displayName)
            : base(id, depth, parent, displayName)
        {
        }

        public override string displayName
        {
            get
            {
                if (string.IsNullOrEmpty(base.displayName))
                {
                    if (m_ObjectPPTR != null)
                        displayName = objectPPTR.name;
                    else
                        displayName = "deleted gameobject";
                }
                return base.displayName;
            }
            set { base.displayName = value; }
        }

        virtual public int colorCode { get { return m_ColorCode; } set { m_ColorCode = value; } }
        virtual public Object objectPPTR { get { return m_ObjectPPTR; } set { m_ObjectPPTR = value; } }
        virtual public bool shouldDisplay { get { return m_ShouldDisplay; } set { m_ShouldDisplay = value; } }

        public bool isSceneHeader { get; set; }
        public Scene scene
        {
            get { return m_UnityScene; }
            set { m_UnityScene = value; }
        }
    }
} // UnityEditor
