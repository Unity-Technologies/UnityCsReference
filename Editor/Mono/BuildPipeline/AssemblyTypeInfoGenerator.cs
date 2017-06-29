// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// #define DOLOG
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Mono.Cecil;
using Unity.SerializationLogic;

namespace UnityEditor
{
    using GenericInstanceTypeMap = System.Collections.Generic.Dictionary<TypeReference, TypeReference>;

    internal class AssemblyTypeInfoGenerator
    {
        [Flags]
        public enum FieldInfoFlags
        {
            None = 0,
            FixedBuffer = (1 << 0)
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct FieldInfo
        {
            public string name;
            public string type;
            public FieldInfoFlags flags;
            public int fixedBufferLength;
            public string fixedBufferTypename;
        };

        [StructLayout(LayoutKind.Sequential)]
        public struct ClassInfo
        {
            public string name;
            public FieldInfo[] fields;
        };


        private AssemblyDefinition assembly_;
        private List<ClassInfo> classes_ = new List<ClassInfo>();
        private TypeResolver typeResolver = new TypeResolver(null);

        public ClassInfo[] ClassInfoArray
        {
            get { return classes_.ToArray(); }
        }

        private class AssemblyResolver : BaseAssemblyResolver
        {
            public static IAssemblyResolver WithSearchDirs(params string[] searchDirs)
            {
                var resolver = new AssemblyResolver();
                foreach (var searchDir in searchDirs)
                    resolver.AddSearchDirectory(searchDir);

                // remove the two directories installed by default as this can cause issues with assemblies outside of the Assets folder
                resolver.RemoveSearchDirectory(".");
                resolver.RemoveSearchDirectory("bin");

                return resolver;
            }

            readonly IDictionary m_Assemblies;

            private AssemblyResolver()
                : this(new Hashtable())
            {
            }

            private AssemblyResolver(IDictionary assemblyCache)
            {
                m_Assemblies = assemblyCache;
            }

            public override AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
            {
                var asm = (AssemblyDefinition)m_Assemblies[name.Name];
                if (asm != null)
                    return asm;

                asm = base.Resolve(name, parameters);
                m_Assemblies[name.Name] = asm;

                return asm;
            }
        }

        public AssemblyTypeInfoGenerator(string assembly, string[] searchDirs)
        {
            assembly_ = AssemblyDefinition.ReadAssembly(assembly, new ReaderParameters
            {
                AssemblyResolver = AssemblyResolver.WithSearchDirs(searchDirs)
            });
        }

        public AssemblyTypeInfoGenerator(string assembly, IAssemblyResolver resolver)
        {
            assembly_ = AssemblyDefinition.ReadAssembly(assembly, new ReaderParameters
            {
                AssemblyResolver = resolver
            });
        }

        // In embedded API inner names are separated by plus instead of fwdslash, eg. MyClass+MyInner, not MyClass/MyInner
        // so we convert '/' to '+' here.
        // Generic parameters are placed in square brackets instead of angle brackets
        private string GetMonoEmbeddedFullTypeNameFor(TypeReference type)
        {
            var typeSpec = type as TypeSpecification;
            string typeName;

            // Strip modifiers like volatile, as Mono doesn't treat them as part of type
            if (typeSpec != null && typeSpec.IsRequiredModifier)
            {
                type  = typeSpec.ElementType;
            }
            else if (type.IsRequiredModifier)
            {
                type = type.GetElementType();
            }

            typeName = type.FullName;

            // Mono compiler generates internal types with names such as  "<byteArray>__FixedBuffer0" and "<OnFinishSubmit>c__Iterator0"
            // so we only replace angle brackets with square brackets if the type is a generic instance or has generic parameters.
            if (type.HasGenericParameters || type.IsGenericInstance)
                typeName = typeName.Replace('<', '[').Replace('>', ']');

            return typeName.Replace('/', '+');
        }

