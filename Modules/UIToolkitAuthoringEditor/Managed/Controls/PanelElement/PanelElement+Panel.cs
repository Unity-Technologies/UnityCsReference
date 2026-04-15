// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.UIToolkit.Editor;

sealed partial class PanelElement : VisualElement
{
    internal class PanelElementRootVisualElement : TemplateContainer;

    public class PanelOwner : ScriptableObject;

    public delegate void OnAfterRepaintHandler(PanelElement panel);


    internal class RuntimePanel : BaseRuntimePanel
    {
        public RenderTexture TargetTexture => targetTexture;
        public PanelElement Owner;
        public readonly PanelElementRootVisualElement Root;

        public RuntimePanel(ScriptableObject ownerObject) : base(ownerObject, EventDispatcher.CreateDefault())
        {
            CreateMenuFunctor = () => new GenericDropdownMenu();
            focusController = new FocusController(new NavigateFocusRing(visualTree));
            visualTree.Add(Root = new PanelElementRootVisualElement());
            Root.pseudoStates |= PseudoStates.Root;
            resetPanelRenderingOnAssetChange = true;
        }

        public override void TickSchedulingUpdaters()
        {
            // Required here because we will use the settings from an "external" panel settings and we need to
            // change some of the resolved values (i.e. the "display rect" will most likely be different).
            Owner.ApplyPanelSettings(Owner.PanelSettings);
            base.TickSchedulingUpdaters();
        }

        public override void Render()
        {
            // Do not render if any dimension is 0, since this would lead to exception with the RenderTexture.
            if (Owner.SubPanelSize.x == 0 || Owner.SubPanelSize.y == 0)
                return;

            base.Render();
        }
    }

    PanelOwner m_PanelOwner;
    ContextType m_ContextType = ContextType.Player;
    EntityId m_PanelOwnerKey;

    public override VisualElement contentContainer => null;

    public bool IsCreated => SubPanel != null && m_PanelOwner;

    public event OnAfterRepaintHandler OnAfterRepaint;

    public ContextType ContextType
    {
        get => m_ContextType;
        set
        {
            if (m_ContextType == value)
                return;

            var wasCreated = IsCreated;

            if (wasCreated)
                ReleaseSubPanel();

            m_ContextType = value;

            if (wasCreated)
                AcquireSubPanel();
        }
    }

    public Panel SubPanel { get; private set; }

    public VisualElement subRootVisualElement
    {
        get
        {
            switch (SubPanel)
            {
                case RuntimePanel runtimePanel:
                    return runtimePanel.Root;
                // case EditorPanel editorPanel:
                //     return editorPanel.visualTree;
            }
            return null;
        }
    }

    public void SetPanelSize(Vector2 size)
    {
        SetSize(size);
        if (panel == null)
        {
            ResizeRenderTexture(size);
        }
    }

    public void CreateSubPanel()
    {
        if (IsCreated)
            return;
        AcquireSubPanel();
    }

    public void DestroySubPanel()
    {
        if (!IsCreated)
            return;
        ReleaseSubPanel();
    }

    /// <summary>
    /// Permanently destroys the panel and removes its owner from the registry.
    /// Use this when the PanelElement is being permanently destroyed and should not be restored.
    /// </summary>
    public void DestroyPanelPermanently()
    {
        if (IsCreated)
        {
            UIElementsRuntimeUtility.DisposeAuthoringPanel(m_PanelOwner);
        }

        if (m_PanelOwner != null)
        {
            Object.DestroyImmediate(m_PanelOwner);
        }

        if (m_PanelOwnerKey.IsValid())
        {
            PanelOwnerRegistry.instance.Unregister(m_PanelOwnerKey);
        }

        Panel.afterRepaint -= InvokeAfterRepaint;
        DisposeRenderTexture();
        m_PanelOwner = null;
        SubPanel = null;
        m_PanelOwnerKey = EntityId.None;
    }

