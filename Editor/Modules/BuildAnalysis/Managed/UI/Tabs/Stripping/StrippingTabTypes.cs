// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Build.Analysis;

    internal enum StrippingItemType
    {
        Module,
        Type,
    }

    internal struct StrippingItem
    {
        // Internal UI Toolkit Id
        public int Id;
        public string Name;
        public Texture Icon;
        public List<string> Reasons;
        public List<string> ModuleDependents;
        public StrippingItemType Type;

        public bool HasDataAboutDependents()
        {
            return Reasons.Count > 0 || ModuleDependents.Count > 0;
        }
    }

    internal class ReasonItem
    {
        public const int KInvalidStrippingItemId = int.MaxValue;
        public int StrippingItemId = KInvalidStrippingItemId;
        public Texture Icon;
        public string Text;
        public bool Enabled = true;
        public string Tooltip;
    }

