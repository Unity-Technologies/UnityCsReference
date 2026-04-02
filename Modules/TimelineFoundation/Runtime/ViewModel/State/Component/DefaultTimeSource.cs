// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Timeline.Foundation.Model;

namespace Unity.Timeline.Foundation.ViewModel.Internals
{
    class DefaultTimeSource : ITimeSource
    {
        public TimeSourceData timeSourceData { get; set; }

        public DefaultTimeSource() : this(TimeSourceData.Zero) { }

        public DefaultTimeSource(TimeSourceData timeSourceData)
        {
            this.timeSourceData = timeSourceData;
        }
    }
}
