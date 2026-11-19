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
using UnityEngine.Bindings;

namespace Unity.UIToolkit.Editor;

[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
sealed partial class PanelElement : VisualElement, IEditorAuthoringPanelHost
{
    internal class PanelElementRootVisualElement : TemplateContainer;

    public class PanelOwner : ScriptableObject;

    public delegate void OnAfterRepaintHandler(PanelElement panel);

    // Implemented by both sub-panel types so PanelElement can drive them without knowing the context type.
    internal interface IPanelElementSubPanel
    {
        PanelElement Owner { get; set; }
        PanelElementRootVisualElement Root { get; }
        RenderTexture TargetTexture { get; set; }
    }

    internal class RuntimePanel : BaseRuntimePanel, IAuthoringPanel, IPanelElementSubPanel
    {
        readonly Action m_BaseRender;

        public RenderTexture TargetTexture
        {
            get => targetTexture;
            set => targetTexture = value;
        }

        public PanelElement Owner { get; set; }
        public PanelElementRootVisualElement Root { get; }

        public RuntimePanel(ScriptableObject ownerObject) : base(ownerObject, EventDispatcher.CreateDefault())
        {
            m_BaseRender = base.Render;
            CreateMenuFunctor = () => new GenericDropdownMenu();
            focusController = new FocusController(new NavigateFocusRing(visualTree));
            Root = AddRoot(visualTree);
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

        public override void Render() => Owner.RenderSubPanelContent(this, m_BaseRender);
    }

    internal class EditorSubPanel : EditorAuthoringPanel, IPanelElementSubPanel
    {
        readonly Action m_BaseRender;

        public RenderTexture TargetTexture
        {
            get => targetTexture;
            set => targetTexture = value;
        }

        public PanelElement Owner { get; set; }
        public PanelElementRootVisualElement Root { get; }

        public EditorSubPanel(ScriptableObject ownerObject) : base(ownerObject)
        {
            m_BaseRender = base.Render;
            Root = AddRoot(visualTree);
        }

        public override void TickSchedulingUpdaters()
        {
            Owner.ApplyPanelSettings(Owner.PanelSettings);
            base.TickSchedulingUpdaters();
        }

        public override void Render() => Owner.RenderSubPanelContent(this, m_BaseRender);
    }

    static PanelElementRootVisualElement AddRoot(VisualElement visualTree)
    {
        var root = new PanelElementRootVisualElement();
        visualTree.Add(root);
        root.pseudoStates |= PseudoStates.Root;
        root.style.zIndex = 0;
        return root;
    }

    bool m_LoggedOnBeforeRenderException;

    void RenderSubPanelContent(Panel subPanel, Action baseRender)
    {
        m_TargetPrefilled = false;

        // Do not render if any dimension is 0, since this would lead to exception with the RenderTexture.
        if (SubPanelSize.x == 0 || SubPanelSize.y == 0)
            return;

        try
        {
            OnBeforeRender?.Invoke();
            m_LoggedOnBeforeRenderException = false;
        }
        catch (Exception e)
        {
            // A partial prefill must not skip the clear, and a deterministic throw would log once per repaint.
            m_TargetPrefilled = false;
            if (!m_LoggedOnBeforeRenderException)
            {
                m_LoggedOnBeforeRenderException = true;
                Debug.LogException(e);
            }
        }

        if (!m_TargetPrefilled)
        {
            baseRender();
        }
        else
        {
            // Restored in the finally: render paths outside the tick (the engine panel sweeps) never re-apply
            // the panel settings, so a one-shot skip left in clearSettings would leak into their next render.
            var restoreClearSettings = subPanel.clearSettings;
            var settings = subPanel.clearSettings;
            settings.clearColor = false;
            subPanel.clearSettings = settings;
            try
            {
                baseRender();
            }
            finally
            {
                subPanel.clearSettings = restoreClearSettings;
            }
        }

        CopyToDisplayTexture();
    }

    // PanelElement itself is often never attached to a panel; set to whichever VisualElement is.
    internal VisualElement CursorHost { get; set; }

    // Event positions forwarded to the sub-panel are local to the host, so mapping back is the inverse scaling.
    VisualElement IEditorAuthoringPanelHost.hostElement => CursorHost ?? this;

    Rect IEditorAuthoringPanelHost.PanelToHost(Rect panelRect)
    {
        var pixelsPerPoint = SubPanel?.pixelsPerPoint ?? 1.0f;
        return new Rect(panelRect.position / pixelsPerPoint, panelRect.size / pixelsPerPoint);
    }

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

    // Raised before a ContextType change releases a created sub-panel, while it can still be detached from registries.
    public event Action<PanelElement> SubPanelReleasing;

    // Raised after a ContextType change replaced a created sub-panel. The new sub-root is empty.
    public event Action<PanelElement> SubPanelRecreated;

    // Raised inside Render itself, so it wraps every render path, including the engine's panel sweeps.
    public event Action OnBeforeRender;

    // Raised after the theme the sub-panel uses changed, whether it comes from Theme or from the panel settings.
    public event Action<PanelElement> ThemeChanged;

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
            {
                SubPanelReleasing?.Invoke(this);
                ReleaseSubPanel();
            }

            m_ContextType = value;

            if (wasCreated)
            {
                AcquireSubPanel();

                // Listeners rebind to the new render texture, so it must exist before they are told.
                ApplyPanelSettings(m_PanelSettings);
                SubPanelRecreated?.Invoke(this);
            }
        }
    }

    public Panel SubPanel { get; private set; }

    internal IPanelElementSubPanel SubPanelContent => SubPanel as IPanelElementSubPanel;

    public VisualElement subRootVisualElement => SubPanelContent?.Root;

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
            DetachContentBeforeDispose();
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
                    SubPanel = UIElementsRuntimeUtility.FindOrCreateAuthoringPanel(m_PanelOwner, CreateEditorPanelFunc);
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

        SubPanelContent.Owner = this;
        if (SubPanel is EditorAuthoringPanel editorPanel)
        {
            editorPanel.host = this;
            editorPanel.hostRepainting += OnHostRepainting;
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
        SubPanel = UIElementsRuntimeUtility.FindOrCreateAuthoringPanel(m_PanelOwner, CreateEditorPanelFunc);
        UIElementsEditorRuntimeUtility.CreateRuntimePanelDebug(SubPanel);
    }

    static Panel CreateEditorPanelFunc(ScriptableObject owner) => new EditorSubPanel(owner);

    void ReleaseSubPanel()
    {
        if (SubPanel == null)
            throw new InvalidOperationException("Trying to release a panel that does not exist.");

        if (!m_PanelOwner)
            throw new InvalidOperationException("Trying to release a panel that does not have an owning object.");

        Panel.afterRepaint -= InvokeAfterRepaint;

        DetachContentBeforeDispose();
        UIElementsRuntimeUtility.DisposeAuthoringPanel(m_PanelOwner);

        // Keep the owner registered so it survives domain reload and playmode transitions
        // Do NOT destroy it: Object.DestroyImmediate(m_PanelOwner);

        DisposeRenderTexture();
        m_PanelOwner = null;
        SubPanel = null;

        // The theme lived on the released sub-panel; the next one must receive it again.
        m_AppliedTheme = null;
    }

    // An open GenericDropdownMenu hides itself when its target detaches, which breaks the visual tree clear in
    // Panel.Dispose; detaching the content first lets it close normally.
    void DetachContentBeforeDispose()
    {
        subRootVisualElement?.Clear();
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
