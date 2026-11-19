// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.GraphToolkit.ItemLibrary.Editor
{
    /// <summary>
    /// Extension methods for <see cref="ITreeItemView"/>.
    /// </summary>
    static class TreeItemViewExtensions
    {
        /// <summary>
        /// Gets the depth of this item by counting its parents.
        /// </summary>
        public static int GetDepth(this ITreeItemView self) => self.Parent?.GetDepth() + 1 ?? 0;

        /// <summary>
        /// Gets the path of this item by following its parents.
        /// </summary>
        /// <returns>The path.</returns>
        public static string GetPathFromParent(this ITreeItemView self)
        {
            var parentPath = self.Parent?.GetPath();
            return string.IsNullOrEmpty(parentPath) ? self.Name : parentPath + "/" + self.Name;
        }
    }
}
