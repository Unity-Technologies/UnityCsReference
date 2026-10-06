// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace Unity.Localization.Components;

/// <summary>
/// Component that resolves a localized audio clip and forwards it through a UnityEvent whenever the value or the selected locale changes.
/// </summary>
/// <remarks>
/// This is the <see cref="AudioClip"/> specialization of <see cref="LocalizeAssetEvent{TObject}"/>. Add it to a
/// <see cref="GameObject"/>, assign its <see cref="LocalizeAssetEvent{TObject}.AssetReference"/> in the Inspector, then
/// connect <see cref="LocalizeAssetEvent{TObject}.OnUpdateAsset"/> to an <see cref="AudioSource"/> to play voice-over
/// lines or other sounds recorded per language.
/// </remarks>
/// <example>
/// Forward the resolved clip to an audio source.
/// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Components/LocalizeAudioClipEventBindingExample.cs"/>
/// </example>
/// <seealso cref="LocalizeAssetEvent{TObject}"/>
/// <seealso cref="LocalizedAsset{TObject}"/>
/// <seealso cref="LocalizationSettings"/>
[AddComponentMenu("Localization/Localize Audio Clip Event")]
public class LocalizeAudioClipEvent : LocalizeAssetEvent<AudioClip> { }
