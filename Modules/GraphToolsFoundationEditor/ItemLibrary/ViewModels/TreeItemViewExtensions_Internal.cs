// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.ItemLibrary.Editor
{
    /// <summary>
    /// Extension methods for <see cref="ITreeItemView_Internal"/>.
    /// </summary>
    static class TreeItemViewExtensions_Internal
    {
        /// <summary>
        /// Gets the depth of this item by counting its parents.
        /// </summary>
        public static int GetDepth(this ITreeItemView_Internal self) => self.Parent?.GetDepth() + 1 ?? 0;

        /// <summary>
        /// Gets the path of this item by following its parents.
        /// </summary>
        public static string GetPath(this ITreeItemView_Internal self)
        {
            var parentPath = self.Parent?.Path;
            return string.IsNullOrEmpty(parentPath) ? self.Name : parentPath + "/" + self.Name;
        }
    }
}
