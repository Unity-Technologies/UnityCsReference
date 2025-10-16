// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace Unity.GraphToolkit.Editor
{
    class GroupItemOrderComparer : IComparer<IGroupItemModel>
    {
        public static GroupItemOrderComparer Default = new GroupItemOrderComparer();
        public int Compare(IGroupItemModel a, IGroupItemModel b)
        {
            if (a == null && b == null)
                return 0;
            if (a == null)
                return 1;
            if (b == null)
                return -1;
            var aDepth = 0;
            var current = a;
            while (current.ParentGroup != null)
            {
                ++aDepth;
                current = current.ParentGroup;
            }

            var bDepth = 0;
            current = b;
            while (current.ParentGroup != null)
            {
                ++bDepth;
                current = current.ParentGroup;
            }

            if (bDepth > aDepth)
            {
                for (var i = aDepth; i < bDepth; ++i)
                    b = b.ParentGroup;
            }
            else if (aDepth > bDepth)
            {
                for (var i = bDepth; i < aDepth; ++i)
                    a = a.ParentGroup;
            }

            // a and b are at the same depth find the base container they share

            while (a.ParentGroup != b.ParentGroup && a.ParentGroup != null)
            {
                a = a.ParentGroup;
                b = b.ParentGroup;
            }

            if (a.ParentGroup != null)
            {
                return a.ParentGroup.Items.IndexOf(a) - a.ParentGroup.Items.IndexOf(b);
            }

            var aElement = a as GraphElementModel;
            return aElement == null ? 0 : aElement.GraphModel.SectionModels.IndexOf(a) - aElement.GraphModel.SectionModels.IndexOf(b);
        }
    }
}
