// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.UIToolkit.Editor
{
    internal partial class PanelElement
    {
        private PanelSettings m_PanelSettings;

        public PanelSettings PanelSettings
        {
            get => m_PanelSettings;
            set => m_PanelSettings = value;
        }
    }

    internal partial class PanelElement : VisualElement
    {
        class RuntimePanel : BaseRuntimePanel
        {
            public RuntimePanel(ScriptableObject ownerObject) : base(ownerObject, EventDispatcher.CreateDefault())
            {
            }
        }

        public class PanelOwner : ScriptableObject {}

        private PanelOwner m_PanelOwner;

        public override VisualElement contentContainer => null;

        public bool IsCreated => m_NestedPanel != null && m_PanelOwner;

        private ContextType m_ContextType = ContextType.Player;

        public ContextType ContextType
        {
            get => m_ContextType;
            set
            {
                if (m_ContextType == value)
                    return;

                var wasCreated = IsCreated;

                if (wasCreated)
                    ReleaseNestedPanel();

                m_ContextType = value;

                if (wasCreated)
                    AcquireNestedPanel();
            }
        }

        private Panel m_NestedPanel;

        public Panel NestedPanel
        {
            get => m_NestedPanel;
            private set
            {
                if (m_NestedPanel == value)
                    return;
                m_NestedPanel = value;
            }
        }

        public VisualElement nestedRootVisualElement => m_NestedPanel?.visualTree;

        public void CreateNestedPanel()
        {
            if (IsCreated)
                return;
            AcquireNestedPanel();
        }

        public void DestroyNestedPanel()
        {
            if (!IsCreated)
                return;
            ReleaseNestedPanel();
        }

        private void AcquireNestedPanel()
        {
            Assert.IsNull(m_PanelOwner);

            switch (m_ContextType)
            {
                case ContextType.Player:
                    CreateRuntimePanel();
                    break;
                case ContextType.Editor:
                    CreateEditorPanel();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            m_NestedPanel.liveReloadSystem.enable = true;
        }

        private void CreateRuntimePanel()
        {
            m_PanelOwner = CreateOwnerObject(ContextType.Player);

            m_NestedPanel = UIElementsRuntimeUtility.FindOrCreateAuthoringPanel(m_PanelOwner, CreateRuntimePanelFunc);
            UIElementsEditorRuntimeUtility.CreateRuntimePanelDebug(m_NestedPanel);
        }

        static BaseRuntimePanel CreateRuntimePanelFunc(ScriptableObject owner) => new RuntimePanel(owner);

        private void CreateEditorPanel()
        {
            m_PanelOwner = CreateOwnerObject(ContextType.Editor);
            m_NestedPanel = EditorPanel.FindOrCreate(m_PanelOwner);
            UIElementsEditorRuntimeUtility.CreateRuntimePanelDebug(m_NestedPanel);
        }

        private void ReleaseNestedPanel()
        {
            if (m_NestedPanel == null)
                throw new InvalidOperationException("Trying to release a panel that does not exist.");

            if (!m_PanelOwner)
                throw new InvalidOperationException("Trying to release a panel that does not have an owning object.");

            UIElementsRuntimeUtility.DisposeAuthoringPanel(m_PanelOwner);
            Object.DestroyImmediate(m_PanelOwner);
            m_PanelOwner = null;
        }

        static PanelOwner CreateOwnerObject(ContextType type)
        {
            var instance = ScriptableObject.CreateInstance<PanelOwner>();
            instance.name = $"panel-element#{type}";
            return instance;
        }
    }
}
