// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Default implementation of <see cref="OverlayToolbarProvider"/> for the option toolbar.
    /// </summary>
    class OptionsToolbarProvider : OverlayToolbarProvider
    {
        /// <inheritdoc />
        public override IEnumerable<string> GetElementIds()
        {
            return new[] { OptionDropDownMenu.id };
        }
    }
}
