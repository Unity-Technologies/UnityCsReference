// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;
using System.Linq;

namespace UnityEditor.PackageManager.UI
{
    internal static class IOUtils
    {
        // Need to re-create this method since Unity's FileUtil equivalent (with overwrite) is internal only.
        // From: https://stackoverflow.com/questions/58744/copy-the-entire-contents-of-a-directory-in-c-sharp
        public static void DirectoryCopy(string sourcePath, string destinationPath, bool makeWritable = false)
        {
            Directory.CreateDirectory(destinationPath);

            //Now Create all of the directories
            foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(dirPath.Replace(sourcePath, destinationPath));

            //Copy all the files & Replaces any files with the same name
            foreach (string source in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
            {
                var dest = source.Replace(sourcePath, destinationPath);
                File.Copy(source, dest, true);
                if (makeWritable)
                    new FileInfo(dest).IsReadOnly = false;
            }
        }

        public static string SanitizeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            // Remove additional special characters that Unity doesn't like
            foreach (char c in "/:?<>*|\\~")
                name = name.Replace(c, '_');
            return name.Trim();
        }

        public static ulong DirectorySizeInBytes(string path)
        {
            var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
            ulong sizeInBytes = 0;
            foreach (var file in files)
            {
                var info = new FileInfo(file);
                sizeInBytes += (ulong)info.Length;
            }
            return sizeInBytes;
        }

        public static string CombinePaths(params string[] paths)
        {
            return paths.Aggregate(Path.Combine);
        }

        public static void RemovePathAndMeta(string path, bool removeEmptyParent = false)
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
            if (File.Exists(path + ".meta"))
                File.Delete(path + ".meta");
            if (removeEmptyParent)
            {
                var parent = Directory.GetParent(path);
                if (parent.GetDirectories().Length == 0 && parent.GetFiles().Length == 0)
                    RemovePathAndMeta(parent.ToString(), removeEmptyParent);
            }
        }
    }
}
