using System;

namespace UnityEngine.Scripting
{
    /// <summary>
    /// Marks a method whose life is tied to a specific type:
    ///   1. Any type references in this method are not marked as 'uses' to the UnityLinker
    ///      (they won't keep a type from being stripped out)
    ///   2. If the named type is instantiated (e.g. one of its instance constructors is marked), this method's
    ///      body is walked normally.
    ///   3. If the named type is never instantiated, this method body is stubbed, taking the typeof(T) reference in its body with it.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public class StubBodyIfTypeNotInstantiatedAttribute : Attribute
    {
        public StubBodyIfTypeNotInstantiatedAttribute(Type preserveIfInstantiated) { }
        // For types not accessible at compile time. Format follows XML doc-signature convention:
        // nested types separated by '.', generics via `arity (e.g. "Ns.OuterType.Nested`1").
        // Assembly defaults to the declaring assembly of the annotated method.
        public StubBodyIfTypeNotInstantiatedAttribute(string preserveIfInstantiatedTypeName) { }
        // Same as above with an explicit assembly short name.
        public StubBodyIfTypeNotInstantiatedAttribute(string preserveIfInstantiatedTypeName, string assemblyName) { }
    }
}
