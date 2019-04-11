// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
    internal class PackageToolbar : VisualElement
    {
        internal new class UxmlFactory : UxmlFactory<PackageToolbar> {}

        private readonly VisualElement root;

        public PackageToolbar()
        {
            root = Resources.GetTemplate("PackageToolbar.uxml");
            Add(root);
            Cache = new VisualElementCache(root);
        }

        private VisualElementCache Cache { get; set; }
    }
}
