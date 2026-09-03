// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.Localization.Editor;

/// <summary>
/// Associates an asset provider editor with the provider type it serves.
/// </summary>
/// <remarks>
/// Apply this to an <see cref="AssetProviderEditor"/> subclass so the settings system can pair it with
/// the runtime provider type it configures.
/// </remarks>
[AttributeUsage(AttributeTargets.Class)]
public sealed class AssetProviderEditorAttribute : Attribute
{
    /// <summary>
    /// Gets the provider type this editor serves.
    /// </summary>
    public Type ProviderType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AssetProviderEditorAttribute"/> class.
    /// </summary>
    /// <param name="providerType">The asset provider type this editor serves.</param>
    public AssetProviderEditorAttribute(Type providerType) => ProviderType = providerType;
}
