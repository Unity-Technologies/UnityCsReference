// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.UIToolkit.Editor
{
    internal enum StylePropertyRecordingChannel
    {
        Unsupported = 0,
        Float1,
        Rotate,
        Length1,
        Color4,
        Int1,
        Translate5,
        Scale3,
        Ratio1,
        EnumInt,
        BackgroundPosition3,
        BackgroundRepeat2,
        BackgroundSize5,
        // 4-channel transform-origin: x.value, x.unit, y.value, y.unit. Z is intentionally
        // not animated and the sampler preserves the current z.
        TransformOrigin4,
        // PropertyKind.Filter. Each Filter value carries 4 slots * 18 sub-channels (16
        // float `.<i>.p<n>` + 1 discrete int `.<i>.type` + 1 PPtr `.<i>.customDefinition`).
        // The dispatcher fans a single List<FilterFunction> change into the per-sub-channel
        // UndoPropertyModifications that the native UIAnimationBinder binding namespace
        // already exposes; see AnimationBindingHelper.SubChannelKind.FilterSlot in
        // Tools/UIElementsGenerator/UIElementsGenerator/Definitions/StylePropertyDefinitions.cs.
        Filter,
        // Single-channel object-reference (PPtr) shape: background-image, -unity-font, ...
        Object1,
        // 7-channel TextShadow: .color.r/.g/.b/.a, .offset.x/.y, .blurRadius (all Float).
        TextShadow7,
    }
}
