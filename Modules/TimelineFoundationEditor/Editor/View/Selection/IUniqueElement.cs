// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Timeline.Foundation.Common;

namespace Unity.Timeline.Foundation.View
{
    /// <summary>
    /// Visual elements that are associated to unique items must inherit from IUniqueElement
    /// </summary>
    interface IUniqueElement
    {
        UniqueID ID { get; }
    }
}
