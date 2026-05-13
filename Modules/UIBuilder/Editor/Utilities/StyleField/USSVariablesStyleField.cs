// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    // We create a custom class for the variables dimension field so that the units don't have to be bound.
    [UxmlElement]
    internal partial class USSVariablesStyleField : DimensionStyleField
    {
        public USSVariablesStyleField() : this(string.Empty) { }

        public USSVariablesStyleField(string label) : base(label)
        {
            option = StyleFieldConstants.UnitPixel;
            units.Clear();
            styleKeywords.Clear();

            units.Add(StyleFieldConstants.UnitPixel);
            units.Add(StyleFieldConstants.UnitPercent);
            units.Add(StyleFieldConstants.UnitDegree);
            units.Add(StyleFieldConstants.UnitGrad);
            units.Add(StyleFieldConstants.UnitRad);
            units.Add(StyleFieldConstants.UnitTurn);
            populatesOptionsMenuFromParentRow = false;

            optionsPopup.choices = units;
        }

        protected override string ComposeValue()
        {
            if (styleKeywords.Contains(option))
                return option;

            // We override value composition to exclude the unit.
            return innerValue.ToString(CultureInfo.InvariantCulture.NumberFormat);
        }

    }
}
