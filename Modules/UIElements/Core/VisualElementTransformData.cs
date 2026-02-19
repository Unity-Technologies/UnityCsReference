// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;

namespace UnityEngine.UIElements;

[Flags]
internal enum VisualElementTransformFlags
{
    // Need to compute world transform
    WorldTransformDirty = 1 << 0,
    // Need to compute world transform inverse
    WorldTransformInverseDirty = 1 << 1,
    // Need to compute bounding box
    BoundingBoxDirty = 1 << 2,
    // Need to compute world bounding box
    WorldBoundingBoxDirty = 1 << 3,
    // Need to compute bounding box without nested panel components
    BoundingBoxWithoutNestedDirty = 1 << 4,
    // Element uses 3-D transforms or contains children that do
    Needs3DBounds = 1 << 5,
    // Element's 3-D transform local bounds need to be recalculated (with or without nested UIDocuments)
    LocalBounds3DDirty = 1 << 6,
    LocalBoundsWithoutNested3DDirty = 1 << 7,
    // Element or descendent received a GeometryChangedEvent since last Layout update
    BoundingBoxDirtiedSinceLastLayoutPass = 1 << 8,
    // Element never clip regardless of overflow style (useful for ScrollView)
    DisableClipping = 1 << 9,
    // Element is shown in the hierarchy (element or one of its ancestors is not DisplayStyle.None)
    // Note that this flag is up-to-date only after UIRLayoutUpdater is done with its updates
    HierarchyDisplayed = 1 << 10,
    // Element layout is manually set
    LayoutManual = 1 << 11,
    // 1-bit encoding of PickingMode
    PickingIgnore = 1 << 12,
    // Element has a ContainsPoint override
    UsesContainsPoint = 1 << 13,

    Init = WorldTransformDirty | WorldTransformInverseDirty | BoundingBoxDirty | WorldBoundingBoxDirty | BoundingBoxWithoutNestedDirty | LocalBounds3DDirty | LocalBoundsWithoutNested3DDirty
}

[StructLayout(LayoutKind.Sequential)]
internal struct VisualElementTransformData
{
    public static readonly VisualElementTransformData Default = new()
    {
        WorldTransform = Matrix4x4.identity,
        WorldTransformInverse = Matrix4x4.identity,
        Flags = VisualElementTransformFlags.Init,
    };

    public Matrix4x4 WorldTransform;
    public Matrix4x4 WorldTransformInverse;
    public Rect BoundingBox;
    public Rect WorldBoundingBox;

    public Rect ManualLayout;

    internal VisualElementTransformFlags Flags;
}
