// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.SceneManagement;
using TreeViewItem = UnityEditor.IMGUI.Controls.TreeViewItem<int>;


namespace UnityEditor
{
    internal class GameObjectTreeViewItem : TreeViewItem
    {
        int m_ColorCode;
        Object m_ObjectPPTR;
        Scene m_UnityScene;

        // Lazy initialized together with icon.
        bool m_LazyInitializationDone;
        bool m_ShowPrefabModeButton;
        Texture2D m_OverlayIcon;
        Texture2D m_SelectedIcon;

        public GameObjectTreeViewItem(int id, int depth, TreeViewItem parent, string displayName)
            : base(id, depth, parent, displayName)
        {
        }

        public override string displayName
        {
            get
            {
                // Lazy initilize displayName
                if (base.displayName == null)
                {
                    if (SubSceneGUI.IsUsingSubScenes())
                    {
                        var subSceneHeaderName = SubSceneGUI.GetSubSceneHeaderText((GameObject)objectPPTR);
                        if (subSceneHeaderName != null)
                        {
                            base.displayName = subSceneHeaderName;
                            return base.displayName;
                        }
                    }

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
        virtual public bool lazyInitializationDone { get { return m_LazyInitializationDone; } set { m_LazyInitializationDone = value; } }
        virtual public bool showPrefabModeButton { get { return m_ShowPrefabModeButton; } set { m_ShowPrefabModeButton = value; } }
        virtual public Texture2D overlayIcon { get { return m_OverlayIcon; } set { m_OverlayIcon = value; } }
        virtual public Texture2D selectedIcon { get { return m_SelectedIcon; } set { m_SelectedIcon = value; } }

        public bool isSceneHeader { get; set; }
        public Scene scene
        {
            get { return m_UnityScene; }
            set { m_UnityScene = value; }
        }
    }
} // UnityEditor
