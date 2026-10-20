// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngineInternal.Input
{
    /// <summary>
    /// Values a device command answers with. Zero or greater is success, negative names the failure.
    /// </summary>
    /// <remarks>
    /// Mirrors the result codes in InputDeviceCallbacks, in Modules/Input/InputDeviceData.h.
    /// </remarks>
    internal static class InputCommandResult
    {
        /// <summary>The target doesn't exist, or the handle naming it is stale.</summary>
        public const int invalidHandle = -9;

        /// <summary>The target exists but is in a state this command can't act on.</summary>
        public const int invalidState = -8;

        /// <summary>The buffer is the expected size but its contents aren't a valid payload.</summary>
        public const int invalidPayload = -7;

        /// <summary>The buffer is the wrong size, and no required size is reported.</summary>
        public const int invalidSize = -6;

        /// <summary>The buffer is too small, and the payload reports the size it needs.</summary>
        public const int bufferTooSmall = -5;

        /// <summary>The command isn't implemented, or isn't in this build.</summary>
        public const int notSupported = -4;

        /// <summary>The command runs only on the main thread, and didn't run.</summary>
        public const int mainThreadOnly = -3;

        public const int busy = -2;

        /// <summary>The command failed for a reason none of the other values names.</summary>
        public const int failure = -1;

        public const int success = 1;

        /// <summary>
        /// Returns the name of a result, for log and assertion messages.
        /// </summary>
        public static string ToName(long result)
        {
            switch (result)
            {
                case invalidHandle: return nameof(invalidHandle);
                case invalidState: return nameof(invalidState);
                case invalidPayload: return nameof(invalidPayload);
                case invalidSize: return nameof(invalidSize);
                case bufferTooSmall: return nameof(bufferTooSmall);
                case notSupported: return nameof(notSupported);
                case mainThreadOnly: return nameof(mainThreadOnly);
                case busy: return nameof(busy);
                case failure: return nameof(failure);
                case success: return nameof(success);
                default: return result.ToString();
            }
        }
    }
}
