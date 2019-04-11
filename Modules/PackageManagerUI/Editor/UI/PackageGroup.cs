// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
    internal class PackageGroup : VisualElement
    {
        internal new class UxmlFactory : UxmlFactory<PackageGroup> {}

        private readonly VisualElement root;
        internal readonly PackageGroupOrigins Origin;
        private Selection Selection;

        public PackageGroup previousGroup;
        public PackageGroup nextGroup;

        public PackageItem firstPackage;
        public PackageItem lastPackage;

        internal IEnumerable<PackageItem> PackageItems
        {
            get { return List.Children().Cast<PackageItem>(); }
        }

        public PackageGroup() : this(string.Empty, null)
        {
        }

        public PackageGroup(string groupName, Selection selection)
        {
            name = groupName;
            root = Resources.GetTemplate("PackageGroup.uxml");
            Add(root);
            Cache = new VisualElementCache(root);

            Selection = selection;

            if (string.IsNullOrEmpty(groupName) || groupName != PackageGroupOrigins.BuiltInPackages.ToString())
            {
                HeaderTitle.text = "Packages";
                Origin = PackageGroupOrigins.Packages;
            }
            else
            {
                HeaderTitle.text = "Built In Packages";
                Origin = PackageGroupOrigins.BuiltInPackages;
            }
        }

        public IEnumerable<IPackageSelection> GetSelectionList()
        {
            foreach (var item in PackageItems)
                foreach (var selection in item.GetSelectionList())
                    yield return selection;
        }

        internal PackageItem AddPackage(Package package)
        {
            var packageItem = new PackageItem(package, Selection);

            List.Add(packageItem);

            if (firstPackage == null) firstPackage = packageItem;
            lastPackage = packageItem;

            return packageItem;
        }

        internal void ReorderPackageItems()
        {
            List.Sort((left, right) =>
            {
                var packageLeft = left as PackageItem;
                var packageRight = right as PackageItem;
                if (packageLeft == null || packageRight == null)
                    return 0;

                return string.Compare(packageLeft.NameLabel.text, packageRight.NameLabel.text,
                    StringComparison.InvariantCultureIgnoreCase);
            });
            firstPackage = PackageItems.FirstOrDefault();
            lastPackage = PackageItems.LastOrDefault();
        }

        private VisualElementCache Cache { get; set; }
        private VisualElement List { get { return Cache.Get<VisualElement>("groupContainer"); } }
        private Label HeaderTitle { get { return Cache.Get<Label>("headerTitle"); } }
    }
}
