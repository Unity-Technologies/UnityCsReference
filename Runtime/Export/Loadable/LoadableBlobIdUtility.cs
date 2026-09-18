// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace Unity.Loading
{
    /// <summary>
    /// Helpers for resolving a <see cref="LoadableBlobId"/> to a virtual filesystem path
    /// suitable for <c>AsyncReadManager.Read</c>.
    /// </summary>
    [VisibleToOtherModules]
    internal static class LoadableBlobIdUtility
    {
        const string k_CAHPrefix = "cah:/";
        const string k_UDSPrefix = "uds:/";

        /// <summary>
        /// Returns a path that the blob's bytes can be read from with <c>AsyncReadManager.Read</c>.
        /// The same call works in the editor and in a built player.
        /// </summary>
        /// <param name="id">The blob id read from a serialized object.</param>
        /// <returns>The path to read the blob's bytes from, or null if the id is invalid.</returns>
        public static string GetVFSPath(LoadableBlobId id)
        {
            if (!id.IsValid)
                return null;

            // Built content is served by the CAH filesystem; importer-registered blobs live in UDS.
            // Only the editor hosts both kinds of content; a player only ever reads built content.
            string prefix = (id.m_Flags & LoadableBlobIdFlags.FromBuiltContent) != 0 ? k_CAHPrefix : k_UDSPrefix;
            return prefix + id.m_Hash.ToString();
        }
    }
}
