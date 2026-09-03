// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace Unity.Localization.Components;

/// <summary>
/// Serves as the base component that resolves a localized asset and forwards it through a UnityEvent whenever the value or the selected locale changes.
/// </summary>
/// <remarks>
/// Derive a concrete component for a specific asset type by closing the type parameter, for example
/// <see cref="LocalizeTextureEvent"/> for <see cref="Texture"/>. Add the derived component to a <see cref="GameObject"/>,
/// assign its <see cref="AssetReference"/> in the Inspector, then connect <see cref="OnUpdateAsset"/> to a consumer such as
/// an image component. While the component is active it subscribes to the localized asset's
/// <see cref="LocalizedAsset{TObject}.AssetChanged"/> event, so it re-resolves and re-invokes the event when
/// <see cref="LocalizationSettings.SelectedLocale"/> changes. Use it to swap assets such as flags or localized artwork per locale.
/// </remarks>
/// <typeparam name="TObject">The asset type to resolve, which must derive from <see cref="UnityEngine.Object"/>.</typeparam>
/// <example>
/// <para>Forward the resolved asset to a consumer.</para>
/// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Components/LocalizeTextureEventBindingExample.cs"/>
/// </example>
/// <seealso cref="LocalizeTextureEvent"/>
/// <seealso cref="LocalizedAsset{TObject}"/>
/// <seealso cref="LocalizationSettings"/>
public abstract class LocalizeAssetEvent<TObject> : MonoBehaviour where TObject : Object
{
    [SerializeField] LocalizedAsset<TObject> m_Asset = new();
    [SerializeField] UnityEvent<TObject> m_OnUpdateAsset = new();

    /// <summary>
    /// The localized asset reference to resolve.
    /// </summary>
    /// <remarks>Assigning a new reference re-subscribes the component so the event tracks the new reference.</remarks>
    public LocalizedAsset<TObject> AssetReference
    {
        get => m_Asset;
        set
        {
            if (m_Asset != null)
                m_Asset.AssetChanged -= OnAssetChanged;
            m_Asset = value;
            if (m_Asset != null && isActiveAndEnabled)
                m_Asset.AssetChanged += OnAssetChanged;
        }
    }

    /// <summary>
    /// The event invoked with the localized asset each time it resolves.
    /// </summary>
    public UnityEvent<TObject> OnUpdateAsset => m_OnUpdateAsset;

    void OnEnable()
    {
        if (m_Asset != null)
            m_Asset.AssetChanged += OnAssetChanged;
    }

    void OnDisable()
    {
        if (m_Asset != null)
            m_Asset.AssetChanged -= OnAssetChanged;
    }

    void OnAssetChanged(TObject value) => m_OnUpdateAsset.Invoke(value);
}