    private void AcquireSubPanel()
    {
        Assert.IsNull(m_PanelOwner);
        // We currently only support runtime panels, so let's make sure we error out early when an editor panel is
        // queried.
        Assert.IsTrue(m_ContextType == ContextType.Player, $"{nameof(PanelElement)} does not support editor panels.");

        // Try to retrieve existing owner from registry if we have a valid key
        if (m_PanelOwnerKey.IsValid() &&
            PanelOwnerRegistry.instance.TryGetOwner(m_PanelOwnerKey, out m_PanelOwner) &&
            m_PanelOwner != null)
        {
            // Owner was retrieved from registry, recreate the panel with the existing owner
            switch (m_ContextType)
            {
                case ContextType.Player:
                    SubPanel = UIElementsRuntimeUtility.FindOrCreateAuthoringPanel(m_PanelOwner, CreateRuntimePanelFunc);
                    UIElementsEditorRuntimeUtility.CreateRuntimePanelDebug(SubPanel);
                    break;
                case ContextType.Editor:
                    SubPanel = EditorPanel.FindOrCreate(m_PanelOwner);
                    UIElementsEditorRuntimeUtility.CreateRuntimePanelDebug(SubPanel);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        else
        {
            // Create new owner if not found in registry
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

            // Get the EntityId of the newly created owner and register it
            m_PanelOwnerKey = m_PanelOwner.GetEntityId();
            PanelOwnerRegistry.instance.Register(m_PanelOwnerKey, m_PanelOwner);
        }

        // [TODO] use interface
        switch (SubPanel)
        {
            case PanelElement.RuntimePanel runtimePanel:
                runtimePanel.Owner = this;
                break;
        }

        Panel.afterRepaint -= InvokeAfterRepaint;
        Panel.afterRepaint += InvokeAfterRepaint;

        SubPanel.liveReloadSystem.enable = true;
    }

    void CreateRuntimePanel()
    {
        m_PanelOwner = CreateOwnerObject(ContextType.Player);

        SubPanel = UIElementsRuntimeUtility.FindOrCreateAuthoringPanel(m_PanelOwner, CreateRuntimePanelFunc);
        UIElementsEditorRuntimeUtility.CreateRuntimePanelDebug(SubPanel);
    }

    static BaseRuntimePanel CreateRuntimePanelFunc(ScriptableObject owner) => new RuntimePanel(owner);

    void CreateEditorPanel()
    {
        m_PanelOwner = CreateOwnerObject(ContextType.Editor);
        SubPanel = EditorPanel.FindOrCreate(m_PanelOwner);
        UIElementsEditorRuntimeUtility.CreateRuntimePanelDebug(SubPanel);
    }

    void ReleaseSubPanel()
    {
        if (SubPanel == null)
            throw new InvalidOperationException("Trying to release a panel that does not exist.");

        if (!m_PanelOwner)
            throw new InvalidOperationException("Trying to release a panel that does not have an owning object.");

        Panel.afterRepaint -= InvokeAfterRepaint;

        UIElementsRuntimeUtility.DisposeAuthoringPanel(m_PanelOwner);

        // Keep the owner registered so it survives domain reload and playmode transitions
        // Do NOT destroy it: Object.DestroyImmediate(m_PanelOwner);

        DisposeRenderTexture();
        m_PanelOwner = null;
        SubPanel = null;
    }

    void InvokeAfterRepaint(Panel p)
    {
        if (p == SubPanel)
            OnAfterRepaint?.Invoke(this);
    }

    static PanelOwner CreateOwnerObject(ContextType type)
    {
        var instance = ScriptableObject.CreateInstance<PanelOwner>();
        instance.name = $"panel-element#{type}";
        // Mark as DontSave so it doesn't get saved with scenes but persists through domain reload
        instance.hideFlags = HideFlags.DontSave | HideFlags.DontUnloadUnusedAsset;
        return instance;
    }
}
