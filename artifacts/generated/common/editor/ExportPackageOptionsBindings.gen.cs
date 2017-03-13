// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
namespace UnityEditor
{


[Flags]
public enum ExportPackageOptions
{
    
    Default = 0,
    
    Interactive = 1,
    
    Recurse = 2,
    
    IncludeDependencies = 4,
    
    IncludeLibraryAssets = 8
}

}
