// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor;

/// <summary>
/// Derive from this class to add custom UI to the view generated for a specific <see cref="State"/> type.
/// </summary>
/// <remarks>
/// A <see cref="VisualElement"/> is built for every <see cref="State"/> that appears in a state machine. A
/// <see cref="StateView{T}"/> lets you inject custom UI into that generated element. Subclasses are
/// discovered by their type parameter: for a <see cref="State"/> of type <c>T</c>, the concrete
/// <see cref="StateView{T}"/> whose type argument matches is used, walking up the <see cref="State"/>
/// inheritance chain if there is no exact match.
///
/// The <see cref="State"/> instance is available through <see cref="State"/> once the view is constructed.
/// Access the generated visual element through <see cref="View"/> and add custom UI to
/// <see cref="IStateView.Root"/>.
///
/// <b>Important:</b> allocate custom UI in <see cref="OnViewBuilt"/> and re-add it in
/// <see cref="OnCullingChanged"/> (when <c>cullingEnabled</c> is <c>false</c>). Do not allocate UI in
/// <see cref="OnViewAttached"/>: that callback can fire multiple times during a state's lifetime — for
/// example when the user tabs away from and back to the state machine view — and any UI you allocate
/// there accumulates as duplicates. When a state returns from being culled, the contents of
/// <see cref="IStateView.Root"/> are cleared, so a cached reference is no longer parented; re-add it
/// from <see cref="OnCullingChanged"/> to avoid allocating a new element.
/// </remarks>
/// <typeparam name="T">The <see cref="State"/> type this view customizes.</typeparam>
/// <example>
/// <code lang="cs">
/// <![CDATA[
/// // The State type that this view customizes.
/// class MyState : State
/// {
/// }
///
/// // This view is constructed for every MyState instance in a state machine.
/// class MyStateView : StateView<MyState>
/// {
///     Label m_Label;
///
///     public override void OnViewBuilt()
///     {
///         // Allocate custom UI once, after the built-in UI is ready and before the state attaches to a
///         // panel. Cache the reference so OnCullingChanged can re-add it without allocating again.
///         m_Label = new Label($"State: {State.Title}");
///         View.Root.Add(m_Label);
///     }
///
///     public override void OnCullingChanged(bool cullingEnabled)
///     {
///         // When cullingEnabled is false, the state has just returned from being culled and Root has
///         // been cleared. Re-add the cached element — no allocation needed.
///         if (!cullingEnabled && m_Label != null)
///             View.Root.Add(m_Label);
///     }
/// }
/// ]]>
/// </code>
/// </example>
public class StateView<T> : IUserStateView
    where T : State
{
    /// <summary>
    /// The <see cref="State"/> instance this view customizes.
    /// </summary>
    /// <remarks>
    /// This property is set before any callback fires. Use it to read the state's data (such as
    /// <see cref="State.Title"/>) when building custom UI.
    /// </remarks>
    public T State { get; internal set; }

    /// <summary>
    /// The generated view for this state. Add custom UI to <see cref="IStateView.Root"/>.
    /// </summary>
    /// <remarks>
    /// This property is set before any callback fires. Its <see cref="IStateView.Root"/> is the
    /// <see cref="VisualElement"/> that hosts both the built-in state UI and any custom UI you add.
    /// </remarks>
    public IStateView View { get; internal set; }

    void IUserStateView.Initialize(State state, IStateView view)
    {
        State = (T)state;
        View = view;
    }

    /// <summary>
    /// Called once, after the state's built-in UI is fully constructed and before its
    /// <see cref="VisualElement"/> is attached to the state machine view.
    /// </summary>
    /// <remarks>
    /// This is the recommended entry point for allocating custom UI and adding it to
    /// <see cref="IStateView.Root"/>. It fires exactly once per view instance, so any element allocated
    /// here can be safely cached in a field.
    ///
    /// If the state is culled and later revealed, the contents of <see cref="IStateView.Root"/> are
    /// cleared; re-add your cached elements from <see cref="OnCullingChanged"/> (when
    /// <c>cullingEnabled</c> is <c>false</c>). Prefer this pattern over allocating a fresh element each
    /// time.
    /// </remarks>
    /// <example>
    /// <code lang="cs">
    /// <![CDATA[
    /// Label m_Label;
    ///
    /// public override void OnViewBuilt()
    /// {
    ///     m_Label = new Label($"State: {State.Title}");
    ///     View.Root.Add(m_Label);
    /// }
    /// ]]>
    /// </code>
    /// </example>
    public virtual void OnViewBuilt() { }

    /// <summary>
    /// Called when the state's <see cref="IStateView.Root"/> is attached to a UI panel.
    /// </summary>
    /// <remarks>
    /// Fires whenever the visual element receives an <see cref="AttachToPanelEvent"/>. This can happen
    /// multiple times during a state's lifetime — for example when the user tabs away from and back to
    /// the state machine view. Use this callback for logic that needs to respond to panel-attach events,
    /// such as subscribing to panel-level events.
    /// </remarks>
    public virtual void OnViewAttached() { }

    /// <summary>
    /// Called when the state's <see cref="IStateView.Root"/> is detached from its UI panel.
    /// </summary>
    /// <remarks>
    /// Fires whenever the visual element receives a <see cref="DetachFromPanelEvent"/>. Like
    /// <see cref="OnViewAttached"/>, this can fire multiple times in a state's lifetime (tab switches,
    /// and so on), and a matching <see cref="OnViewAttached"/> may follow. Use this callback to release
    /// resources tied to panel presence, such as event subscriptions or scheduled tasks.
    /// </remarks>
    public virtual void OnViewDetached() { }

    /// <summary>
    /// Called when the state machine view's zoom level changes.
    /// </summary>
    /// <param name="zoom">
    /// The new zoom level, equal to the horizontal scale of the state machine view's content. A value of
    /// 1 represents 100% zoom; values below 1 represent zoomed-out views.
    /// </param>
    /// <remarks>
    /// Fires whenever the state machine view's zoom changes, and also once for the initial zoom when the
    /// view is first built. Override this to swap custom UI for a level-of-detail-appropriate
    /// representation — for example, hiding fine details or replacing text with icons when the view is
    /// zoomed out.
    /// </remarks>
    /// <example>
    /// <code lang="cs">
    /// <![CDATA[
    /// public override void OnViewLODChanged(float zoom)
    /// {
    ///     // Hide detailed UI when zoomed out below 50%.
    ///     m_DetailsContainer.style.display = zoom < 0.5f
    ///         ? DisplayStyle.None
    ///         : DisplayStyle.Flex;
    /// }
    /// ]]>
    /// </code>
    /// </example>
    public virtual void OnViewLODChanged(float zoom) { }

    /// <summary>
    /// Called when the state's culling state changes.
    /// </summary>
    /// <param name="cullingEnabled">
    /// <c>true</c> when the state has just been culled; <c>false</c> when the state has just returned
    /// from being culled.
    /// </param>
    /// <remarks>
    /// States are culled when they are off-screen or too small to render at the current zoom level. When
    /// the state returns from being culled (<paramref name="cullingEnabled"/> is <c>false</c>), the
    /// contents of <see cref="IStateView.Root"/> have been cleared and the built-in parts have been
    /// rebuilt. Re-add any custom UI you allocated in <see cref="OnViewBuilt"/>.
    ///
    /// Cache references to your custom elements in fields so this callback can re-parent them via
    /// <c>View.Root.Add(...)</c> without allocating new ones.
    ///
    /// When the state is being culled (<paramref name="cullingEnabled"/> is <c>true</c>), you typically
    /// don't need to do anything; use this branch only if you need to release resources for culled
    /// states.
    /// </remarks>
    /// <example>
    /// <code lang="cs">
    /// <![CDATA[
    /// Label m_Label; // Allocated in OnViewBuilt.
    ///
    /// public override void OnCullingChanged(bool cullingEnabled)
    /// {
    ///     if (!cullingEnabled && m_Label != null)
    ///     {
    ///         // Root was cleared while culled — re-parent the cached label.
    ///         View.Root.Add(m_Label);
    ///     }
    /// }
    /// ]]>
    /// </code>
    /// </example>
    public virtual void OnCullingChanged(bool cullingEnabled) { }
}
