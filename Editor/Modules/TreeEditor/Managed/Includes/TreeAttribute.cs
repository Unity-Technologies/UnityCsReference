// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace TreeEditor
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class TreeAttribute : Attribute
    {
        public TreeAttribute(string uiLabel, string uiGadget, float uiMin, float uiMax)
        {
            this.uiLabel = uiLabel;
            this.uiGadget = uiGadget;
            this.uiMin = uiMin;
            this.uiMax = uiMax;
            this.uiCurve = "";
            this.uiRequirement = "";
        }

        public TreeAttribute(string uiLabel, string uiGadget, float uiMin, float uiMax, string uiRequirement)
        {
            this.uiLabel = uiLabel;
            this.uiGadget = uiGadget;
            this.uiMin = uiMin;
            this.uiMax = uiMax;
            this.uiCurve = "";
            this.uiRequirement = uiRequirement;
        }

        public TreeAttribute(string uiLabel, string uiGadget, float uiMin, float uiMax, string uiCurve, float uiCurveMin, float uiCurveMax)
        {
            this.uiLabel = uiLabel;
            this.uiGadget = uiGadget;
            this.uiMin = uiMin;
            this.uiMax = uiMax;
            this.uiCurve = uiCurve;
            this.uiCurveMin = uiCurveMin;
            this.uiCurveMax = uiCurveMax;
            this.uiRequirement = "";
        }

        public TreeAttribute(string uiLabel, string uiGadget, float uiMin, float uiMax, string uiCurve, float uiCurveMin, float uiCurveMax, string uiRequirement)
        {
            this.uiLabel = uiLabel;
            this.uiGadget = uiGadget;
            this.uiMin = uiMin;
            this.uiMax = uiMax;
            this.uiCurve = uiCurve;
            this.uiCurveMin = uiCurveMin;
            this.uiCurveMax = uiCurveMax;
            this.uiRequirement = uiRequirement;
        }

        public TreeAttribute(string uiLabel, string uiGadget, string uiOptions)
        {
            char[] optionSplitter = { ',' };
            this.uiLabel = uiLabel;
            this.uiGadget = uiGadget;
            this.uiRequirement = uiOptions;
            string[] opts = uiOptions.Split(optionSplitter);
            this.uiOptions = new GUIContent[opts.Length];
            for (int i = 0; i < opts.Length; i++)
            {
                this.uiOptions[i] = new GUIContent(opts[i]);
            }
        }

        public TreeAttribute(string uiLabel, string uiGadget, string uiOptions, string uiCurve, float uiCurveMin, float uiCurveMax, string uiRequirement)
        {
            char[] optionSplitter = { ',' };
            this.uiLabel = uiLabel;
            this.uiGadget = uiGadget;
            this.uiRequirement = uiRequirement;
            this.uiCurve = uiCurve;
            this.uiCurveMin = uiCurveMin;
            this.uiCurveMax = uiCurveMax;
            string[] opts = uiOptions.Split(optionSplitter);
            this.uiOptions = new GUIContent[opts.Length];
            for (int i = 0; i < opts.Length; i++)
            {
                this.uiOptions[i] = new GUIContent(opts[i]);
            }
        }

        public override string ToString()
        {
            string value = "uiLabel: " + uiLabel + ", uiGadget: " + uiGadget + ", uiMin: " + uiMin + ", uiMax: " + uiMax;
            if (uiCurve != "")
            {
                value += ", uiCurve: " + uiCurve;
            }
            return value;
        }

        public string uiLabel;
        public string uiGadget;
        public string uiCurve;
        public string uiRequirement;
        public GUIContent[] uiOptions;
        public float uiCurveMin;
        public float uiCurveMax;
        public float uiMin;
        public float uiMax;
    }
}
