// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Class that defines the content of the error toolbar.
    /// </summary>
    class ErrorToolbarProvider : OverlayToolbarProvider
    {
        /// <inheritdoc />
        public override IEnumerable<string> GetElementIds()
        {
            return new[] { ErrorCountLabel.id, PreviousErrorButton.id, NextErrorButton.id };
        }
    }
}
