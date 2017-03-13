// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine
{
    // Base class to derive custom property attributes from. Use this to create custom attributes for script variables.
    [System.AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public abstract class PropertyAttribute : Attribute
    {
        public int order { get; set; }
    }

    [System.AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
    public class ContextMenuItemAttribute : PropertyAttribute
    {
        public readonly string name;
        public readonly string function;

        public ContextMenuItemAttribute(string name, string function)
        {
            this.name = name;
            this.function = function;
        }
    }

    [System.AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class TooltipAttribute : PropertyAttribute
    {
        public readonly string tooltip;

        public TooltipAttribute(string tooltip)
        {
            this.tooltip = tooltip;
        }
    }

    [System.AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
    public class SpaceAttribute : PropertyAttribute
    {
        public readonly float height;

        public SpaceAttribute()
        {
            // By default uses 8 pixels which corresponds to EditorGUILayout.Space()
            // which reserves 6 pixels, plus the usual 2 pixels caused by the neighboring margin.
            // (Why not 2 pixels for margin both below and above?
            // Because one of those is already accounted for when the space is not there.)
            this.height = 8;
        }

        public SpaceAttribute(float height)
        {
            this.height = height;
        }
    }

    [System.AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
    public class HeaderAttribute : PropertyAttribute
    {
        public readonly string header;

        public HeaderAttribute(string header)
        {
            this.header = header;
        }
    }

    // Attribute used to make a float or int variable in a script be restricted to a specific range.
    [System.AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public sealed class RangeAttribute : PropertyAttribute
    {
        public readonly float min;
        public readonly float max;

        // Attribute used to make a float or int variable in a script be restricted to a specific range.
        public RangeAttribute(float min, float max)
        {
            this.min = min;
            this.max = max;
        }
    }


    // Attribute to make a string be edited with a multi-line textfield
    [System.AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public sealed class MultilineAttribute : PropertyAttribute
    {
        public readonly int lines;

        public MultilineAttribute()
        {
            this.lines = 3;
        }

        // Attribute used to make a string value be shown in a multiline textarea.
        public MultilineAttribute(int lines)
        {
            this.lines = lines;
        }
    }

    // Attribute to make a string be edited with a multi-line textfield
    [System.AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public sealed class TextAreaAttribute : PropertyAttribute
    {
        public readonly int minLines;
        public readonly int maxLines;

        public TextAreaAttribute()
        {
            this.minLines = 3;
            this.maxLines = 3;
        }

        // Attribute used to make a string value be shown in a multiline textarea.
        public TextAreaAttribute(int minLines, int maxLines)
        {
            this.minLines = minLines;
            this.maxLines = maxLines;
        }
    }

    // Attribute to control how the color can be manipulated in the ColorPicker
    [System.AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public sealed class ColorUsageAttribute : PropertyAttribute
    {
        public readonly bool showAlpha = true;
        public readonly bool hdr = false;
        public readonly float minBrightness = 0f;
        public readonly float maxBrightness = 8f;
        public readonly float minExposureValue = 1 / 8f;
        public readonly float maxExposureValue = 3f;

        public ColorUsageAttribute(bool showAlpha)
        {
            this.showAlpha = showAlpha;
        }

        public ColorUsageAttribute(bool showAlpha, bool hdr, float minBrightness, float maxBrightness, float minExposureValue, float maxExposureValue)
        {
            this.showAlpha = showAlpha;
            this.hdr = hdr;
            this.minBrightness = minBrightness;
            this.maxBrightness = maxBrightness;
            this.minExposureValue = minExposureValue;
            this.maxExposureValue = maxExposureValue;
        }
    }

    // Attribute to make int or float fields delayed.
    [System.AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public sealed class DelayedAttribute : PropertyAttribute
    {
    }
}
