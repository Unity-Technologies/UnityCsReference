// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace Unity.Localization.Components;

/// <summary>
/// Component that resolves a localized texture and forwards it through a UnityEvent whenever the value or the selected locale changes.
/// </summary>
/// <remarks>
/// This is the <see cref="Texture"/> specialization of <see cref="LocalizeAssetEvent{TObject}"/>. Add it to a
/// <see cref="GameObject"/>, assign its <see cref="LocalizeAssetEvent{TObject}.AssetReference"/> in the Inspector, then
/// connect <see cref="LocalizeAssetEvent{TObject}.OnUpdateAsset"/> to an image component to swap textures such as flags or
/// localized artwork per locale.
/// </remarks>
/// <example>
/// <para>Forward the resolved texture to a UI image.</para>
/// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Components/LocalizeTextureEventBindingExample.cs"/>
/// </example>
/// <seealso cref="LocalizeAssetEvent{TObject}"/>
/// <seealso cref="LocalizedAsset{TObject}"/>
/// <seealso cref="LocalizationSettings"/>
[AddComponentMenu("Localization/Localize Texture Event")]
public class LocalizeTextureEvent : LocalizeAssetEvent<Texture> { }
