// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.CommandStateObserver;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Command that reframe the <see cref="GraphView"/> to show an error.
    /// </summary>
    class SetCurrentErrorCommand : ICommand
    {
        /// <summary>
        /// The index of the error to show.
        /// </summary>
        public int ErrorIndex { get; }

        /// <summary>
        /// The <see cref="GraphView"/> offset.
        /// </summary>
        public Vector3 GraphViewOffset { get; }

        /// <summary>
        /// The <see cref="GraphView"/> scale.
        /// </summary>
        public Vector3 GraphViewScale { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SetCurrentErrorCommand"/> class.
        /// </summary>
        /// <param name="errorIndex">The index of the error to show.</param>
        /// <param name="graphViewOffset">The <see cref="GraphView"/> offset.</param>
        /// <param name="graphViewScale">The <see cref="GraphView"/> scale.</param>
        public SetCurrentErrorCommand(int errorIndex, Vector3 graphViewOffset, Vector3 graphViewScale)
        {
            ErrorIndex = errorIndex;
            GraphViewOffset = graphViewOffset;
            GraphViewScale = graphViewScale;
        }

        /// <summary>
        /// Default command handler for <see cref="SetCurrentErrorCommand"/>.
        /// </summary>
        /// <param name="graphViewState">The graph view state.</param>
        /// <param name="command">The command/</param>
        public static void DefaultCommandHandler(GraphViewStateComponent graphViewState, SetCurrentErrorCommand command)
        {
            using (var graphViewUpdater = graphViewState.UpdateScope)
            {
                graphViewUpdater.SetCurrentErrorIndex(command.ErrorIndex);
                graphViewUpdater.Position = command.GraphViewOffset;
                graphViewUpdater.Scale = command.GraphViewScale;
            }
        }
    }
}
