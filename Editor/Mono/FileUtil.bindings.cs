// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using System.IO;


namespace UnityEditor
{
    // Lets you do ''move'', ''copy'', ''delete'' operations over files or directories
    [NativeHeader("Runtime/Utilities/FileUtilities.h")]
    [NativeHeader("Editor/Platform/Interface/EditorUtility.h")]
    public partial class FileUtil
    {
        // Deletes a file or a directory given a path.
        [FreeFunction]
        public static extern bool DeleteFileOrDirectory(string path);

        [FreeFunction("IsPathCreated")]
        private static extern bool PathExists(string path);

        // Copies a file or directory.
        public static void CopyFileOrDirectory(string source, string dest)
        {
            CheckForValidSourceAndDestinationArgumentsAndRaiseAnExceptionWhenNullOrEmpty(source, dest);

            if (PathExists(dest))
            {
                throw new System.IO.IOException(string.Format(
                        "Failed to Copy File / Directory from '{0}' to '{1}': destination path already exists.", source, dest));
            }

            if (!CopyFileOrDirectoryInternal(source, dest))
            {
                throw new System.IO.IOException(string.Format(
                        "Failed to Copy File / Directory from '{0}' to '{1}'.", source, dest));
            }
        }

        [FreeFunction("CopyFileOrDirectory")]
        private static extern bool CopyFileOrDirectoryInternal(string source, string dest);

        // Copies the file or directory following symbolic links.
        public static void CopyFileOrDirectoryFollowSymlinks(string source, string dest)
        {
            CheckForValidSourceAndDestinationArgumentsAndRaiseAnExceptionWhenNullOrEmpty(source, dest);

            if (PathExists(dest))
            {
                throw new System.IO.IOException(string.Format(
                        "Failed to Copy File / Directory from '{0}' to '{1}': destination path already exists.", source, dest));
            }

            if (!CopyFileOrDirectoryFollowSymlinksInternal(source, dest))
            {
                throw new System.IO.IOException(string.Format(
                        "Failed to Copy File / Directory from '{0}' to '{1}'.", source, dest));
            }
        }

        [FreeFunction("CopyFileOrDirectoryFollowSymlinks")]
        private static extern bool CopyFileOrDirectoryFollowSymlinksInternal(string source, string dest);

        // Moves a file or a directory from a given path to another path.
        public static void MoveFileOrDirectory(string source, string dest)
        {
            CheckForValidSourceAndDestinationArgumentsAndRaiseAnExceptionWhenNullOrEmpty(source, dest);

            if (PathExists(dest))
            {
                throw new System.IO.IOException(string.Format(
                        "Failed to Copy File / Directory from '{0}' to '{1}': destination path already exists.", source, dest));
            }

            if (!MoveFileOrDirectoryInternal(source, dest))
            {
                throw new System.IO.IOException(string.Format(
                        "Failed to Move File / Directory from '{0}' to '{1}'.", source, dest));
            }
        }

        [FreeFunction("MoveFileOrDirectory")]
        private static extern bool MoveFileOrDirectoryInternal(string source, string dest);

        private static void CheckForValidSourceAndDestinationArgumentsAndRaiseAnExceptionWhenNullOrEmpty(string source, string dest)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (dest == null) throw new ArgumentNullException("dest");

            if (source == string.Empty) throw new ArgumentException("source", "The source path cannot be empty.");
            if (dest == string.Empty) throw new ArgumentException("dest", "The destination path cannot be empty.");
        }

        // Returns a unique path in the Temp folder within your current project.
        [FreeFunction]
        public static extern string GetUniqueTempPathInProject();

        [FreeFunction("GetActualPathSlow")]
        internal static extern string GetActualPathName(string path);

        //*undocumented*
        [FreeFunction]
        public static extern string GetProjectRelativePath(string path);

        [FreeFunction]
        internal static extern string GetLastPathNameComponent(string path);

        [FreeFunction]
        internal static extern string DeleteLastPathNameComponent(string path);

        [FreeFunction("GetPathNameExtension")]
        internal static extern string GetPathExtension(string path);

        [FreeFunction("DeletePathNameExtension")]
        internal static extern string GetPathWithoutExtension(string path);

        [FreeFunction]
        internal static extern string ResolveSymlinks(string path);

        [FreeFunction]
        internal static extern bool IsSymlink(string path);

        // Replaces a file.
        public static void ReplaceFile(string src, string dst)
        {
            if (File.Exists(dst))
                FileUtil.DeleteFileOrDirectory(dst);

            FileUtil.CopyFileOrDirectory(src, dst);
        }

        // Replaces a directory.
        public static void ReplaceDirectory(string src, string dst)
        {
            if (Directory.Exists(dst))
                FileUtil.DeleteFileOrDirectory(dst);

            FileUtil.CopyFileOrDirectory(src, dst);
        }
    }
}
