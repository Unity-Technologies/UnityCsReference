// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Unity.UNetWeaver
{
    class MessageClassProcessor
    {
        TypeDefinition m_td;

        public MessageClassProcessor(TypeDefinition td)
        {
            Weaver.DLog(td, "MessageClassProcessor for " + td.Name);
            m_td = td;
        }

        public void Process()
        {
            Weaver.DLog(m_td, "MessageClassProcessor Start");

            Weaver.ResetRecursionCount();

            GenerateSerialization();
            if (Weaver.fail)
            {
                return;
            }

            GenerateDeSerialization();
            Weaver.DLog(m_td, "MessageClassProcessor Done");
        }

        void GenerateSerialization()
        {
            Weaver.DLog(m_td, "  GenerateSerialization");
            foreach (var m in m_td.Methods)
            {
                if (m.Name == "Serialize")
                    return;
            }

            if (m_td.Fields.Count == 0)
            {
                return;
            }

            // check for self-referencing types
            foreach (var field in m_td.Fields)
            {
                if (field.FieldType.FullName == m_td.FullName)
                {
                    Weaver.fail = true;
                    Log.Error("GenerateSerialization for " + m_td.Name + " [" + field.FullName + "]. [MessageBase] member cannot be self referencing.");
                    return;
                }
            }

            MethodDefinition serializeFunc = new MethodDefinition("Serialize", MethodAttributes.Public |
                    MethodAttributes.Virtual |
                    MethodAttributes.HideBySig,
                    Weaver.voidType);

            serializeFunc.Parameters.Add(new ParameterDefinition("writer", ParameterAttributes.None, Weaver.scriptDef.MainModule.ImportReference(Weaver.NetworkWriterType)));
            ILProcessor serWorker = serializeFunc.Body.GetILProcessor();

            foreach (var field in m_td.Fields)
            {
                if (field.IsStatic || field.IsPrivate || field.IsSpecialName)
                    continue;

                if (field.FieldType.Resolve().HasGenericParameters)
                {
                    Weaver.fail = true;
                    Log.Error("GenerateSerialization for " + m_td.Name + " [" + field.FieldType + "/" + field.FieldType.FullName + "]. [MessageBase] member cannot have generic parameters.");
                    return;
                }

                if (field.FieldType.Resolve().IsInterface)
                {
                    Weaver.fail = true;
                    Log.Error("GenerateSerialization for " + m_td.Name + " [" + field.FieldType + "/" + field.FieldType.FullName + "]. [MessageBase] member cannot be an interface.");
                    return;
                }

                MethodReference writeFunc = Weaver.GetWriteFunc(field.FieldType);
                if (writeFunc != null)
                {
                    serWorker.Append(serWorker.Create(OpCodes.Ldarg_1));
                    serWorker.Append(serWorker.Create(OpCodes.Ldarg_0));
                    serWorker.Append(serWorker.Create(OpCodes.Ldfld, field));
                    serWorker.Append(serWorker.Create(OpCodes.Call, writeFunc));
                }
                else
                {
                    Weaver.fail = true;
                    Log.Error("GenerateSerialization for " + m_td.Name + " unknown type [" + field.FieldType + "/" + field.FieldType.FullName + "]. [MessageBase] member variables must be basic types.");
                    return;
                }
            }
            serWorker.Append(serWorker.Create(OpCodes.Ret));

            m_td.Methods.Add(serializeFunc);
        }

        void GenerateDeSerialization()
        {
            Weaver.DLog(m_td, "  GenerateDeserialization");
            foreach (var m in m_td.Methods)
            {
                if (m.Name == "Deserialize")
                    return;
            }

            if (m_td.Fields.Count == 0)
            {
                return;
            }

            MethodDefinition serializeFunc = new MethodDefinition("Deserialize", MethodAttributes.Public |
                    MethodAttributes.Virtual |
                    MethodAttributes.HideBySig,
                    Weaver.voidType);

            serializeFunc.Parameters.Add(new ParameterDefinition("reader", ParameterAttributes.None, Weaver.scriptDef.MainModule.ImportReference(Weaver.NetworkReaderType)));
            ILProcessor serWorker = serializeFunc.Body.GetILProcessor();

            foreach (var field in m_td.Fields)
            {
                if (field.IsStatic || field.IsPrivate || field.IsSpecialName)
                    continue;

                MethodReference readerFunc = Weaver.GetReadFunc(field.FieldType);
                if (readerFunc != null)
                {
                    serWorker.Append(serWorker.Create(OpCodes.Ldarg_0));
                    serWorker.Append(serWorker.Create(OpCodes.Ldarg_1));
                    serWorker.Append(serWorker.Create(OpCodes.Call, readerFunc));
                    serWorker.Append(serWorker.Create(OpCodes.Stfld, field));
                }
                else
                {
                    Weaver.fail = true;
                    Log.Error("GenerateDeSerialization for " + m_td.Name + " unknown type [" + field.FieldType + "]. [SyncVar] member variables must be basic types.");
                    return;
                }
            }
            serWorker.Append(serWorker.Create(OpCodes.Ret));

            m_td.Methods.Add(serializeFunc);
        }
    }
}
