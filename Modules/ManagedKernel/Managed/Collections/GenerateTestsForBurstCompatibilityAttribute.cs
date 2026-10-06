// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.Collections
{
    /// <summary>
    /// 
    /// </summary>
    [Obsolete("Use GenerateTestsForBurstCompatibility (UnityUpgradable) -> GenerateTestsForBurstCompatibilityAttribute", true)]
    public class BurstCompatibleAttribute : Attribute
    {
    }

    /// <summary>
    /// Documents and enforces (via generated tests) that the tagged method or property has to stay burst compatible.
    /// </summary>
    /// <remarks>This attribute cannot be used with private methods or properties.</remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Constructor, AllowMultiple = true)]
    public class GenerateTestsForBurstCompatibilityAttribute : Attribute
    {
        /// <summary>
        /// Burst compatible compile target.
        /// </summary>
        public enum BurstCompatibleCompileTarget
        {
            /// <summary>
            /// Player.
            /// </summary>
            Player,

            /// <summary>
            /// Editor.
            /// </summary>
            Editor,

            /// <summary>
            /// Player and editor.
            /// </summary>
            PlayerAndEditor
        }

        /// <summary>
        /// Types to be used for the declared generic type or method.
        /// </summary>
        /// <remarks>
        /// The generic type arguments are tracked separately for types and methods. Say a generic type also contains
        /// a generic method, like in the case of Foo&lt;T&gt;.Bar&lt;U&gt;(T baz, U blah). You must specify
        /// GenericTypeArguments for Foo and also for Bar to establish the concrete types for T and U. When code
        /// generation occurs for the Burst compatibility tests, any time T appears (in the definition of Foo)
        /// it will be replaced with the generic type argument you specified for Foo and whenever U appears
        /// (in method Bar's body) it will be replaced by whatever generic type argument you specified for the method
        /// Bar.
        /// </remarks>
        public Type[] GenericTypeArguments { get; set; }

        /// <summary>
        /// Specifies the symbol that must be defined in order for the method to be tested for Burst compatibility.
        /// </summary>
        public string RequiredUnityDefine = null;

        /// <summary>
        /// Specifies whether code should be Burst compiled for the player, editor, or both.
        /// </summary>
        /// <remarks>
        /// When set to BurstCompatibleCompileTarget.Editor, the generated Burst compatibility code will be
        /// surrounded by #if UNITY_EDITOR to ensure that the Burst compatibility test will only be executed in the
        /// editor. The code will be compiled with Burst function pointers. If you have a non-null RequiredUnityDefine,
        /// an #if with the RequiredUnityDefine will also be emitted.<para/> <para/>
        ///
        /// When set to BurstCompatibilityCompileTarget.Player, the generated Burst compatibility code will
        /// only be surrounded by an #if containing the RequiredUnityDefine (or nothing if RequiredUnityDefine is null).
        /// Instead of compiling with Burst function pointers, a player build is started where the Burst AOT compiler
        /// will verify the Burst compatibility. This is done to speed up Burst compilation for the compatibility tests
        /// since Burst function pointer compilation is not done in parallel.<para/> <para/>
        ///
        /// When set to BurstCompatibilityCompileTarget.PlayerAndEditor, the generated Burst compatibility code will
        /// only be surrounded by an #if containing the RequiredUnityDefine (or nothing if RequiredUnityDefine is null).
        /// The code will be compiled both by the editor (using Burst function pointers) and with a player build (using
        /// Burst AOT).<para/> <para/>
        ///
        /// For best performance of the Burst compatibility tests, prefer to use BurstCompatibilityCompileTarget.Player
        /// as much as possible.
        /// </remarks>
        public BurstCompatibleCompileTarget CompileTarget = BurstCompatibleCompileTarget.Player;
    }

    /// <summary>
    /// Attribute to exclude a method from burst compatibility testing even though the containing type is.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Constructor)]
    public class ExcludeFromBurstCompatTestingAttribute : Attribute
    {
        /// <summary>
        /// Reason for excluding a method from being included in generated Burst compilation tests
        /// </summary>
        public string Reason { get; set; }
        
        /// <summary>
        /// Create this attribute with the reason to exclude from burst compatibility testing.
        /// </summary>
        /// <param name="_reason">Reason target is not burst compatible.</param>
        public ExcludeFromBurstCompatTestingAttribute(string _reason)
        {
            Reason = _reason;
        }
    }
}
