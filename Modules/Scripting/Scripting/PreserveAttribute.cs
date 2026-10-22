// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Scripting
{
    ///<summary>Prevents byte code stripping from removing a class, method, field, or property.</summary>
    ///<remarks>When you create a build, Unity tries to strip unused code from your project, which can help keep build sizes small. However, you might want some code to not be stripped, even if it looks like it's not used. This can happen if for example you use reflection to call a method, or instantiate an object of a certain class.
    ///
    ///You can apply the `[Preserve]` attribute to classes, methods, fields and properties. You can also use a link.xml file to tell the linker to not remove things.
    ///
    ///For more details on `[Preserve]` and link.xml, refer to [Managed code stripping](xref:um-managed-code-stripping).
    ///
    /// For third party libraries that don't take on a dependency on UnityEngine.dll, they can define their own `PreserveAttribute`. The code stripper respects that and also considers any attribute with the exact name `PreserveAttribute` as a reason not to code strip, regardless of the namespace or assembly of the attribute.
    ///</remarks>
    ///<example>
    ///  <code><![CDATA[
    ///using UnityEngine;
    ///using System.Collections;
    ///using System.Reflection;
    ///using UnityEngine.Scripting;
    ///
    ///public class NewBehaviourScript : MonoBehaviour
    ///{
    ///    void Start()
    ///    {
    ///        ReflectionExample.InvokeBoinkByReflection();
    ///    }
    ///}
    ///
    ///public class ReflectionExample
    ///{
    ///    static public void InvokeBoinkByReflection()
    ///    {
    ///        typeof(ReflectionExample).GetMethod("Boink", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
    ///    }
    ///
    ///    // No other code directly references the Boink method, so when when stripping is enabled,
    ///    // it will be removed unless the [Preserve] attribute is applied.
    ///    [Preserve]
    ///    static void Boink()
    ///    {
    ///        Debug.Log("Boink");
    ///    }
    ///}
    ///]]></code>
    ///</example>
    [System.AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Property
        | AttributeTargets.Constructor | AttributeTargets.Interface | AttributeTargets.Delegate | AttributeTargets.Event | AttributeTargets.Struct | AttributeTargets.Assembly | AttributeTargets.Enum, Inherited = false)]
    public class PreserveAttribute : Attribute
    {
    }
}
