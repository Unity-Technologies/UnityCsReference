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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UnityEditor
{



public partial class AssetPostprocessor
{
    private string m_PathName;
    
    
    public string assetPath { get { return m_PathName; } set { m_PathName = value; } }
    
    
    [uei.ExcludeFromDocs]
public void LogWarning (string warning) {
    Object context = null;
    LogWarning ( warning, context );
}

public void LogWarning(string warning, [uei.DefaultValue("null")]  Object context ) { Debug.LogWarning(warning, context); }

    
    
    [uei.ExcludeFromDocs]
public void LogError (string warning) {
    Object context = null;
    LogError ( warning, context );
}

public void LogError(string warning, [uei.DefaultValue("null")]  Object context ) { Debug.LogError(warning, context); }

    
    
    public virtual uint GetVersion() {return 0; }
    
    
    public AssetImporter assetImporter { get { return AssetImporter.GetAtPath(assetPath); }  }
    
    
    [System.Obsolete ("To set or get the preview, call EditorUtility.SetAssetPreview or AssetPreview.GetAssetPreview instead", true)]
    public Texture2D preview { get { return null; } set {} }
    
    
    public virtual int GetPostprocessOrder() { return 0; }
    
    
    
}

}
