// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// An element to display a set of variables. A special case of <see cref="BlackboardGroup"/>.
    /// </summary>
    /// <remarks>
    /// 'BlackboardVariableSet' is an element used to display a set of variables in the blackboard. It is a specialized version of <see cref="BlackboardGroup"/>,
    /// designed specifically for grouping and organizing related variables within the blackboard interface.
    /// </remarks>
    [UnityRestricted]
    internal class BlackboardVariableSet : BlackboardGroup
    {
        /// <inheritdoc/>
        public override Texture Icon => null;

        /// <inheritdoc/>
        protected override void BuildUI()
        {
            this.AddPackageStylesheet("BlackboardVariableSet.uss");
            base.BuildUI();

            // move title in from of selection border
            var selectionBorder = hierarchy.Children().FirstOrDefault(t => t.ClassListContains(selectionBorderUssClassName));
            selectionBorder?.SendToBack();
        }
    }
}
