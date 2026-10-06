// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using Unity.Scripting.LifecycleManagement;

namespace Unity.UIToolkit.Editor;

sealed partial class PanelElement : VisualElement
{
    internal class PanelElementRootVisualElement : TemplateContainer;

    public class PanelOwner : ScriptableObject;

    public delegate void OnAfterRepaintHandler(PanelElement panel);


    internal class RuntimePanel : BaseRuntimePanel, IAuthoringPanel
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
            Root.style.zIndex = 0;
            resetPanelRenderingOnAssetChange = true;
        }

        protected internal override PanelSettings GetLinkedPanelSettings()
        {
            return Owner.PanelSettings;
        }

        public override void TickSchedulingUpdaters()
        {
            // Required here because we will use the settings from an "external" panel settings and we need to
            // change some of the resolved values (i.e. the "display rect" will most likely be different).
            Owner.ApplyPanelSettings(Owner.PanelSettings);
            base.TickSchedulingUpdaters();
        }

        bool m_LoggedOnBeforeRenderException;

        public override void Render()
        {
            Owner.m_TargetPrefilled = false;

            // Do not render if any dimension is 0, since this would lead to exception with the RenderTexture.
            if (Owner.SubPanelSize.x == 0 || Owner.SubPanelSize.y == 0)
                return;

            try
            {
                Owner.OnBeforeRender?.Invoke();
                m_LoggedOnBeforeRenderException = false;
            }
            catch (Exception e)
            {
                // A partial prefill must not skip the clear, and a deterministic throw would log once per repaint.
                Owner.m_TargetPrefilled = false;
                if (!m_LoggedOnBeforeRenderException)
                {
                    m_LoggedOnBeforeRenderException = true;
                    Debug.LogException(e);
                }
            }

            if (!Owner.m_TargetPrefilled)
            {
                base.Render();
                return;
            }

            // Restored in the finally: render paths outside the tick (the engine panel sweeps) never re-apply
            // the panel settings, so a one-shot skip left in clearSettings would leak into their next render.
            var restoreClearSettings = clearSettings;
            var settings = clearSettings;
            settings.clearColor = false;
            clearSettings = settings;
            try
            {
                base.Render();
            }
            finally
            {
                clearSettings = restoreClearSettings;
            }
        }
    }

    // PanelElement itself is often never attached to a panel; set to whichever VisualElement is.
    internal VisualElement CursorHost { get; set; }

    // Keeps previewed-content cursor requests inside the host window's own GUIView cursor reconciliation.
    sealed class ForwardingCursorManager : ICursorManager
    {
        readonly PanelElement m_Owner;

        public ForwardingCursorManager(PanelElement owner)
        {
            m_Owner = owner;
        }

        BaseVisualElementPanel HostPanel => (m_Owner.CursorHost ?? m_Owner).elementPanel;

        public void SetCursor(UnityEngine.UIElements.Cursor cursor) => HostPanel?.cursorManager.SetCursor(cursor);
        public void ResetCursor() => HostPanel?.cursorManager.ResetCursor();
    }

    [NoAutoStaticsCleanup] // shared empty animation-system, safe to persist
    static readonly EmptyStylePropertyAnimationSystem s_EmptyAnimationSystem = new EmptyStylePropertyAnimationSystem();

    bool m_AnimationEnabled;
    PanelOwner m_PanelOwner;
    ContextType m_ContextType = ContextType.Player;
    EntityId m_PanelOwnerKey;

    public override VisualElement contentContainer => null;

    public bool IsCreated => SubPanel != null && m_PanelOwner;

    public event OnAfterRepaintHandler OnAfterRepaint;

    // Raised inside Render itself, so it wraps every render path, including the engine's panel sweeps.
    public event Action OnBeforeRender;

    bool m_TargetPrefilled;

    // Valid during OnBeforeRender: skips the sub-panel's color clear for this render only.
    // Internal until the callback carries a context object to scope this to (review r1093059).
    internal void MarkTargetPrefilled() => m_TargetPrefilled = true;

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

        SubPanel.cursorManager = new ForwardingCursorManager(this);

        Panel.afterRepaint -= InvokeAfterRepaint;
        Panel.afterRepaint += InvokeAfterRepaint;

        SubPanel.liveReloadSystem.enable = true;
        ApplyAnimationSystem();
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

    public void EnableAnimationSystem(bool enable)
    {
        m_AnimationEnabled = enable;
        ApplyAnimationSystem();
    }

    void ApplyAnimationSystem()
    {
        if (SubPanel == null)
            return;

        if (m_AnimationEnabled)
        {
            if (SubPanel.styleAnimationSystem is not StylePropertyAnimationSystem)
                SubPanel.styleAnimationSystem = new StylePropertyAnimationSystem(SubPanel);
        }
        else
        {
            if (SubPanel.styleAnimationSystem != s_EmptyAnimationSystem)
                SubPanel.styleAnimationSystem = s_EmptyAnimationSystem;
        }
    }
}
