// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// SUBSTANCE HOOK

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;

namespace UnityEngine
{
    public partial class ProceduralMaterial : Material
    {
        static void FeatureRemoved()
        {
            throw new Exception("Built-in support for Substance Designer materials has been removed from Unity. To continue using Substance Designer materials, you will need to install Allegorithmic's external importer from the Asset Store.");
        }

        //static extern void BindingsGeneratorTrigger();
    }

} // namespace UnityEngine