        /* We use this GenericInstanceTypeMap to map generic types to their generic instance types,
           This is needed for correctly assess inheritance in cases like this:

            class One<T>
            {
                public T one;
            }

            class Two : One<int>
            {
            }

           In this case, we look at type Two, and see that it inherits from type One<int>,
           so we map T -> int. When we come across looking at field "one", we see that its
           declaring type is a generic instance type and that T maps to int, so it writes down
           that class One<int> has a field "int one".

           We use a new map for going through each type instance (check GatherClassInfo()).
        */
        private TypeReference ResolveGenericInstanceType(TypeReference typeToResolve, GenericInstanceTypeMap genericInstanceTypeMap)
        {
            var arrayType = typeToResolve as ArrayType;

            if (arrayType != null)
            {
                typeToResolve = new ArrayType(ResolveGenericInstanceType(arrayType.ElementType, genericInstanceTypeMap), arrayType.Rank);
            }

            while (genericInstanceTypeMap.ContainsKey(typeToResolve))
            {
                typeToResolve = genericInstanceTypeMap[typeToResolve];
            }

            if (typeToResolve.IsGenericInstance)
            {
                // Handle the case of nested generics, like List<Dictionary<int, List<string>>>
                var genericInstance = ((GenericInstanceType)typeToResolve);
                typeToResolve = MakeGenericInstance(genericInstance.ElementType, genericInstance.GenericArguments, genericInstanceTypeMap);
            }

            return typeToResolve;
        }

        private void AddType(TypeReference typeRef, GenericInstanceTypeMap genericInstanceTypeMap)
        {
            // Prevent duplicates
            if (classes_.Any(x => x.name == GetMonoEmbeddedFullTypeNameFor(typeRef)))
            {
                return;
            }

            TypeDefinition type;

            try
            {
                type = typeRef.Resolve();
            } // This will happen for types which we don't have access to, like Windows.Foundation.IAsyncOperation<int>
            catch (AssemblyResolutionException)
            {
                return;
            }
            catch (NotSupportedException) // "NotSupportedException: Version not supported: 255.255.255.255" is thrown when assembly references WinRT assembly (e.g. mscorlib)
            {
                return;
            }

            if (type == null) return;

            if (typeRef.IsGenericInstance)
            {
                var arguments = ((GenericInstanceType)typeRef).GenericArguments;
                var parameters = type.GenericParameters;

                for (int i = 0; i < arguments.Count; i++)
                {
                    if (parameters[i] != arguments[i])
                    {
                        genericInstanceTypeMap[parameters[i]] = arguments[i];
                    }
                }

                typeResolver.Add((GenericInstanceType)typeRef);
            }

            /* Process class itself before nested/base types in case user does something evil, for example:

                class Outer
                {
                    class Inner : Child
                    {
                    }
                }

                class Child : Outer
                {
                }
            */

            bool shouldImplementDeserializable = false;

            try
            {
                shouldImplementDeserializable = UnitySerializationLogic.ShouldImplementIDeserializable(type);
            }
            catch
            {
                // If assembly has unknown reference (for ex., see tests VariousPlugins, where Metro plugins are used), skip field
            }

            if (!shouldImplementDeserializable)
            {
                // In this case we only take care of processing the nested types, if any.
                AddNestedTypes(type, genericInstanceTypeMap);
            }
            else
            {
                var ci = new ClassInfo();
                ci.name = GetMonoEmbeddedFullTypeNameFor(typeRef);
                ci.fields = GetFields(type, typeRef.IsGenericInstance, genericInstanceTypeMap);

                classes_.Add(ci);

                // Fetch info for inner types
                AddNestedTypes(type, genericInstanceTypeMap);

                // Add base type
                AddBaseType(typeRef, genericInstanceTypeMap);
            }

            if (typeRef.IsGenericInstance)
                typeResolver.Remove((GenericInstanceType)typeRef);
        }

        private void AddNestedTypes(TypeDefinition type, GenericInstanceTypeMap genericInstanceTypeMap)
        {
            foreach (TypeDefinition nestedType in type.NestedTypes)
            {
                AddType(nestedType, genericInstanceTypeMap);
            }
        }

