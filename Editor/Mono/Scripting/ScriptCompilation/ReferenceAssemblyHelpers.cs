// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;
using UnityEngine;

namespace UnityEditor.Scripting.ScriptCompilation
{
    class ReferenceAssemblyHelpers
    {
        public static bool IsReferenceAssemblyUnchanged(ScriptAssembly assembly, string outputDirectory)
        {
            var referenceAssemblyPath = AssetPath.Combine(outputDirectory, assembly.ReferenceAssemblyFilename);
            var assemblyFileHashPath = AssetPath.Combine(outputDirectory, $"{assembly.Filename}.hash");

            return IsFileUnchanged(referenceAssemblyPath, assemblyFileHashPath);
        }

        public static bool IsFileUnchanged(string path, string hashFilePath)
        {
            // File has changed if it does not exist.
            if (!File.Exists(path))
                return false;

            ulong newHash1 = 0;
            ulong newHash2 = 0;

            // Compute hash for file
            ComputeHashForFile(path, ref newHash1, ref newHash2);

            // If the hash file does not exist, create it and
            // and return that the file has changed.
            if (!File.Exists(hashFilePath))
            {
                WriteHashFile(hashFilePath, newHash1, newHash2);
                return false;
            }

            // Read stored hash for file
            ulong oldHash1 = 0;
            ulong oldHash2 = 0;

            // If we are unable to read the hash file,
            // write a new one and return that was file was changed
            if (!ReadHashFile(hashFilePath, ref oldHash1, ref oldHash2))
            {
                WriteHashFile(hashFilePath, newHash1, newHash2);
                return false;
            }

            // Check if hashes match to determine if file has changed
            bool hashMatch = newHash1 == oldHash1 && newHash2 == oldHash2;

            // If the new hash does not match the old hash,
            // create a new hash file
            if (!hashMatch)
            {
                WriteHashFile(hashFilePath, newHash1, newHash2);
            }

            return hashMatch;
        }

        static void ComputeHashForFile(string path, ref ulong h1, ref ulong h2)
        {
            byte[] bytes;

            // Read bytes from file as read-only
            using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                bytes = new byte[fileStream.Length];
                fileStream.Read(bytes, 0, (int)fileStream.Length);
            }

            ulong hash1;
            ulong hash2;

            unsafe
            {
                fixed(byte* bytePointer = bytes)
                {
                    HashUnsafeUtilities.ComputeHash128(bytePointer, (ulong)bytes.LongLength, &hash1, &hash2);
                }
            }

            h1 = hash1;
            h2 = hash2;
        }

        static bool ReadHashFile(string path, ref ulong h1, ref ulong h2)
        {
            using (BinaryReader br = new BinaryReader(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                if (br.BaseStream.Length != sizeof(ulong) * 2)
                    return false;

                h1 = br.ReadUInt64();
                h2 = br.ReadUInt64();
            }

            return true;
        }

        static void WriteHashFile(string path, ulong h1, ulong h2)
        {
            using (BinaryWriter bw = new BinaryWriter(new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
            {
                bw.Write(h1);
                bw.Write(h2);
            }
        }
    }
}
