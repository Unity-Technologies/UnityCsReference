// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.UI.Builder
{
    internal class BackgroundPositionDimensionStyleField : StyleField<float>
    {
        [Serializable]
        public new class UxmlSerializedData : DimensionStyleField.UxmlSerializedData
        {
            public override object CreateInstance() => new BackgroundPositionDimensionStyleField();
        }

        public BackgroundPositionDimensionStyleField() : base()
        {
        }

        public BackgroundPositionDimensionStyleField(string label) : base(label)
        {
        }
    }
}