        private void AddBaseType(TypeReference typeRef, GenericInstanceTypeMap genericInstanceTypeMap)
        {
            var baseType = typeRef.Resolve().BaseType;
            if (baseType != null)
            {
                /* If we are processing generic instance type and
                   its base type happens to be a generic instance class as well,
                   we want to forward our generic arguments to the base. Consider:

                    class One<T>
                    {
                        T one;
                    }

                    class Two<T1, T2> : One<T2>
                    {
                    }

                    class Three : Two<int, float>
                    {
                    }

                   In this case, three is inheriting from Two<int, float>,
                   so we want Two<int, float> to inherit from One<float>,
                   however, cecil will tell us that base of Two<T1, T2> is One<T2>,
                   therefore we have to create the generic instance type of One<float> ourselves
                    */
                if (typeRef.IsGenericInstance && baseType.IsGenericInstance)
                {
                    var genericInstance = ((GenericInstanceType)baseType);
                    baseType = MakeGenericInstance(genericInstance.ElementType, genericInstance.GenericArguments, genericInstanceTypeMap);
                }

                AddType(baseType, genericInstanceTypeMap);
            }
        }

        private TypeReference MakeGenericInstance(TypeReference genericClass, IEnumerable<TypeReference> arguments, GenericInstanceTypeMap genericInstanceTypeMap)
        {
            var genericInstance = new GenericInstanceType(genericClass);

            foreach (var argument in arguments.Select(x => ResolveGenericInstanceType(x, genericInstanceTypeMap)))
            {
                genericInstance.GenericArguments.Add(argument);
            }

            return genericInstance;
        }

        private FieldInfo[] GetFields(TypeDefinition type, bool isGenericInstance, GenericInstanceTypeMap genericInstanceTypeMap)
        {
            var fields = new List<FieldInfo>();

            foreach (FieldDefinition field in type.Fields)
            {
                var fieldInfo = GetFieldInfo(type, field, isGenericInstance, genericInstanceTypeMap);

                if (fieldInfo != null)
                {
                    fields.Add(fieldInfo.Value);
                }
            }

            return fields.ToArray();
        }

        private static CustomAttribute GetFixedBufferAttribute(FieldDefinition fieldDefinition)
        {
            if (!fieldDefinition.HasCustomAttributes)
                return null;

            return fieldDefinition.CustomAttributes.SingleOrDefault(a => a.AttributeType.FullName == "System.Runtime.CompilerServices.FixedBufferAttribute");
        }

        private static int GetFixedBufferLength(CustomAttribute fixedBufferAttribute)
        {
            return (Int32)fixedBufferAttribute.ConstructorArguments[1].Value;
        }

        private static string GetFixedBufferTypename(CustomAttribute fixedBufferAttribute)
        {
            var typeRef = (TypeReference)fixedBufferAttribute.ConstructorArguments[0].Value;
            return typeRef.Name;
        }

        private FieldInfo? GetFieldInfo(TypeDefinition type, FieldDefinition field, bool isDeclaringTypeGenericInstance,
            GenericInstanceTypeMap genericInstanceTypeMap)
        {
            if (!WillSerialize(field))
                return null;

            var ti = new FieldInfo();
            ti.name = field.Name;

            TypeReference fieldType;

            if (isDeclaringTypeGenericInstance)
            {
                fieldType = ResolveGenericInstanceType(field.FieldType, genericInstanceTypeMap);
            }
            else
            {
                fieldType = field.FieldType;
            }

            ti.type = GetMonoEmbeddedFullTypeNameFor(fieldType);
            ti.flags = FieldInfoFlags.None;

            var fixedBufferAttribute = GetFixedBufferAttribute(field);

            if (fixedBufferAttribute  != null)
            {
                ti.flags |= FieldInfoFlags.FixedBuffer;
                ti.fixedBufferLength = GetFixedBufferLength(fixedBufferAttribute);
                ti.fixedBufferTypename = GetFixedBufferTypename(fixedBufferAttribute);
            }
            return ti;
        }

        private bool WillSerialize(FieldDefinition field)
        {
            try
            {
                return UnitySerializationLogic.WillUnitySerialize(field, typeResolver);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogFormat("Field '{0}' from '{1}', exception {2}", field.FullName, field.Module.FileName, ex.Message);
                // If assembly has unknown reference (for ex., see tests VariousPlugins, where Metro plugins are used), skip field
                return false;
            }
        }

        public ClassInfo[] GatherClassInfo()
        {
            foreach (ModuleDefinition module in assembly_.Modules)
            {
                foreach (TypeDefinition type in module.Types)
                {
                    // Skip compiler-generated <Module> class
                    if (type.Name == "<Module>")
                        continue;

                    AddType(type, new Dictionary<TypeReference, TypeReference>());
                }
            }
            return classes_.ToArray();
        }
    }
}
