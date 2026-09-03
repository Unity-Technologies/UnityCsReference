// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.Localization;

/// <summary>
/// A serializable reference to a localized asset that updates as the selected locale changes.
/// </summary>
/// <remarks>
/// A <see cref="LocalizedAsset{TObject}"/> extends <see cref="LocalizedEntry{TEntry}"/> to resolve an
/// <see cref="IAssetEntry"/> and load its asset as <typeparamref name="TObject"/> through the <see cref="ResourceDatabase"/>
/// provider chain. Load the asset with <see cref="GetLocalizedAssetAsync"/>, or subscribe to <see cref="AssetChanged"/> to
/// receive it immediately and again whenever the selected locale changes. For common asset types, use the
/// <see cref="LocalizedTexture"/>, <see cref="LocalizedGameObject"/>, and <see cref="LocalizedObject"/> subclasses, which are
/// serializable and appear in the Inspector.
/// </remarks>
/// <typeparam name="TObject">The type of asset to resolve, which must derive from <see cref="UnityEngine.Object"/>.</typeparam>
/// <example>
/// <para>Resolve a localized texture and apply it whenever the locale changes.</para>
/// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Reference/LocalizedAssetTextureExample.cs"/>
/// </example>
/// <seealso cref="ResourceDatabase"/>
/// <seealso cref="Unity.Localization.TableReference"/>
/// <seealso cref="LocalizedString"/>
[Serializable]
public partial class LocalizedAsset<TObject> : LocalizedEntry<IAssetEntry> where TObject : Object
{
    /// <summary>
    /// Represents a method that receives a resolved localized asset value.
    /// </summary>
    /// <remarks>Used by the <see cref="AssetChanged"/> event to deliver the loaded asset to subscribers.</remarks>
    /// <param name="value">The resolved asset, or null when no asset is available.</param>
    public delegate void ChangeHandler(TObject value);

    ChangeHandler m_Changed;
    Action<Locale> m_LocaleChanged;

    /// <summary>
    /// Occurs when the resolved asset changes, and once immediately when a handler subscribes.
    /// </summary>
    /// <remarks>
    /// Adding a handler invokes it right away with the current asset, then again whenever the selected locale changes or
    /// <see cref="RefreshAsset"/> runs. Because loading is asynchronous, the first call arrives after the asset has loaded.
    /// Removing the last handler stops the reference from listening for locale changes.
    /// </remarks>
    /// <example>
    /// <para>Swap a texture whenever the localized asset changes.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Reference/LocalizedAssetChangedExample.cs"/>
    /// </example>
    /// <seealso cref="RefreshAsset"/>
    /// <seealso cref="GetLocalizedAssetAsync"/>
    public event ChangeHandler AssetChanged
    {
        add
        {
            RegisterLocaleChanged();
            m_Changed += value;
            // Deliver to the new handler only: a refresh would re-notify every existing subscriber.
            PushTo(value);
        }
        remove
        {
            m_Changed -= value;
            if (m_Changed == null)
                UnregisterLocaleChanged();
        }
    }

    void PushTo(ChangeHandler handler)
    {
        if (LocalizationSettings.PreferredLoading == LoadingPreference.Synchronous)
        {
            var asset = GetLocalizedAsset();
            if (asset != null)
            {
                handler?.Invoke(asset);
                return;
            }
        }
        PushToAsync(handler);
    }

