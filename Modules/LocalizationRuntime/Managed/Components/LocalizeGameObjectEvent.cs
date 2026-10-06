// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace Unity.Localization.Components;

/// <summary>
/// Component that resolves a localized prefab and forwards it through a UnityEvent whenever the value or the selected locale changes.
/// </summary>
/// <remarks>
/// This is the <see cref="GameObject"/> specialization of <see cref="LocalizeAssetEvent{TObject}"/>. Add it to a
/// <see cref="GameObject"/>, assign its <see cref="LocalizeAssetEvent{TObject}.AssetReference"/> in the Inspector, then
/// connect <see cref="LocalizeAssetEvent{TObject}.OnUpdateAsset"/> to code that swaps an instantiated prefab, for
/// content that differs per locale in more than text, such as signage models or layout variants.
/// </remarks>
/// <example>
/// Replace the spawned instance when the resolved prefab changes.
/// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Components/LocalizeGameObjectEventBindingExample.cs"/>
/// </example>
/// <seealso cref="LocalizeAssetEvent{TObject}"/>
/// <seealso cref="LocalizedAsset{TObject}"/>
/// <seealso cref="LocalizationSettings"/>
[AddComponentMenu("Localization/Localize Game Object Event")]
public class LocalizeGameObjectEvent : LocalizeAssetEvent<GameObject> { }
