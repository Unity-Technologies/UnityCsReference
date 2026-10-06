// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
abstract class SaveCommand<T> : Command<T>
    where T : SaveCommand<T>, new()
{
    protected static T GetPooled(object source, VisualTreeAssetEditingContext context, bool succeeded = true)
    {
        var command = GetPooled();
        command.Source = source;
        command.Context = context;
        command.Asset = context.EditedVisualTreeAsset;
        command.Succeeded = succeeded;
        return command;
    }

    protected static T GetPooled(object source, UnityEngine.Object asset, bool succeeded = true)
    {
        var command = GetPooled();
        command.Source = source;
        command.Asset = asset;
        command.Succeeded = succeeded;
        // Populate the VTA-centric context when the affected asset is a VisualTreeAsset, so existing
        // context-based handlers keep working. A standalone StyleSheet has no context and leaves it default.
        command.Context = asset is VisualTreeAsset vta ? new VisualTreeAssetEditingContext(vta) : default;
        return command;
    }

    protected static T GetPooled(object source, UnityEngine.Object asset, bool succeeded, bool singleAsset)
    {
        var command = GetPooled(source, asset, succeeded);
        command.SingleAsset = singleAsset;
        return command;
    }

    protected override void Init()
    {
        base.Init();
        Context = default;
        Asset = null;
        Succeeded = true;
        SingleAsset = false;
    }

    public override CommandCategory Category => CommandCategory.Save;

    public VisualTreeAssetEditingContext Context { get; private set; }

    /// <summary>
    /// The specific asset this command concerns.
    /// </summary>
    public UnityEngine.Object Asset { get; private set; }

    /// <summary>
    /// Whether the operation this command closes actually completed.
    /// </summary>
    public bool Succeeded { get; private set; }

    /// <summary>
    /// Whether the operation concerns <see cref="Asset"/> on its own, leaving every other asset of its
    /// document — a document's style sheets, a document that instantiates it — untouched and still unsaved.
    /// </summary>
    public bool SingleAsset { get; private set; }
}

[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
class PreSaveCommand : SaveCommand<PreSaveCommand>
{
    public static PreSaveCommand GetPooled(object source, VisualTreeAssetEditingContext context)
        => SaveCommand<PreSaveCommand>.GetPooled(source, context);

    public static PreSaveCommand GetPooled(object source, UnityEngine.Object asset)
        => SaveCommand<PreSaveCommand>.GetPooled(source, asset);

    public static void Execute(object source, VisualTreeAssetEditingContext context)
    {
        using var command = GetPooled(source, context);
        UICommandQueue.Execute(command);
    }

    public static void Execute(object source, UnityEngine.Object asset)
    {
        using var command = GetPooled(source, asset);
        UICommandQueue.Execute(command);
    }

    /// <inheritdoc cref="SaveCommand{T}.SingleAsset"/>
    public static void ExecuteForSingleAsset(object source, UnityEngine.Object asset)
    {
        using var command = SaveCommand<PreSaveCommand>.GetPooled(source, asset, succeeded: true, singleAsset: true);
        UICommandQueue.Execute(command);
    }
}

[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
class PostSaveCommand : SaveCommand<PostSaveCommand>
{
    public new static PostSaveCommand GetPooled(object source, VisualTreeAssetEditingContext context, bool succeeded = true)
        => SaveCommand<PostSaveCommand>.GetPooled(source, context, succeeded);

    public new static PostSaveCommand GetPooled(object source, UnityEngine.Object asset, bool succeeded = true)
        => SaveCommand<PostSaveCommand>.GetPooled(source, asset, succeeded);

    public static void Execute(object source, VisualTreeAssetEditingContext context, bool succeeded = true)
    {
        using var command = GetPooled(source, context, succeeded);
        UICommandQueue.Execute(command);
    }

    public static void Execute(object source, UnityEngine.Object asset, bool succeeded = true)
    {
        using var command = GetPooled(source, asset, succeeded);
        UICommandQueue.Execute(command);
    }

    /// <inheritdoc cref="SaveCommand{T}.SingleAsset"/>
    public static void ExecuteForSingleAsset(object source, UnityEngine.Object asset, bool succeeded)
    {
        using var command = SaveCommand<PostSaveCommand>.GetPooled(source, asset, succeeded, singleAsset: true);
        UICommandQueue.Execute(command);
    }
}

[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
class PreDiscardCommand : SaveCommand<PreDiscardCommand>
{
    // No Succeeded overload: a Pre command opens the operation, so there is no outcome to report yet.
    public static PreDiscardCommand GetPooled(object source, VisualTreeAssetEditingContext context)
        => SaveCommand<PreDiscardCommand>.GetPooled(source, context);

    public static PreDiscardCommand GetPooled(object source, UnityEngine.Object asset)
        => SaveCommand<PreDiscardCommand>.GetPooled(source, asset);

    public static void Execute(object source, VisualTreeAssetEditingContext context)
    {
        using var command = GetPooled(source, context);
        UICommandQueue.Execute(command);
    }

    public static void Execute(object source, UnityEngine.Object asset)
    {
        using var command = GetPooled(source, asset);
        UICommandQueue.Execute(command);
    }

    /// <inheritdoc cref="SaveCommand{T}.SingleAsset"/>
    public static void ExecuteForSingleAsset(object source, UnityEngine.Object asset)
    {
        using var command = SaveCommand<PreDiscardCommand>.GetPooled(source, asset, succeeded: true, singleAsset: true);
        UICommandQueue.Execute(command);
    }
}

[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
class PostDiscardCommand : SaveCommand<PostDiscardCommand>
{
    public new static PostDiscardCommand GetPooled(object source, VisualTreeAssetEditingContext context, bool succeeded = true)
        => SaveCommand<PostDiscardCommand>.GetPooled(source, context, succeeded);

    public new static PostDiscardCommand GetPooled(object source, UnityEngine.Object asset, bool succeeded = true)
        => SaveCommand<PostDiscardCommand>.GetPooled(source, asset, succeeded);

    /// <inheritdoc cref="SaveCommand{T}.SingleAsset"/>
    public static void ExecuteForSingleAsset(object source, UnityEngine.Object asset)
    {
        using var command = SaveCommand<PostDiscardCommand>.GetPooled(source, asset, succeeded: true, singleAsset: true);
        UICommandQueue.Execute(command);
    }

    public static void Execute(object source, VisualTreeAssetEditingContext context, bool succeeded = true)
    {
        using var command = GetPooled(source, context, succeeded);
        UICommandQueue.Execute(command);
    }

    public static void Execute(object source, UnityEngine.Object asset, bool succeeded = true)
    {
        using var command = GetPooled(source, asset, succeeded);
        UICommandQueue.Execute(command);
    }
}
