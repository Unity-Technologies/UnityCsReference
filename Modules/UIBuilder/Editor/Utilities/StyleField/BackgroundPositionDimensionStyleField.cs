// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    [UxmlElement]
    internal partial class BackgroundPositionDimensionStyleField : StyleField<float>
    {
        public BackgroundPositionDimensionStyleField() : base()
        {
        }

        public BackgroundPositionDimensionStyleField(string label) : base(label)
        {
        }
    }
}
