// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace Unity.CommandStateObserver
{
    /// <summary>
    /// Extension methods for <see cref="UpdateType"/>
    /// </summary>
    static class UpdateTypeExtensions
    {
        /// <summary>
        /// Combines two update types together.
        /// </summary>
        /// <remarks>
        /// Use this method when an observer observes more than one state component and
        /// you want to combine the update types returned by each.
        /// </remarks>
        /// <param name="self">The first update type.</param>
        /// <param name="other">The second update type.</param>
        /// <returns>The combined update type.</returns>
        public static UpdateType Combine(this UpdateType self, UpdateType other)
        {
            if (self == UpdateType.Complete || other == UpdateType.Complete)
                return UpdateType.Complete;

            if (self == UpdateType.Partial || other == UpdateType.Partial)
                return UpdateType.Partial;

            return UpdateType.None;
        }

        /// <summary>
        /// Combines update types together.
        /// </summary>
        /// <remarks>
        /// Use this method when an observer observes more than one state component and
        /// you want to combine the update types returned by each.
        /// </remarks>
        /// <param name="updateTypes">The update types to combine.</param>
        /// <returns>The combined update type.</returns>
        public static UpdateType Combine(IEnumerable<UpdateType> updateTypes)
        {
            var partialFound = false;

            foreach (var viewUpdateType in updateTypes)
            {
                switch (viewUpdateType)
                {
                    case UpdateType.Complete:
                        return UpdateType.Complete;

                    case UpdateType.Partial:
                        partialFound = true;
                        break;
                }
            }

            return partialFound ? UpdateType.Partial : UpdateType.None;
        }
    }
}
