// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Collections;
using Unity.Entities;

namespace Unity.Animations
{
    /// <summary>
    /// Utility functions to find the index of a binding or a socket from its identifier.
    /// </summary>
    /// <remarks>
    /// Rigs and clips store the identifiers of their bindings in arrays of <see cref="StringHash"/>, and a rig stores its
    /// sockets, each with its identifier, in <see cref="RigDefinition.SocketValues"/>. Use these functions to turn an identifier
    /// into an index into one of these arrays, for example to match a binding of a clip with a binding of a rig.
    ///
    /// The index that <see cref="FindBindingIndex"/> returns is the position in the array that you search. For a rig,
    /// that is the index among the bindings of one kind, such as <see cref="BindingSet.TranslationBindings"/>. To get the
    /// index among all the bindings of the rig, add the first-binding index of that kind, such as
    /// <see cref="BindingSet.TranslationBindingIndex"/>.
    ///
    /// Both functions search the array linearly and return -1 if nothing matches.
    ///
    /// The following example uses the `Unity.Animations` and `Unity.Entities` namespaces. It finds a float channel by
    /// its identifier, then reads and writes its value in an animation stream.
    /// </remarks>
    /// <example nocheck="true">
    /// <code lang="cs"><![CDATA[{code Modules/AnimationCore/Tests/UTFTests/Playmode/DocumentationExamples/ReadChannelExample.cs#ReadAndWriteFloatChannel}]]></code>
    /// </example>
    /// <seealso cref="BindingSet"/>
    [GenerateTestsForBurstCompatibility]
    public static class BindingUtility
    {
        /// <summary>
        /// Find a binding in a binding array.
        /// </summary>
        /// <remarks>This is a linear search in the array.</remarks>
        /// <param name="bindings">The bindings' array to search in.</param>
        /// <param name="searchedBinding">The binding to match.</param>
        /// <returns>The index of the first binding in the array that matches. If no binding matches, returns -1.</returns>
        public static int FindBindingIndex(ref BlobArray<StringHash> bindings, StringHash searchedBinding)
        {
            // TODO : Optimize
            var i = 0;
            while (i < bindings.Length && !bindings[i].Equals(searchedBinding))
                ++i;
            return i < bindings.Length ? i : -1;
        }

        /// <summary>
        /// Finds the index of the socket that matches a binding in an array of sockets.
        /// </summary>
        /// <remarks>
        /// This method does a linear search in a BlobArray of <see cref="SocketNode"/> values.
        ///
        /// The following example uses the `Unity.Animations`, `Unity.Collections`, `Unity.Entities`, and
        /// `Unity.Mathematics` namespaces. It adds a socket to a rig, then finds the socket by its identifier to read
        /// its transform in character space.
        /// </remarks>
        /// <example nocheck="true">
        /// <code lang="cs"><![CDATA[{code Modules/AnimationCore/Tests/UTFTests/Playmode/DocumentationExamples/SocketExample.cs#AddAndReadSocket}]]></code>
        /// </example>
        /// <param name="sockets">The sockets' array to search in.</param>
        /// <param name="searchedBinding">The binding to match.</param>
        /// <returns>The index of the socket in the array matching the binding. If no socket matches, returns -1.</returns>
        public static int FindSocketIndex(ref BlobArray<SocketNode> sockets, StringHash searchedBinding)
        {
            // TODO : Optimize
            var i = 0;
            while (i < sockets.Length && !sockets[i].Id.Equals(searchedBinding))
                ++i;
            return i < sockets.Length ? i : -1;
        }
    }
}
