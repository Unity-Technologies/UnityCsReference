// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace UnityEditor.Macros
{
    internal class MethodEvaluatorSurrogateProvider : ISerializationSurrogateProvider
    {
        /// <summary>
        /// EnumSurrogate is needed to pass enum as a simple integer value to the Editor.
        /// DataContractSerializer supports enum serialization, but it requires explicit markup on the enum type with
        /// [EnumMember] attribute - https://learn.microsoft.com/en-us/dotnet/framework/wcf/feature-details/enumeration-types-in-data-contracts.
        /// To avoid intrusive changes to the code, we use this surrogate instead.
        /// </summary>
        [DataContract(Name = "EnumSurrogate", Namespace = "com.unity3d.automation")]
        public class EnumSurrogate
        {
            [DataMember]
            public string TypeName;
            [DataMember]
            public string Value;
        }

        public object GetDeserializedObject(object obj, Type targetType)
        {
            if (obj is EnumSurrogate surrogate)
            {
                var type = Type.GetType(surrogate.TypeName);
                if (type == null)
                    throw new Exception($"Could not find Enum type {surrogate.TypeName} for UnityEditorCodeEval enum surrogate");
                return Enum.Parse(type, surrogate.Value);
            }

            return obj;
        }

        public object GetObjectToSerialize(object obj, Type targetType)
        {
            if (obj is Enum e)
            {
                return new EnumSurrogate { TypeName = obj.GetType().AssemblyQualifiedName, Value = e.ToString() };
            }

            return obj;
        }

        public Type GetSurrogateType(Type type)
        {
            if (type.IsAssignableFrom(typeof(Enum)))
            {
                return typeof(EnumSurrogate);
            }

            return type;
        }
    }
}
