// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Abstract class for wires.
    /// </summary>
    [UnityRestricted]
    internal abstract class AbstractWire : GraphElement
    {
        /// <summary>
        /// The USS modifier added to ghost wires.
        /// </summary>
        public static readonly string ghostUssModifier = "ghost";

        /// <summary>
        /// The wire model.
        /// </summary>
        public WireModel WireModel => Model as WireModel;

        /// <summary>
        /// Gets the position of the start of the wire.
        /// </summary>
        /// <returns>The start position.</returns>
        public abstract Vector2 GetFrom();

        /// <summary>
        /// Sets the position of the end of the wire.
        /// </summary>
        /// <returns>The end position.</returns>
        public abstract Vector2 GetTo();

        /// <inheritdoc />
        public override bool ShowInMiniMap => false;

        /// <inheritdoc/>
        protected override DynamicBorder CreateDynamicBorder()
        {
            return null;
        }
    }
}
