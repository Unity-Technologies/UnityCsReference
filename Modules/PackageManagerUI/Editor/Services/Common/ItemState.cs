// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.PackageManager.UI
{
    [Serializable]
    internal class ItemState
    {
        public string PackageId;
        public bool Expanded;
        public bool SeeAllVersions;
        public bool Selected;

        public ItemState(string packageId, bool selected = false)
        {
            PackageId = packageId;
            Selected = selected;
        }
    }
}
