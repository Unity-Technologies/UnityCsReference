// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Experimental.UIElements;
using UnityEditor;

namespace Unity.Experimental.EditorMode
{
    internal abstract class EditorWindowOverride<TWindow> : IEditorWindowOverride, IHasCustomMenu
        where TWindow : EditorWindow
    {
        private TWindow m_Window;
        private VisualElement m_Root;

        public bool InvokeOnGUIEnabled { get; protected set; } = true;

        public TWindow Window
        {
            get { return m_Window; }
            internal set { m_Window = value; }
        }

        public VisualElement Root
        {
            get { return m_Root; }
            internal set { m_Root = value; }
        }

        public VisualElement DefaultRoot
        {
            get { return m_Window.rootVisualContainer; }
        }

        public virtual void OnEnable()
        {
        }

        public virtual void OnDisable()
        {
        }

        public virtual void OnBecameVisible()
        {
        }

        public virtual void OnBecameInvisible()
        {
        }

        public virtual void Update()
        {
        }

        public virtual void OnFocus()
        {
        }

        public virtual void OnLostFocus()
        {
        }

        public virtual void OnSelectionChanged()
        {
        }

        public virtual void OnProjectChange()
        {
        }

        public virtual void OnDidOpenScene()
        {
        }

        public virtual void OnInspectorUpdate()
        {
        }

        public virtual void OnHierarchyChange()
        {
        }

        public virtual void OnResize()
        {
        }

        public virtual void ModifierKeysChanged()
        {
        }

        public virtual void OnSwitchedToOverride()
        {
        }

        public virtual void OnSwitchedToDefault()
        {
        }

        public void AddItemsToMenu(GenericMenu menu)
        {
            if (OnAddItemsToMenu(menu))
            {
                var provider = Window as IHasCustomMenu;
                if (null != provider)
                {
                    provider.AddItemsToMenu(menu);
                }
            }

            menu.AddSeparator("");

            // Allow user to toggle between default and current mode through menu items.
            if (Root.visible)
            {
                menu.AddItem(new UnityEngine.GUIContent(string.Format("Mode/{0}", EditorModes.DefaultMode.Name)), false, () =>
                {
                    SwitchToDefault();
                });
            }
            else
            {
                menu.AddItem(new UnityEngine.GUIContent(string.Format("Mode/{0}", EditorModes.CurrentModeName)), false, () =>
                {
                    SwitchToOverride();
                });
            }
        }

        public virtual bool OnAddItemsToMenu(GenericMenu menu)
        {
            return true;
        }

        protected void SwitchToDefault()
        {
            OnSwitchedToDefault();
            Root.visible = false;
            DefaultRoot.visible = true;
            InvokeOnGUIEnabled = true;
        }

        protected void SwitchToOverride()
        {
            Root.visible = true;
            DefaultRoot.visible = false;
            InvokeOnGUIEnabled = false;
            OnSwitchedToOverride();
        }
    }
}
