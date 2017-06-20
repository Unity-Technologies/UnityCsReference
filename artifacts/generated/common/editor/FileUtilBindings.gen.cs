// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using UnityEngine;
using Object = UnityEngine.Object;
using System.IO;


namespace UnityEditor
{
public sealed partial class FileUtil
{
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool DeleteFileOrDirectory (string path) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  bool PathExists (string path) ;

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
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  bool CopyFileOrDirectoryInternal (string source, string dest) ;

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
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  bool CopyFileOrDirectoryFollowSymlinksInternal (string source, string dest) ;

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
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  bool MoveFileOrDirectoryInternal (string source, string dest) ;

    private static void CheckForValidSourceAndDestinationArgumentsAndRaiseAnExceptionWhenNullOrEmpty(string source, string dest)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (dest == null) throw new ArgumentNullException("dest");

            if (source == string.Empty) throw new ArgumentException("source", "The source path cannot be empty.");
            if (dest == string.Empty) throw new ArgumentException("dest", "The destination path cannot be empty.");
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string GetUniqueTempPathInProject () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  string GetActualPathName (string path) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string GetProjectRelativePath (string path) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  string GetLastPathNameComponent (string path) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  string DeleteLastPathNameComponent (string path) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  string GetPathExtension (string path) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  string GetPathWithoutExtension (string path) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  string ResolveSymlinks (string path) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool IsSymlink (string path) ;

    public static void ReplaceFile(string src, string dst)
        {
            if (File.Exists(dst))
                FileUtil.DeleteFileOrDirectory(dst);

            FileUtil.CopyFileOrDirectory(src, dst);
        }
    
    
    public static void ReplaceDirectory(string src, string dst)
        {
            if (Directory.Exists(dst))
                FileUtil.DeleteFileOrDirectory(dst);

            FileUtil.CopyFileOrDirectory(src, dst);
        }
    
    
}

}
