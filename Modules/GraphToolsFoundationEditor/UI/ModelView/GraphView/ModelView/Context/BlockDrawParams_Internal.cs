// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor;

struct BlockDrawParams_Internal
{
    public float etchHeight;
    public float topEtchWidth;
    public float topEtchMargin;
    public float bottomEtchWidth;
    public float bottomEtchMargin;
    public float etchInnerRadius;
    public float etchOuterRadius;
    public float extremeBlockRadius;

    public static BlockDrawParams_Internal Default = new BlockDrawParams_Internal
    {
        etchHeight = 6,
        topEtchWidth = 29,
        topEtchMargin = 18,
        bottomEtchWidth = 29,
        bottomEtchMargin = 18,
        etchInnerRadius = 2,
        etchOuterRadius = 4,
        extremeBlockRadius = 3
    };

    static readonly CustomStyleProperty<float> k_BlockEtchHeightStyle = new CustomStyleProperty<float>("--block--etch-height");
    static readonly CustomStyleProperty<float> k_BlockEtchWidthStyle = new CustomStyleProperty<float>("--block--etch-width");
    static readonly CustomStyleProperty<float> k_BlockEtchMarginStyle = new CustomStyleProperty<float>("--block--etch-margin");
    static readonly CustomStyleProperty<float> k_BlockEtchInnerRadiusStyle = new CustomStyleProperty<float>("--block--etch-inner-radius");
    static readonly CustomStyleProperty<float> k_BlockEtchOuterRadiusStyle = new CustomStyleProperty<float>("--block--etch-outer-radius");
    static readonly CustomStyleProperty<float> k_BlockExtremeRadiusStyle = new CustomStyleProperty<float>("--block--extreme-radius");

    public bool CustomStyleResolved(CustomStyleResolvedEvent e)
    {
        bool changed = false;
        {
            if (e.customStyle.TryGetValue(k_BlockEtchHeightStyle, out float value))
            {
                etchHeight = value;
                changed = true;
            }
        }
        {
            if (e.customStyle.TryGetValue(k_BlockEtchWidthStyle, out float value))
            {
                topEtchWidth = value;
                bottomEtchWidth = value;
                changed = true;
            }
        }
        {
            if (e.customStyle.TryGetValue(k_BlockEtchMarginStyle, out float value))
            {
                topEtchMargin = value;
                bottomEtchMargin = value;
                changed = true;
            }
        }
        {
            if (e.customStyle.TryGetValue(k_BlockEtchInnerRadiusStyle, out float value))
            {
                etchInnerRadius = value;
                changed = true;
            }
        }
        {
            if (e.customStyle.TryGetValue(k_BlockEtchOuterRadiusStyle, out float value))
            {
                etchOuterRadius = value;
                changed = true;
            }
        }
        {
            if (e.customStyle.TryGetValue(k_BlockExtremeRadiusStyle, out float value))
            {
                extremeBlockRadius = value;
                changed = true;
            }
        }
        return changed;
    }
}
