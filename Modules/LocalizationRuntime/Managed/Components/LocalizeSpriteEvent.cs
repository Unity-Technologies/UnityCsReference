// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace Unity.Localization.Components;

/// <summary>
/// Component that resolves a localized sprite and forwards it through a UnityEvent whenever the value or the selected locale changes.
/// </summary>
/// <remarks>
/// This is the <see cref="Sprite"/> specialization of <see cref="LocalizeAssetEvent{TObject}"/>. Add it to a
/// <see cref="GameObject"/>, assign its <see cref="LocalizeAssetEvent{TObject}.AssetReference"/> in the Inspector, then
/// connect <see cref="LocalizeAssetEvent{TObject}.OnUpdateAsset"/> to a sprite renderer or UI image to swap artwork
/// such as icons or illustrations per locale.
/// </remarks>
/// <example>
/// Forward the resolved sprite to a sprite renderer.
/// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Components/LocalizeSpriteEventBindingExample.cs"/>
/// </example>
/// <seealso cref="LocalizeAssetEvent{TObject}"/>
/// <seealso cref="LocalizedAsset{TObject}"/>
/// <seealso cref="LocalizationSettings"/>
[AddComponentMenu("Localization/Localize Sprite Event")]
public class LocalizeSpriteEvent : LocalizeAssetEvent<Sprite> { }
