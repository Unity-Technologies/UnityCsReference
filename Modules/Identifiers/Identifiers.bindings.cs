// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.Identifiers
{
    [NativeHeader("Modules/Identifiers/Identifiers.h")]
    public static class Identifiers
    {
        public static string installationId => GetInstallationId();

        /// <summary>
        /// Raised when <see cref="userId"/> changes value. The argument is the new value.
        /// Subscribers are notified after the value is changed.
        /// </summary>
        public static event Action<string> userIdChanged;

        internal const int MaxUserIdLength = 1024;

        /// <summary>
        /// Gets or sets a user identifier supplied by the developer, typically the player's
        /// id in a third-party identity provider.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the assigned value exceeds 1024 characters.
        /// </exception>
        public static string userId
        {
            get => GetUserId();
            set
            {
                string incoming = value ?? string.Empty;
                if (incoming.Length > MaxUserIdLength)
                    throw new ArgumentOutOfRangeException(nameof(value), incoming.Length, $"userId cannot exceed {MaxUserIdLength} characters.");

                string current = GetUserId();
                if (current == incoming)
                    return;

                SetUserId(incoming);
                userIdChanged?.Invoke(incoming);
            }
        }

        [FreeFunction("UnityEngine_Identifiers_GetInstallationId")]
        extern static string GetInstallationId();

        [FreeFunction("UnityEngine_Identifiers_GetUserId")]
        extern static string GetUserId();

        [FreeFunction("UnityEngine_Identifiers_SetUserId")]
        extern static void SetUserId(string userId);
    }
}