    async void PushToAsync(ChangeHandler handler)
    {
        var version = CurrentRefresh;
        try
        {
            var asset = await GetLocalizedAssetAsync();
            // A refresh started since; it pushes the newer value to this handler too.
            if (!IsCurrentRefresh(version))
                return;
            // The handler may have unsubscribed while the load was in flight, so deliver only if it is still live.
            if (IsSubscribed(handler))
                handler(asset);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    bool IsSubscribed(ChangeHandler handler)
    {
        var current = m_Changed;
        if (current == null || handler == null)
            return false;
        if (current == handler)
            return true;
        // Compare target by target, so a handler subscribed as an already-combined delegate still matches.
        var live = current.GetInvocationList();
        var wanted = handler.GetInvocationList();
        for (var i = 0; i < wanted.Length; i++)
        {
            if (Array.IndexOf(live, wanted[i]) < 0)
                return false;
        }
        return true;
    }

    /// <summary>
    /// Loads the localized asset asynchronously through the resource database.
    /// </summary>
    /// <remarks>
    /// Resolves the asset as <typeparamref name="TObject"/> through the <see cref="ResourceDatabase"/> provider chain,
    /// loading the table if needed. The result is null when the reference is empty, no resource database is configured, or
    /// the asset cannot be found. When <see cref="LocalizationSettings.PreferredLoading"/> is
    /// <see cref="LoadingPreference.Synchronous"/> and the asset is available synchronously, the asset resolves without an
    /// asynchronous load.
    /// </remarks>
    /// <returns>An awaitable that produces the loaded asset, or null when it cannot be resolved.</returns>
    /// <example>
    /// <para>Load a localized asset.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Reference/LocalizedAssetLoadAsyncExample.cs"/>
    /// </example>
    public Awaitable<TObject> GetLocalizedAssetAsync()
    {
        var database = LocalizationSettings.ResourceDatabase;
        if (database == null || IsEmpty)
            return AwaitableUtility.FromResult<TObject>(null);
        if (LocalizationSettings.PreferredLoading == LoadingPreference.Synchronous)
        {
            var asset = GetLocalizedAsset();
            if (asset != null)
                return AwaitableUtility.FromResult(asset);
        }
        return LoadAsync(database);
    }

    async Awaitable<TObject> LoadAsync(ResourceDatabase database)
    {
        // Initialization selects the startup locale, so settle it before capturing the resolving locale.
        if (!LocalizationSettings.InitializationSettled)
            await LocalizationSettings.EnsureInitializedForUse();
        var locale = ResolvingLocale();
        var entry = await GetEntryAsync(locale, default);
        return await database.LoadLocalizedAssetAsync<TObject>(entry, locale);
    }

    /// <summary>
    /// Resolves the localized asset synchronously when a synchronous provider and entry can supply it.
    /// </summary>
    /// <remarks>
    /// Resolves the asset as <typeparamref name="TObject"/> through providers and entries that support synchronous
    /// loading, such as direct references or a <c>Resources</c> load. The result is null when the reference is empty,
    /// no resource database is configured, or the asset can only be loaded asynchronously and is not already cached.
    /// Use <see cref="GetLocalizedAssetAsync"/> to load asynchronously instead.
    /// </remarks>
    /// <returns>The resolved asset, or null when it cannot be resolved synchronously.</returns>
    /// <example>
    /// <para>Read a localized asset synchronously.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Reference/LocalizedAssetSyncExample.cs"/>
    /// </example>
    public TObject GetLocalizedAsset()
    {
        var database = LocalizationSettings.ResourceDatabase;
        if (database == null || IsEmpty)
            return null;
        var entry = GetEntry();
        return database.LoadLocalizedAsset<TObject>(entry, ResolvingLocale());
    }

    /// <summary>
    /// Re-resolves the asset and pushes the result to subscribers.
    /// </summary>
    /// <remarks>
    /// Loads the asset again and invokes <see cref="AssetChanged"/> with the new value. Does nothing when no handler is
    /// subscribed. It runs automatically when the reference changes or the selected locale changes, so call it manually only
    /// when an external input requires a reload.
    /// </remarks>
    /// <example>
    /// <para>Force a reload of the localized asset.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Reference/LocalizedAssetRefreshExample.cs"/>
    /// </example>
    public void RefreshAsset()
    {
        if (m_Changed == null)
            return;
        var version = BeginRefresh();
        if (LocalizationSettings.PreferredLoading == LoadingPreference.Synchronous)
        {
            var asset = GetLocalizedAsset();
            if (asset != null)
            {
                m_Changed?.Invoke(asset);
                return;
            }
        }
        RefreshAssetAsync(version);
    }

    async void RefreshAssetAsync(int version)
    {
        try
        {
            var asset = await GetLocalizedAssetAsync();
            if (!IsCurrentRefresh(version))
                return;
            m_Changed?.Invoke(asset);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception e)
        {
            // async void: an escaping exception would be unhandled and can bring down the player.
            Debug.LogException(e);
        }
    }

    /// <inheritdoc/>
    protected override void ForceUpdate()
    {
        base.ForceUpdate();
        RefreshAsset();
    }

    void RegisterLocaleChanged()
    {
        m_LocaleChanged ??= _ => RefreshAsset();
        // Remove before adding so this subscribes exactly once whatever state the static event is in.
        LocalizationSettings.SelectedLocaleChanged -= m_LocaleChanged;
        LocalizationSettings.SelectedLocaleChanged += m_LocaleChanged;
    }

    void UnregisterLocaleChanged()
    {
        if (m_LocaleChanged == null)
            return;
        LocalizationSettings.SelectedLocaleChanged -= m_LocaleChanged;
        m_LocaleChanged = null;
    }
}

/// <summary>
/// A serializable reference to a localized texture asset.
/// </summary>
/// <remarks>
/// A <see cref="LocalizedTexture"/> is a <see cref="LocalizedAsset{TObject}"/> specialized for <see cref="Texture"/>, so it
/// can be serialized and shown in the Inspector.
/// </remarks>
/// <example>
/// <para>Load a localized texture.</para>
/// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Reference/LocalizedTextureExample.cs"/>
/// </example>
/// <seealso cref="LocalizedAsset{TObject}"/>
/// <seealso cref="ResourceDatabase"/>
[Serializable]
public partial class LocalizedTexture : LocalizedAsset<Texture> { }

/// <summary>
/// A serializable reference to a localized GameObject asset.
/// </summary>
/// <remarks>
/// A <see cref="LocalizedGameObject"/> is a <see cref="LocalizedAsset{TObject}"/> specialized for <see cref="GameObject"/>,
/// so it can be serialized and shown in the Inspector.
/// </remarks>
/// <example>
/// <para>Load a localized prefab.</para>
/// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Reference/LocalizedGameObjectExample.cs"/>
/// </example>
/// <seealso cref="LocalizedAsset{TObject}"/>
/// <seealso cref="ResourceDatabase"/>
[Serializable]
public partial class LocalizedGameObject : LocalizedAsset<GameObject> { }

/// <summary>
/// A serializable reference to a localized asset of any Object-derived type.
/// </summary>
/// <remarks>
/// A <see cref="LocalizedObject"/> is a <see cref="LocalizedAsset{TObject}"/> specialized for
/// <see cref="UnityEngine.Object"/>. Use it when the asset type is not known ahead of time; otherwise prefer a more specific
/// type such as <see cref="LocalizedTexture"/>.
/// </remarks>
/// <example>
/// <para>Load a localized asset of an unknown type.</para>
/// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Reference/LocalizedObjectExample.cs"/>
/// </example>
/// <seealso cref="LocalizedAsset{TObject}"/>
/// <seealso cref="ResourceDatabase"/>
[Serializable]
public partial class LocalizedObject : LocalizedAsset<Object> { }

/// <summary>
/// A serializable reference to a localized sprite asset.
/// </summary>
/// <remarks>
/// A <see cref="LocalizedSprite"/> is a <see cref="LocalizedAsset{TObject}"/> specialized for <see cref="Sprite"/>, so it can
/// be serialized and shown in the Inspector. Prefer it over <see cref="LocalizedTexture"/> for UI images, which take a sprite.
/// </remarks>
/// <example>
/// <para>Load a localized sprite.</para>
/// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Reference/LocalizedSpriteExample.cs"/>
/// </example>
/// <seealso cref="LocalizedAsset{TObject}"/>
/// <seealso cref="ResourceDatabase"/>
[Serializable]
public partial class LocalizedSprite : LocalizedAsset<Sprite> { }

/// <summary>
/// A serializable reference to a localized material asset.
/// </summary>
/// <remarks>
/// A <see cref="LocalizedMaterial"/> is a <see cref="LocalizedAsset{TObject}"/> specialized for <see cref="Material"/>, so it
/// can be serialized and shown in the Inspector.
/// </remarks>
/// <example>
/// <para>Load a localized material.</para>
/// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Reference/LocalizedMaterialExample.cs"/>
/// </example>
/// <seealso cref="LocalizedAsset{TObject}"/>
/// <seealso cref="ResourceDatabase"/>
[Serializable]
public partial class LocalizedMaterial : LocalizedAsset<Material> { }

/// <summary>
/// A serializable reference to a localized font asset.
/// </summary>
/// <remarks>
/// A <see cref="LocalizedFont"/> is a <see cref="LocalizedAsset{TObject}"/> specialized for <see cref="Font"/>, so it can be
/// serialized and shown in the Inspector. Use it to switch typeface with the locale, which a language with a different
/// script needs.
/// </remarks>
/// <example>
/// <para>Load a localized font.</para>
/// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Reference/LocalizedFontExample.cs"/>
/// </example>
/// <seealso cref="LocalizedAsset{TObject}"/>
/// <seealso cref="ResourceDatabase"/>
[Serializable]
public partial class LocalizedFont : LocalizedAsset<Font> { }
