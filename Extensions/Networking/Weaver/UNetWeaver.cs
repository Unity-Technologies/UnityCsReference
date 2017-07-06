// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Linq;
using Mono.Cecil.Pdb;
using Mono.Cecil.Mdb;

namespace Unity.UNetWeaver
{
    public enum OutSymbolsFormat
    {
        None,
        Pdb,
        Mdb
    }

    // This data is flushed each time - if we are run multiple times in the same process/domain
    class WeaverLists
    {
        // [SyncVar] member variables that should be replaced
        public List<FieldDefinition> replacedFields = new List<FieldDefinition>();
        // setter functions that replace [SyncVar] member variable references
        public List<MethodDefinition> replacementProperties = new List<MethodDefinition>();
        // GameObject SyncVar generated netId fields
        public List<FieldDefinition> netIdFields = new List<FieldDefinition>();

        // [Command]/[ClientRpc] functions that should be replaced
        public List<MethodDefinition> replacedMethods = new List<MethodDefinition>();
        // remote call functions that replace [Command]/[ClientRpc] references
        public List<MethodDefinition> replacementMethods = new List<MethodDefinition>();

        public HashSet<string> replacementMethodNames = new HashSet<string>();

        // [SyncEvent] invoke functions that should be replaced
        public List<EventDefinition> replacedEvents = new List<EventDefinition>();
        // remote call functions that replace [SyncEvent] references
        public List<MethodDefinition> replacementEvents = new List<MethodDefinition>();

        public Dictionary<string, MethodReference> readFuncs;
        public Dictionary<string, MethodReference> readByReferenceFuncs;
        public Dictionary<string, MethodReference> writeFuncs;

        public List<MethodDefinition> generatedReadFunctions = new List<MethodDefinition>();
        public List<MethodDefinition> generatedWriteFunctions = new List<MethodDefinition>();

        public TypeDefinition generateContainerClass;
        public Dictionary<string, int> numSyncVars = new Dictionary<string, int>();
    }

    class Weaver
    {
        // UNetwork types
        public static TypeReference NetworkBehaviourType;
        public static TypeReference NetworkBehaviourType2;
        public static TypeReference MonoBehaviourType;
        public static TypeReference ScriptableObjectType;
        public static TypeReference NetworkConnectionType;
        public static TypeReference ULocalConnectionToServerType;
        public static TypeReference ULocalConnectionToClientType;

        public static TypeReference MessageBaseType;
        public static TypeReference SyncListStructType;

        public static MethodReference NetworkBehaviourDirtyBitsReference;
        public static TypeReference NetworkClientType;
        public static TypeReference NetworkServerType;
        public static TypeReference NetworkCRCType;

        public static TypeReference NetworkReaderType;
        public static TypeDefinition NetworkReaderDef;

        public static TypeReference NetworkWriterType;
        public static TypeDefinition NetworkWriterDef;

        public static MethodReference NetworkWriterCtor;
        public static MethodReference NetworkReaderCtor;
        public static TypeReference MemoryStreamType;
        public static MethodReference MemoryStreamCtor;
        public static MethodReference getComponentReference;
        public static MethodReference getUNetIdReference;
        public static MethodReference getPlayerIdReference;
        public static TypeReference NetworkIdentityType;
        public static TypeReference NetworkInstanceIdType;
        public static TypeReference NetworkSceneIdType;
        public static TypeReference IEnumeratorType;

        public static TypeReference ClientSceneType;
        public static MethodReference FindLocalObjectReference;
        public static MethodReference RegisterBehaviourReference;
        public static MethodReference ReadyConnectionReference;

        public static TypeReference ComponentType;

        public static TypeReference CmdDelegateReference;
        public static MethodReference CmdDelegateConstructor;

        public static MethodReference NetworkReaderReadInt32;

        public static MethodReference NetworkWriterWriteInt32;
        public static MethodReference NetworkWriterWriteInt16;

        public static MethodReference NetworkServerGetActive;
        public static MethodReference NetworkServerGetLocalClientActive;
        public static MethodReference NetworkClientGetActive;
        public static MethodReference UBehaviourIsServer;
        public static MethodReference NetworkReaderReadPacked32;
        public static MethodReference NetworkReaderReadPacked64;
        public static MethodReference NetworkReaderReadByte;
        public static MethodReference NetworkWriterWritePacked32;
        public static MethodReference NetworkWriterWritePacked64;

        public static MethodReference NetworkWriterWriteNetworkInstanceId;
        public static MethodReference NetworkWriterWriteNetworkSceneId;

        public static MethodReference NetworkReaderReadNetworkInstanceId;
        public static MethodReference NetworkReaderReadNetworkSceneId;
        public static MethodReference NetworkInstanceIsEmpty;

        public static MethodReference NetworkReadUInt16;
        public static MethodReference NetworkWriteUInt16;

        // custom attribute types
        public static TypeReference SyncVarType;
        public static TypeReference CommandType;
        public static TypeReference ClientRpcType;
        public static TypeReference TargetRpcType;
        public static TypeReference SyncEventType;
        public static TypeReference SyncListType;
        public static MethodReference SyncListInitBehaviourReference;
        public static MethodReference SyncListInitHandleMsg;
        public static MethodReference SyncListClear;
        public static TypeReference NetworkSettingsType;

        // sync list types
        public static TypeReference SyncListFloatType;
        public static TypeReference SyncListIntType;
        public static TypeReference SyncListUIntType;
        public static TypeReference SyncListBoolType;
        public static TypeReference SyncListStringType;

        public static MethodReference SyncListFloatReadType;
        public static MethodReference SyncListIntReadType;
        public static MethodReference SyncListUIntReadType;
        public static MethodReference SyncListStringReadType;
        public static MethodReference SyncListBoolReadType;

        public static MethodReference SyncListFloatWriteType;
        public static MethodReference SyncListIntWriteType;
        public static MethodReference SyncListUIntWriteType;
        public static MethodReference SyncListBoolWriteType;
        public static MethodReference SyncListStringWriteType;

        // system types
        public static TypeReference voidType;
        public static TypeReference singleType;
        public static TypeReference doubleType;
        public static TypeReference decimalType;
        public static TypeReference boolType;
        public static TypeReference stringType;
        public static TypeReference int64Type;
        public static TypeReference uint64Type;
        public static TypeReference int32Type;
        public static TypeReference uint32Type;
        public static TypeReference int16Type;
        public static TypeReference uint16Type;
        public static TypeReference byteType;
        public static TypeReference sbyteType;
        public static TypeReference charType;
        public static TypeReference objectType;
        public static TypeReference valueTypeType;
        public static TypeReference vector2Type;
        public static TypeReference vector3Type;
        public static TypeReference vector4Type;
        public static TypeReference colorType;
        public static TypeReference color32Type;
        public static TypeReference quaternionType;
        public static TypeReference rectType;
        public static TypeReference rayType;
        public static TypeReference planeType;
        public static TypeReference matrixType;
        public static TypeReference hashType;
        public static TypeReference typeType;
        public static TypeReference gameObjectType;
        public static TypeReference transformType;
        public static TypeReference unityObjectType;
        public static MethodReference gameObjectInequality;

        public static MethodReference setSyncVarReference;
        public static MethodReference setSyncVarHookGuard;
        public static MethodReference getSyncVarHookGuard;
        public static MethodReference setSyncVarGameObjectReference;
        public static MethodReference registerCommandDelegateReference;
        public static MethodReference registerRpcDelegateReference;
        public static MethodReference registerEventDelegateReference;
        public static MethodReference registerSyncListDelegateReference;
        public static MethodReference getTypeReference;
        public static MethodReference getTypeFromHandleReference;
        public static MethodReference logErrorReference;
        public static MethodReference logWarningReference;
        public static MethodReference sendCommandInternal;
        public static MethodReference sendRpcInternal;
        public static MethodReference sendTargetRpcInternal;
        public static MethodReference sendEventInternal;

        public static WeaverLists lists;

        public static AssemblyDefinition scriptDef;
        public static ModuleDefinition corLib;
        public static AssemblyDefinition m_UnityAssemblyDefinition;
        public static AssemblyDefinition m_UNetAssemblyDefinition;

        static bool m_DebugFlag = true;

        public static bool fail;
        public static bool generateLogErrors = false;

        // this is used to prevent stack overflows when generating serialization code when there are self-referencing types.
        // All the utility classes use GetWriteFunc() to generate serialization code, so the recursion check is implemented there instead of in each utility class.
        // A NetworkBehaviour with the max SyncVars (32) can legitimately increment this value to 65 - so max must be higher than that
        const int MaxRecursionCount = 128;
        static int s_RecursionCount;
        public static void ResetRecursionCount()
        {
            s_RecursionCount = 0;
        }

        public static bool CanBeResolved(TypeReference parent)
        {
            while (parent != null)
            {
                if (parent.Scope.Name == "Windows")
                {
                    return false;
                }

                if (parent.Scope.Name == "mscorlib")
                {
                    var resolved = parent.Resolve();
                    return resolved != null;
                }

                try
                {
                    parent = parent.Resolve().BaseType;
                }
                catch
                {
                    return false;
                }
            }
            return true;
        }

        public static bool IsArrayType(TypeReference variable)
        {
            if ((variable.IsArray && ((ArrayType)variable).ElementType.IsArray) || // jagged array
                (variable.IsArray && ((ArrayType)variable).Rank > 1)) // multidimensional array
                return false;
            return true;
        }

        public static void DLog(TypeDefinition td, string fmt, params object[] args)
        {
            if (!m_DebugFlag)
                return;

            Console.WriteLine("[" + td.Name + "] " + String.Format(fmt, args));
        }

        public static int GetSyncVarStart(string className)
        {
            if (lists.numSyncVars.ContainsKey(className))
            {
                int num =  lists.numSyncVars[className];
                return num;
            }
            // start at zero
            return 0;
        }

        public static void SetNumSyncVars(string className, int num)
        {
            lists.numSyncVars[className] = num;
        }

        public static MethodReference GetWriteFunc(TypeReference variable)
        {
            if (s_RecursionCount++ > MaxRecursionCount)
            {
                Log.Error("GetWriteFunc recursion depth exceeded for " + variable.Name + ". Check for self-referencing member variables.");
                fail = true;
                return null;
            }

            if (lists.writeFuncs.ContainsKey(variable.FullName))
            {
                var foundFunc = lists.writeFuncs[variable.FullName];
                if (foundFunc.Parameters[0].ParameterType.IsArray == variable.IsArray)
                {
                    return foundFunc;
                }
            }

            if (variable.IsByReference)
            {
                // error??
                Log.Error("GetWriteFunc variable.IsByReference error.");
                return null;
            }

            MethodDefinition newWriterFunc;

            if (variable.IsArray)
            {
                var elementType = variable.GetElementType();
                var elemenWriteFunc = GetWriteFunc(elementType);
                if (elemenWriteFunc == null)
                {
                    return null;
                }
                newWriterFunc = GenerateArrayWriteFunc(variable, elemenWriteFunc);
            }
            else
            {
                if (variable.Resolve().IsEnum)
                {
                    return NetworkWriterWriteInt32;
                }

                newWriterFunc = GenerateWriterFunction(variable);
            }

            if (newWriterFunc == null)
            {
                return null;
            }

            RegisterWriteFunc(variable.FullName, newWriterFunc);
            return newWriterFunc;
        }

        static public void RegisterWriteFunc(string name, MethodDefinition newWriterFunc)
        {
            lists.writeFuncs[name] = newWriterFunc;
            lists.generatedWriteFunctions.Add(newWriterFunc);

            ConfirmGeneratedCodeClass(scriptDef.MainModule);
            lists.generateContainerClass.Methods.Add(newWriterFunc);
        }

        public static MethodReference GetReadByReferenceFunc(TypeReference variable)
        {
            if (lists.readByReferenceFuncs.ContainsKey(variable.FullName))
            {
                return lists.readByReferenceFuncs[variable.FullName];
            }
            return null;
        }

        public static MethodReference GetReadFunc(TypeReference variable)
        {
            if (lists.readFuncs.ContainsKey(variable.FullName))
            {
                var foundFunc = lists.readFuncs[variable.FullName];
                if (foundFunc.ReturnType.IsArray == variable.IsArray)
                {
                    return foundFunc;
                }
            }

            var td = variable.Resolve();
            if (td == null)
            {
                Log.Error("GetReadFunc unsupported type " + variable.FullName);
                return null;
            }

            if (variable.IsByReference)
            {
                // error??
                Log.Error("GetReadFunc variable.IsByReference error.");
                return null;
            }

            MethodDefinition newReaderFunc;

            if (variable.IsArray)
            {
                var elementType = variable.GetElementType();
                var elementReadFunc = GetReadFunc(elementType);
                if (elementReadFunc == null)
                {
                    return null;
                }
                newReaderFunc = GenerateArrayReadFunc(variable, elementReadFunc);
            }
            else
            {
                if (td.IsEnum)
                {
                    return NetworkReaderReadInt32;
                }

                newReaderFunc = GenerateReadFunction(variable);
            }

            if (newReaderFunc == null)
            {
                Log.Error("GetReadFunc unable to generate function for:" + variable.FullName);
                return null;
            }
            RegisterReadFunc(variable.FullName, newReaderFunc);
            return newReaderFunc;
        }

        static public void RegisterReadByReferenceFunc(string name, MethodDefinition newReaderFunc)
        {
            lists.readByReferenceFuncs[name] = newReaderFunc;
            lists.generatedReadFunctions.Add(newReaderFunc);

            ConfirmGeneratedCodeClass(scriptDef.MainModule);
            lists.generateContainerClass.Methods.Add(newReaderFunc);
        }

        static public void RegisterReadFunc(string name, MethodDefinition newReaderFunc)
        {
            lists.readFuncs[name] = newReaderFunc;
            lists.generatedReadFunctions.Add(newReaderFunc);

            ConfirmGeneratedCodeClass(scriptDef.MainModule);
            lists.generateContainerClass.Methods.Add(newReaderFunc);
        }

        static MethodDefinition GenerateArrayReadFunc(TypeReference variable, MethodReference elementReadFunc)
        {
            if (!IsArrayType(variable))
            {
                Log.Error(variable.FullName + " is an unsupported array type. Jagged and multidimensional arrays are not supported");
                return null;
            }
            var functionName = "_ReadArray" + variable.GetElementType().Name + "_";
            if (variable.DeclaringType != null)
            {
                functionName += variable.DeclaringType.Name;
            }
            else
            {
                functionName += "None";
            }

            // create new reader for this type
            MethodDefinition readerFunc = new MethodDefinition(functionName,
                    MethodAttributes.Public |
                    MethodAttributes.Static |
                    MethodAttributes.HideBySig,
                    variable);

            readerFunc.Parameters.Add(new ParameterDefinition("reader", ParameterAttributes.None, scriptDef.MainModule.ImportReference(NetworkReaderType)));

            readerFunc.Body.Variables.Add(new VariableDefinition(int32Type));
            readerFunc.Body.Variables.Add(new VariableDefinition(variable));
            readerFunc.Body.Variables.Add(new VariableDefinition(int32Type));
            readerFunc.Body.InitLocals = true;

            ILProcessor worker = readerFunc.Body.GetILProcessor();

            worker.Append(worker.Create(OpCodes.Ldarg_0));
            worker.Append(worker.Create(OpCodes.Call, NetworkReadUInt16));
            worker.Append(worker.Create(OpCodes.Stloc_0));
            worker.Append(worker.Create(OpCodes.Ldloc_0));

            Instruction labelEmptyArray = worker.Create(OpCodes.Nop);
            worker.Append(worker.Create(OpCodes.Brtrue, labelEmptyArray));

            // return empty array
            worker.Append(worker.Create(OpCodes.Ldc_I4_0));
            worker.Append(worker.Create(OpCodes.Newarr, variable.GetElementType()));
            worker.Append(worker.Create(OpCodes.Ret));

            // create the actual array
            worker.Append(labelEmptyArray);
            worker.Append(worker.Create(OpCodes.Ldloc_0));
            worker.Append(worker.Create(OpCodes.Newarr, variable.GetElementType()));
            worker.Append(worker.Create(OpCodes.Stloc_1));
            worker.Append(worker.Create(OpCodes.Ldc_I4_0));
            worker.Append(worker.Create(OpCodes.Stloc_2));

            // loop start
            Instruction labelHead = worker.Create(OpCodes.Nop);
            worker.Append(worker.Create(OpCodes.Br, labelHead));

            // loop body
            Instruction labelBody = worker.Create(OpCodes.Nop);
            worker.Append(labelBody);
            worker.Append(worker.Create(OpCodes.Ldloc_1));
            worker.Append(worker.Create(OpCodes.Ldloc_2));
            worker.Append(worker.Create(OpCodes.Ldelema, variable.GetElementType()));
            worker.Append(worker.Create(OpCodes.Ldarg_0));
            worker.Append(worker.Create(OpCodes.Call, elementReadFunc));
            worker.Append(worker.Create(OpCodes.Stobj, variable.GetElementType()));
            worker.Append(worker.Create(OpCodes.Ldloc_2));
            worker.Append(worker.Create(OpCodes.Ldc_I4_1));
            worker.Append(worker.Create(OpCodes.Add));
            worker.Append(worker.Create(OpCodes.Stloc_2));

            // loop while check
            worker.Append(labelHead);
            worker.Append(worker.Create(OpCodes.Ldloc_2));
            worker.Append(worker.Create(OpCodes.Ldloc_0));
            worker.Append(worker.Create(OpCodes.Blt, labelBody));

            worker.Append(worker.Create(OpCodes.Ldloc_1));
            worker.Append(worker.Create(OpCodes.Ret));
            return readerFunc;
        }

        static MethodDefinition GenerateArrayWriteFunc(TypeReference variable, MethodReference elementWriteFunc)
        {
            if (!IsArrayType(variable))
            {
                Log.Error(variable.FullName + " is an unsupported array type. Jagged and multidimensional arrays are not supported");
                return null;
            }
            var functionName = "_WriteArray" + variable.GetElementType().Name + "_";
            if (variable.DeclaringType != null)
            {
                functionName += variable.DeclaringType.Name;
            }
            else
            {
                functionName += "None";
            }

            // create new writer for this type
            MethodDefinition writerFunc = new MethodDefinition(functionName,
                    MethodAttributes.Public |
                    MethodAttributes.Static |
                    MethodAttributes.HideBySig,
                    voidType);

            writerFunc.Parameters.Add(new ParameterDefinition("writer", ParameterAttributes.None, scriptDef.MainModule.ImportReference(NetworkWriterType)));
            writerFunc.Parameters.Add(new ParameterDefinition("value", ParameterAttributes.None, scriptDef.MainModule.ImportReference(variable)));

            writerFunc.Body.Variables.Add(new VariableDefinition(uint16Type));
            writerFunc.Body.Variables.Add(new VariableDefinition(uint16Type));
            writerFunc.Body.InitLocals = true;

            ILProcessor worker = writerFunc.Body.GetILProcessor();

            // null check
            Instruction labelNull = worker.Create(OpCodes.Nop);
            worker.Append(worker.Create(OpCodes.Ldarg_1));
            worker.Append(worker.Create(OpCodes.Brtrue, labelNull));

            worker.Append(worker.Create(OpCodes.Ldarg_0));
            worker.Append(worker.Create(OpCodes.Ldc_I4_0));
            worker.Append(worker.Create(OpCodes.Call, NetworkWriteUInt16));
            worker.Append(worker.Create(OpCodes.Ret));

            // setup array length local variable
            worker.Append(labelNull);
            worker.Append(worker.Create(OpCodes.Ldarg_1));
            worker.Append(worker.Create(OpCodes.Ldlen));
            worker.Append(worker.Create(OpCodes.Conv_I4));
            worker.Append(worker.Create(OpCodes.Conv_U2));
            worker.Append(worker.Create(OpCodes.Stloc_0));

            //write length
            worker.Append(worker.Create(OpCodes.Ldarg_0));
            worker.Append(worker.Create(OpCodes.Ldloc_0));
            worker.Append(worker.Create(OpCodes.Call, NetworkWriteUInt16));

            // start loop
            worker.Append(worker.Create(OpCodes.Ldc_I4_0));
            worker.Append(worker.Create(OpCodes.Stloc_1));
            Instruction labelHead = worker.Create(OpCodes.Nop);
            worker.Append(worker.Create(OpCodes.Br, labelHead));

            // loop body
            Instruction labelBody = worker.Create(OpCodes.Nop);
            worker.Append(labelBody);
            worker.Append(worker.Create(OpCodes.Ldarg_0));
            worker.Append(worker.Create(OpCodes.Ldarg_1));
            worker.Append(worker.Create(OpCodes.Ldloc_1));
            worker.Append(worker.Create(OpCodes.Ldelema, variable.GetElementType()));
            worker.Append(worker.Create(OpCodes.Ldobj, variable.GetElementType()));
            worker.Append(worker.Create(OpCodes.Call, elementWriteFunc));
            worker.Append(worker.Create(OpCodes.Ldloc_1));
            worker.Append(worker.Create(OpCodes.Ldc_I4_1));
            worker.Append(worker.Create(OpCodes.Add));
            worker.Append(worker.Create(OpCodes.Conv_U2));
            worker.Append(worker.Create(OpCodes.Stloc_1));

            // loop while check
            worker.Append(labelHead);
            worker.Append(worker.Create(OpCodes.Ldloc_1));
            worker.Append(worker.Create(OpCodes.Ldarg_1));
            worker.Append(worker.Create(OpCodes.Ldlen));
            worker.Append(worker.Create(OpCodes.Conv_I4));
            worker.Append(worker.Create(OpCodes.Blt, labelBody));

            worker.Append(worker.Create(OpCodes.Ret));
            return writerFunc;
        }

        static MethodDefinition GenerateWriterFunction(TypeReference variable)
        {
            if (!IsValidTypeToGenerate(variable.Resolve()))
            {
                return null;
            }

            var functionName = "_Write" + variable.Name + "_";
            if (variable.DeclaringType != null)
            {
                functionName += variable.DeclaringType.Name;
            }
            else
            {
                functionName += "None";
            }
            // create new writer for this type
            MethodDefinition writerFunc = new MethodDefinition(functionName,
                    MethodAttributes.Public |
                    MethodAttributes.Static |
                    MethodAttributes.HideBySig,
                    voidType);

            writerFunc.Parameters.Add(new ParameterDefinition("writer", ParameterAttributes.None, scriptDef.MainModule.ImportReference(NetworkWriterType)));
            writerFunc.Parameters.Add(new ParameterDefinition("value", ParameterAttributes.None, scriptDef.MainModule.ImportReference(variable)));

            ILProcessor worker = writerFunc.Body.GetILProcessor();

            uint fields = 0;
            foreach (var field in variable.Resolve().Fields)
            {
                if (field.IsStatic || field.IsPrivate)
                    continue;

                if (field.FieldType.Resolve().HasGenericParameters)
                {
                    Weaver.fail = true;
                    Log.Error("WriteReadFunc for " + field.Name + " [" + field.FieldType + "/" + field.FieldType.FullName + "]. Cannot have generic parameters.");
                    return null;
                }

                if (field.FieldType.Resolve().IsInterface)
                {
                    Weaver.fail = true;
                    Log.Error("WriteReadFunc for " + field.Name + " [" + field.FieldType + "/" + field.FieldType.FullName + "]. Cannot be an interface.");
                    return null;
                }

                var writeFunc = GetWriteFunc(field.FieldType);
                if (writeFunc != null)
                {
                    fields++;
                    worker.Append(worker.Create(OpCodes.Ldarg_0));
                    worker.Append(worker.Create(OpCodes.Ldarg_1));
                    worker.Append(worker.Create(OpCodes.Ldfld, field));
                    worker.Append(worker.Create(OpCodes.Call, writeFunc));
                }
                else
                {
                    Log.Error("WriteReadFunc for " + field.Name + " type " + field.FieldType + " no supported");
                    fail = true;
                    return null;
                }
            }
            if (fields == 0)
            {
                Log.Warning("The class / struct " + variable.Name + " has no public or non-static fields to serialize");
            }
            worker.Append(worker.Create(OpCodes.Ret));
            return writerFunc;
        }

        static MethodDefinition GenerateReadFunction(TypeReference variable)
        {
            if (!IsValidTypeToGenerate(variable.Resolve()))
            {
                return null;
            }

            var functionName = "_Read" + variable.Name + "_";
            if (variable.DeclaringType != null)
            {
                functionName += variable.DeclaringType.Name;
            }
            else
            {
                functionName += "None";
            }

            // create new reader for this type
            MethodDefinition readerFunc = new MethodDefinition(functionName,
                    MethodAttributes.Public |
                    MethodAttributes.Static |
                    MethodAttributes.HideBySig,
                    variable);

            // create local for return value
            readerFunc.Body.Variables.Add(new VariableDefinition(variable));
            readerFunc.Body.InitLocals = true;

            readerFunc.Parameters.Add(new ParameterDefinition("reader", ParameterAttributes.None, scriptDef.MainModule.ImportReference(NetworkReaderType)));

            ILProcessor worker = readerFunc.Body.GetILProcessor();

            if (variable.IsValueType)
            {
                // structs are created with Initobj
                worker.Append(worker.Create(OpCodes.Ldloca, 0));
                worker.Append(worker.Create(OpCodes.Initobj, variable));
            }
            else
            {
                // classes are created with their constructor

                var ctor = ResolveDefaultPublicCtor(variable);
                if (ctor == null)
                {
                    Log.Error("The class " + variable.Name + " has no default constructor or it's private, aborting.");
                    return null;
                }

                worker.Append(worker.Create(OpCodes.Newobj, ctor));
                worker.Append(worker.Create(OpCodes.Stloc_0));
            }

            uint fields = 0;
            foreach (var field in variable.Resolve().Fields)
            {
                if (field.IsStatic || field.IsPrivate)
                    continue;

                // mismatched ldloca/ldloc for struct/class combinations is invalid IL, which causes crash at runtime
                if (variable.IsValueType)
                {
                    worker.Append(worker.Create(OpCodes.Ldloca, 0));
                }
                else
                {
                    worker.Append(worker.Create(OpCodes.Ldloc, 0));
                }

                var readFunc = GetReadFunc(field.FieldType);
                if (readFunc != null)
                {
                    worker.Append(worker.Create(OpCodes.Ldarg_0));
                    worker.Append(worker.Create(OpCodes.Call, readFunc));
                }
                else
                {
                    Log.Error("GetReadFunc for " + field.Name + " type " + field.FieldType + " no supported");
                    fail = true;
                    return null;
                }

                worker.Append(worker.Create(OpCodes.Stfld, field));
                fields++;
            }
            if (fields == 0)
            {
                Log.Warning("The class / struct " + variable.Name + " has no public or non-static fields to serialize");
            }

            worker.Append(worker.Create(OpCodes.Ldloc_0));
            worker.Append(worker.Create(OpCodes.Ret));
            return readerFunc;
        }

        static Instruction GetEventLoadInstruction(ModuleDefinition moduleDef, TypeDefinition td, MethodDefinition md, int iCount, FieldReference foundEventField)
        {
            // go backwards until find a ldfld instruction for this event field
            while (iCount > 0)
            {
                iCount -= 1;
                Instruction inst = md.Body.Instructions[iCount];
                if (inst.OpCode == OpCodes.Ldfld)
                {
                    if (inst.Operand == foundEventField)
                    {
                        DLog(td, "    " + inst.Operand);
                        return inst;
                    }
                }
            }
            return null;
        }

        static void ProcessInstructionMethod(ModuleDefinition moduleDef, TypeDefinition td, MethodDefinition md, Instruction instr, MethodReference opMethodRef, int iCount)
        {
            //DLog(td, "ProcessInstructionMethod " + opMethod.Name);
            if (opMethodRef.Name == "Invoke")
            {
                // Events use an "Invoke" method to call the delegate.
                // this code replaces the "Invoke" instruction with the generated "Call***" instruction which send the event to the server.
                // but the "Invoke" instruction is called on the event field - where the "call" instruction is not.
                // so the earlier instruction that loads the event field is replaced with a Noop.

                // go backwards until find a ldfld instruction that matches ANY event
                bool found = false;
                while (iCount > 0 && !found)
                {
                    iCount -= 1;
                    Instruction inst = md.Body.Instructions[iCount];
                    if (inst.OpCode == OpCodes.Ldfld)
                    {
                        var opField = inst.Operand as FieldReference;

                        // find replaceEvent with matching name
                        for (int n = 0; n < lists.replacedEvents.Count; n++)
                        {
                            EventDefinition foundEvent = lists.replacedEvents[n];
                            if (foundEvent.Name == opField.Name)
                            {
                                instr.Operand = lists.replacementEvents[n];
                                inst.OpCode = OpCodes.Nop;
                                found = true;
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                if (lists.replacementMethodNames.Contains(opMethodRef.FullName))
                {
                    for (int n = 0; n < lists.replacedMethods.Count; n++)
                    {
                        MethodDefinition foundMethod = lists.replacedMethods[n];
                        if (opMethodRef.FullName == foundMethod.FullName)
                        {
                            //DLog(td, "    replacing "  + md.Name + ":" + i);
                            instr.Operand = lists.replacementMethods[n];
                            //DLog(td, "    replaced  "  + md.Name + ":" + i);
                            break;
                        }
                    }
                }
            }
        }

        static void ConfirmGeneratedCodeClass(ModuleDefinition moduleDef)
        {
            if (lists.generateContainerClass == null)
            {
                lists.generateContainerClass = new TypeDefinition("Unity", "GeneratedNetworkCode",
                        TypeAttributes.BeforeFieldInit | TypeAttributes.Class | TypeAttributes.AnsiClass | TypeAttributes.Public | TypeAttributes.AutoClass,
                        objectType);

                const MethodAttributes methodAttributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
                var method = new MethodDefinition(".ctor", methodAttributes, voidType);
                method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
                method.Body.Instructions.Add(Instruction.Create(OpCodes.Call, ResolveMethod(objectType, ".ctor")));
                method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

                lists.generateContainerClass.Methods.Add(method);
            }
        }

        static void ProcessInstructionField(TypeDefinition td, MethodDefinition md, Instruction i, FieldDefinition opField)
        {
            // dont replace property call sites in constructors or deserialize
            if (md.Name == ".ctor" || md.Name == "OnDeserialize")
                return;

            // does it set a field that we replaced?
            for (int n = 0; n < lists.replacedFields.Count; n++)
            {
                FieldDefinition fd = lists.replacedFields[n];
                if (opField == fd)
                {
                    //replace with property
                    //DLog(td, "    replacing "  + md.Name + ":" + i);
                    i.OpCode = OpCodes.Call;
                    i.Operand = lists.replacementProperties[n];
                    //DLog(td, "    replaced  "  + md.Name + ":" + i);
                    break;
                }
            }
        }

        static void ProcessInstruction(ModuleDefinition moduleDef, TypeDefinition td, MethodDefinition md, Instruction i, int iCount)
        {
            if (i.OpCode == OpCodes.Call || i.OpCode == OpCodes.Callvirt)
            {
                MethodReference opMethod = i.Operand as MethodReference;
                if (opMethod != null)
                {
                    ProcessInstructionMethod(moduleDef, td, md, i, opMethod, iCount);
                }
            }

            if (i.OpCode == OpCodes.Stfld)
            {
                // this instruction sets the value of a field. cache the field reference.
                FieldDefinition opField = i.Operand as FieldDefinition;
                if (opField != null)
                {
                    ProcessInstructionField(td, md, i, opField);
                }
            }
        }

        // this is required to early-out from a function with "ref" or "out" parameters
        static void InjectGuardParameters(MethodDefinition md, ILProcessor worker, Instruction top)
        {
            int offset = md.Resolve().IsStatic ? 0 : 1;
            for (int index = 0; index < md.Parameters.Count; index++)
            {
                var param = md.Parameters[index];
                if (param.IsOut)
                {
                    var elementType = param.ParameterType.GetElementType();
                    if (elementType.IsPrimitive)
                    {
                        worker.InsertBefore(top, worker.Create(OpCodes.Ldarg, index + offset));
                        worker.InsertBefore(top, worker.Create(OpCodes.Ldc_I4_0));
                        worker.InsertBefore(top, worker.Create(OpCodes.Stind_I4));
                    }
                    else
                    {
                        md.Body.Variables.Add(new VariableDefinition(elementType));
                        md.Body.InitLocals = true;

                        worker.InsertBefore(top, worker.Create(OpCodes.Ldarg, index + offset));
                        worker.InsertBefore(top, worker.Create(OpCodes.Ldloca_S, (byte)(md.Body.Variables.Count - 1)));
                        worker.InsertBefore(top, worker.Create(OpCodes.Initobj, elementType));
                        worker.InsertBefore(top, worker.Create(OpCodes.Ldloc, md.Body.Variables.Count - 1));
                        worker.InsertBefore(top, worker.Create(OpCodes.Stobj, elementType));
                    }
                }
            }
        }

        // this is required to early-out from a function with a return value.
        static void InjectGuardReturnValue(MethodDefinition md, ILProcessor worker, Instruction top)
        {
            if (md.ReturnType.FullName != voidType.FullName)
            {
                if (md.ReturnType.IsPrimitive)
                {
                    worker.InsertBefore(top, worker.Create(OpCodes.Ldc_I4_0));
                }
                else
                {
                    md.Body.Variables.Add(new VariableDefinition(md.ReturnType));
                    md.Body.InitLocals = true;

                    worker.InsertBefore(top, worker.Create(OpCodes.Ldloca_S, (byte)(md.Body.Variables.Count - 1)));
                    worker.InsertBefore(top, worker.Create(OpCodes.Initobj, md.ReturnType));
                    worker.InsertBefore(top, worker.Create(OpCodes.Ldloc, md.Body.Variables.Count - 1));
                }
            }
        }

        static void InjectServerGuard(ModuleDefinition moduleDef, TypeDefinition td, MethodDefinition md, bool logWarning)
        {
            if (!IsNetworkBehaviour(td))
            {
                Log.Error("[Server] guard on non-NetworkBehaviour script at [" + md.FullName + "]");
                return;
            }
            ILProcessor worker = md.Body.GetILProcessor();
            Instruction top = md.Body.Instructions[0];

            worker.InsertBefore(top, worker.Create(OpCodes.Call, NetworkServerGetActive));
            worker.InsertBefore(top, worker.Create(OpCodes.Brtrue, top));
            if (logWarning)
            {
                worker.InsertBefore(top, worker.Create(OpCodes.Ldstr, "[Server] function '" + md.FullName + "' called on client"));
                worker.InsertBefore(top, worker.Create(OpCodes.Call, logWarningReference));
            }
            InjectGuardParameters(md, worker, top);
            InjectGuardReturnValue(md, worker, top);
            worker.InsertBefore(top, worker.Create(OpCodes.Ret));
        }

        static void InjectClientGuard(ModuleDefinition moduleDef, TypeDefinition td, MethodDefinition md, bool logWarning)
        {
            if (!IsNetworkBehaviour(td))
            {
                Log.Error("[Client] guard on non-NetworkBehaviour script at [" + md.FullName + "]");
                return;
            }
            ILProcessor worker = md.Body.GetILProcessor();
            Instruction top = md.Body.Instructions[0];

            worker.InsertBefore(top, worker.Create(OpCodes.Call, NetworkClientGetActive));
            worker.InsertBefore(top, worker.Create(OpCodes.Brtrue, top));
            if (logWarning)
            {
                worker.InsertBefore(top, worker.Create(OpCodes.Ldstr, "[Client] function '" + md.FullName + "' called on server"));
                worker.InsertBefore(top, worker.Create(OpCodes.Call, logWarningReference));
            }

            InjectGuardParameters(md, worker, top);
            InjectGuardReturnValue(md, worker, top);
            worker.InsertBefore(top, worker.Create(OpCodes.Ret));
        }

        static void ProcessSiteMethod(ModuleDefinition moduleDef, TypeDefinition td, MethodDefinition md)
        {
            // process all references to replaced members with properties
            //Weaver.DLog(td, "      ProcessSiteMethod " + md);

            if (md.Name == ".cctor" || md.Name == "OnUnserializeVars")
                return;

            string prefix = md.Name.Substring(0, Math.Min(md.Name.Length, 4));

            if (prefix == "UNet")
                return;

            prefix = md.Name.Substring(0, Math.Min(md.Name.Length, 7));
            if (prefix == "CallCmd")
                return;

            prefix = md.Name.Substring(0, Math.Min(md.Name.Length, 9));
            if (prefix == "InvokeCmd" || prefix == "InvokeRpc" || prefix == "InvokeSyn")
                return;

            if (md.Body != null && md.Body.Instructions != null)
            {
                foreach (CustomAttribute attr in md.CustomAttributes)
                {
                    if (attr.Constructor.DeclaringType.ToString() == "UnityEngine.Networking.ServerAttribute")
                    {
                        InjectServerGuard(moduleDef, td, md, true);
                    }
                    else if (attr.Constructor.DeclaringType.ToString() == "UnityEngine.Networking.ServerCallbackAttribute")
                    {
                        InjectServerGuard(moduleDef, td, md, false);
                    }
                    else if (attr.Constructor.DeclaringType.ToString() == "UnityEngine.Networking.ClientAttribute")
                    {
                        InjectClientGuard(moduleDef, td, md, true);
                    }
                    else if (attr.Constructor.DeclaringType.ToString() == "UnityEngine.Networking.ClientCallbackAttribute")
                    {
                        InjectClientGuard(moduleDef, td, md, false);
                    }
                }

                int iCount = 0;
                foreach (Instruction i in md.Body.Instructions)
                {
                    ProcessInstruction(moduleDef, td, md, i, iCount);
                    iCount += 1;
                }
            }
        }

        static void ProcessSiteClass(ModuleDefinition moduleDef, TypeDefinition td)
        {
            //Console.WriteLine("    ProcessSiteClass " + td);
            foreach (MethodDefinition md in td.Methods)
            {
                ProcessSiteMethod(moduleDef, td, md);
            }

            foreach (var nested in td.NestedTypes)
            {
                ProcessSiteClass(moduleDef, nested);
            }
        }

        static void ProcessSitesModule(ModuleDefinition moduleDef)
        {
            var startTime = System.DateTime.Now;

            //Search through the types
            foreach (TypeDefinition td in moduleDef.Types)
            {
                if (td.IsClass)
                {
                    ProcessSiteClass(moduleDef, td);
                }
            }
            if (lists.generateContainerClass != null)
            {
                moduleDef.Types.Add(lists.generateContainerClass);
                scriptDef.MainModule.ImportReference(lists.generateContainerClass);

                foreach (var f in lists.generatedReadFunctions)
                {
                    scriptDef.MainModule.ImportReference(f);
                }

                foreach (var f in lists.generatedWriteFunctions)
                {
                    scriptDef.MainModule.ImportReference(f);
                }
            }
            Console.WriteLine("  ProcessSitesModule " + moduleDef.Name + " elapsed time:" + (System.DateTime.Now - startTime));
        }

        static void ProcessPropertySites()
        {
            ProcessSitesModule(scriptDef.MainModule);
        }

        static bool ProcessMessageType(TypeDefinition td)
        {
            var proc = new MessageClassProcessor(td);
            proc.Process();
            return true;
        }

        static bool ProcessSyncListStructType(TypeDefinition td)
        {
            var proc = new SyncListStructProcessor(td);
            proc.Process();
            return true;
        }

        static void ProcessMonoBehaviourType(TypeDefinition td)
        {
            var proc = new MonoBehaviourProcessor(td);
            proc.Process();
        }

        static bool ProcessNetworkBehaviourType(TypeDefinition td)
        {
            foreach (var md in td.Resolve().Methods)
            {
                if (md.Name == "UNetVersion")
                {
                    DLog(td, " Already processed");
                    return false; // did no work
                }
            }
            DLog(td, "Found NetworkBehaviour " + td.FullName);

            NetworkBehaviourProcessor proc = new NetworkBehaviourProcessor(td);
            proc.Process();
            return true;
        }

        public static MethodReference ResolveMethod(TypeReference t, string name)
        {
            //Console.WriteLine("ResolveMethod " + t.ToString () + " " + name);
            if (t == null)
            {
                Log.Error("Type missing for " + name);
                fail = true;
                return null;
            }
            foreach (var methodRef in t.Resolve().Methods)
            {
                if (methodRef.Name == name)
                {
                    return scriptDef.MainModule.ImportReference(methodRef);
                }
            }
            Log.Error("ResolveMethod failed " + t.Name + "::" + name + " " + t.Resolve());

            // why did it fail!?
            foreach (var methodRef in t.Resolve().Methods)
            {
                Log.Error("Method " + methodRef.Name);
            }

            fail = true;
            return null;
        }

        static MethodReference ResolveMethodWithArg(TypeReference t, string name, TypeReference argType)
        {
            foreach (var methodRef in t.Resolve().Methods)
            {
                if (methodRef.Name == name)
                {
                    if (methodRef.Parameters.Count == 1)
                    {
                        if (methodRef.Parameters[0].ParameterType.FullName == argType.FullName)
                        {
                            return scriptDef.MainModule.ImportReference(methodRef);
                        }
                    }
                }
            }
            Log.Error("ResolveMethodWithArg failed " + t.Name + "::" + name + " " + argType);
            fail = true;
            return null;
        }

        static MethodDefinition ResolveDefaultPublicCtor(TypeReference variable)
        {
            foreach (MethodDefinition methodRef in variable.Resolve().Methods)
            {
                if (methodRef.Name == ".ctor" &&
                    methodRef.Resolve().IsPublic &&
                    methodRef.Parameters.Count == 0)
                {
                    return methodRef;
                }
            }
            return null;
        }

        static GenericInstanceMethod ResolveMethodGeneric(TypeReference t, string name, TypeReference genericType)
        {
            foreach (var methodRef in t.Resolve().Methods)
            {
                if (methodRef.Name == name)
                {
                    if (methodRef.Parameters.Count == 0)
                    {
                        if (methodRef.GenericParameters.Count == 1)
                        {
                            MethodReference tmp = scriptDef.MainModule.ImportReference(methodRef);
                            GenericInstanceMethod gm = new GenericInstanceMethod(tmp);
                            gm.GenericArguments.Add(genericType);
                            if (gm.GenericArguments[0].FullName == genericType.FullName)
                            {
                                return gm;
                            }
                        }
                    }
                }
            }

            Log.Error("ResolveMethodGeneric failed " + t.Name + "::" + name + " " + genericType);
            fail = true;
            return null;
        }

        public static FieldReference ResolveField(TypeReference t, string name)
        {
            foreach (FieldDefinition fd in t.Resolve().Fields)
            {
                if (fd.Name == name)
                {
                    return scriptDef.MainModule.ImportReference(fd);
                }
            }
            return null;
        }

        public static MethodReference ResolveProperty(TypeReference t, string name)
        {
            foreach (var fd in t.Resolve().Properties)
            {
                if (fd.Name == name)
                {
                    return scriptDef.MainModule.ImportReference(fd.GetMethod);
                }
            }
            return null;
        }

        static void SetupUnityTypes()
        {
            vector2Type = m_UnityAssemblyDefinition.MainModule.GetType("UnityEngine.Vector2");
            vector3Type = m_UnityAssemblyDefinition.MainModule.GetType("UnityEngine.Vector3");
            vector4Type = m_UnityAssemblyDefinition.MainModule.GetType("UnityEngine.Vector4");
            colorType = m_UnityAssemblyDefinition.MainModule.GetType("UnityEngine.Color");
            color32Type = m_UnityAssemblyDefinition.MainModule.GetType("UnityEngine.Color32");
            quaternionType = m_UnityAssemblyDefinition.MainModule.GetType("UnityEngine.Quaternion");
            rectType = m_UnityAssemblyDefinition.MainModule.GetType("UnityEngine.Rect");
            planeType = m_UnityAssemblyDefinition.MainModule.GetType("UnityEngine.Plane");
            rayType = m_UnityAssemblyDefinition.MainModule.GetType("UnityEngine.Ray");
            matrixType = m_UnityAssemblyDefinition.MainModule.GetType("UnityEngine.Matrix4x4");
            gameObjectType = m_UnityAssemblyDefinition.MainModule.GetType("UnityEngine.GameObject");
            transformType = m_UnityAssemblyDefinition.MainModule.GetType("UnityEngine.Transform");
            unityObjectType = m_UnityAssemblyDefinition.MainModule.GetType("UnityEngine.Object");

            hashType = m_UNetAssemblyDefinition.MainModule.GetType("UnityEngine.Networking.NetworkHash128");
            NetworkClientType = m_UNetAssemblyDefinition.MainModule.GetType("UnityEngine.Networking.NetworkClient");
            NetworkServerType = m_UNetAssemblyDefinition.MainModule.GetType("UnityEngine.Networking.NetworkServer");
            NetworkCRCType = m_UNetAssemblyDefinition.MainModule.GetType("UnityEngine.Networking.NetworkCRC");

            SyncVarType = m_UNetAssemblyDefinition.MainModule.GetType("UnityEngine.Networking.SyncVarAttribute");
            CommandType = m_UNetAssemblyDefinition.MainModule.GetType("UnityEngine.Networking.CommandAttribute");
            ClientRpcType = m_UNetAssemblyDefinition.MainModule.GetType("UnityEngine.Networking.ClientRpcAttribute");
            TargetRpcType = m_UNetAssemblyDefinition.MainModule.GetType("UnityEngine.Networking.TargetRpcAttribute");
            SyncEventType = m_UNetAssemblyDefinition.MainModule.GetType("UnityEngine.Networking.SyncEventAttribute");
            SyncListType = m_UNetAssemblyDefinition.MainModule.GetType("UnityEngine.Networking.SyncList`1");
            NetworkSettingsType = m_UNetAssemblyDefinition.MainModule.GetType("UnityEngine.Networking.NetworkSettingsAttribute");

            SyncListFloatType = m_UNetAssemblyDefinition.MainModule.GetType("UnityEngine.Networking.SyncListFloat");
            SyncListIntType = m_UNetAssemblyDefinition.MainModule.GetType("UnityEngine.Networking.SyncListInt");
            SyncListUIntType = m_UNetAssemblyDefinition.MainModule.GetType("UnityEngine.Networking.SyncListUInt");
            SyncListBoolType = m_UNetAssemblyDefinition.MainModule.GetType("UnityEngine.Networking.SyncListBool");
            SyncListStringType = m_UNetAssemblyDefinition.MainModule.GetType("UnityEngine.Networking.SyncListString");
        }

        static void SetupCorLib()
        {
            var name = AssemblyNameReference.Parse("mscorlib");
            var parameters = new ReaderParameters
            {
                AssemblyResolver = scriptDef.MainModule.AssemblyResolver,
            };
            corLib = scriptDef.MainModule.AssemblyResolver.Resolve(name, parameters).MainModule;
        }

        static TypeReference ImportCorLibType(string fullName)
        {
            var type = corLib.GetType(fullName) ?? corLib.ExportedTypes.First(t => t.FullName == fullName).Resolve();
            return scriptDef.MainModule.ImportReference(type);
        }

        static void SetupTargetTypes()
        {
            // system types
            SetupCorLib();
            voidType = ImportCorLibType("System.Void");
            singleType = ImportCorLibType("System.Single");
            doubleType = ImportCorLibType("System.Double");
            decimalType = ImportCorLibType("System.Decimal");
            boolType = ImportCorLibType("System.Boolean");
            stringType = ImportCorLibType("System.String");
            int64Type = ImportCorLibType("System.Int64");
            uint64Type = ImportCorLibType("System.UInt64");
            int32Type = ImportCorLibType("System.Int32");
            uint32Type = ImportCorLibType("System.UInt32");
            int16Type = ImportCorLibType("System.Int16");
            uint16Type = ImportCorLibType("System.UInt16");
            byteType = ImportCorLibType("System.Byte");
            sbyteType = ImportCorLibType("System.SByte");
            charType = ImportCorLibType("System.Char");
            objectType = ImportCorLibType("System.Object");
            valueTypeType = ImportCorLibType("System.ValueType");
            typeType = ImportCorLibType("System.Type");
            IEnumeratorType = ImportCorLibType("System.Collections.IEnumerator");
            MemoryStreamType = ImportCorLibType("System.IO.MemoryStream");

            NetworkReaderType = m_UNetAssemblyDefinition.MainModule.GetType("UnityEngine.Networking.NetworkReader");
            NetworkReaderDef = NetworkReaderType.Resolve();

            NetworkReaderCtor = ResolveMethod(NetworkReaderDef, ".ctor");

            NetworkWriterType = m_UNetAssemblyDefinition.MainModule.GetType("UnityEngine.Networking.NetworkWriter");
            NetworkWriterDef  = NetworkWriterType.Resolve();

            NetworkWriterCtor = ResolveMethod(NetworkWriterDef, ".ctor");

            MemoryStreamCtor = ResolveMethod(MemoryStreamType, ".ctor");

            NetworkInstanceIdType = m_UNetAssemblyDefinition.MainModule.GetType("UnityEngine.Networking.NetworkInstanceId");
            NetworkSceneIdType = m_UNetAssemblyDefinition.MainModule.GetType("UnityEngine.Networking.NetworkSceneId");

            NetworkInstanceIdType = m_UNetAssemblyDefinition.MainModule.GetType("UnityEngine.Networking.NetworkInstanceId");
            NetworkSceneIdType = m_UNetAssemblyDefinition.MainModule.GetType("UnityEngine.Networking.NetworkSceneId");

            NetworkServerGetActive = ResolveMethod(NetworkServerType, "get_active");
            NetworkServerGetLocalClientActive = ResolveMethod(NetworkServerType, "get_localClientActive");
            NetworkClientGetActive = ResolveMethod(NetworkClientType, "get_active");

            NetworkReaderReadInt32 = ResolveMethod(NetworkReaderType, "ReadInt32");

            NetworkWriterWriteInt32 = ResolveMethodWithArg(NetworkWriterType, "Write", int32Type);
            NetworkWriterWriteInt16 = ResolveMethodWithArg(NetworkWriterType, "Write", int16Type);

            NetworkReaderReadPacked32 = ResolveMethod(NetworkReaderType, "ReadPackedUInt32");
            NetworkReaderReadPacked64 = ResolveMethod(NetworkReaderType, "ReadPackedUInt64");
            NetworkReaderReadByte = ResolveMethod(NetworkReaderType, "ReadByte");

            NetworkWriterWritePacked32 = ResolveMethod(NetworkWriterType, "WritePackedUInt32");
            NetworkWriterWritePacked64 = ResolveMethod(NetworkWriterType, "WritePackedUInt64");

            NetworkWriterWriteNetworkInstanceId = ResolveMethodWithArg(NetworkWriterType, "Write", NetworkInstanceIdType);
            NetworkWriterWriteNetworkSceneId = ResolveMethodWithArg(NetworkWriterType, "Write", NetworkSceneIdType);

            NetworkReaderReadNetworkInstanceId = ResolveMethod(NetworkReaderType, "ReadNetworkId");
            NetworkReaderReadNetworkSceneId = ResolveMethod(NetworkReaderType, "ReadSceneId");
            NetworkInstanceIsEmpty = ResolveMethod(NetworkInstanceIdType, "IsEmpty");

            NetworkReadUInt16 = ResolveMethod(NetworkReaderType, "ReadUInt16");
            NetworkWriteUInt16 = ResolveMethodWithArg(NetworkWriterType, "Write", uint16Type);

            CmdDelegateReference = m_UNetAssemblyDefinition.MainModule.GetType("UnityEngine.Networking.NetworkBehaviour/CmdDelegate");
            CmdDelegateConstructor = ResolveMethod(CmdDelegateReference, ".ctor");
            scriptDef.MainModule.ImportReference(gameObjectType);
            scriptDef.MainModule.ImportReference(transformType);

            TypeReference unetViewTmp = m_UNetAssemblyDefinition.MainModule.GetType("UnityEngine.Networking.NetworkIdentity");
            NetworkIdentityType = scriptDef.MainModule.ImportReference(unetViewTmp);

            NetworkInstanceIdType = scriptDef.MainModule.ImportReference(NetworkInstanceIdType);

            SyncListFloatReadType = ResolveMethod(SyncListFloatType, "ReadReference");
            SyncListIntReadType = ResolveMethod(SyncListIntType, "ReadReference");
            SyncListUIntReadType = ResolveMethod(SyncListUIntType, "ReadReference");
            SyncListBoolReadType = ResolveMethod(SyncListBoolType, "ReadReference");
            SyncListStringReadType = ResolveMethod(SyncListStringType, "ReadReference");

            SyncListFloatWriteType = ResolveMethod(SyncListFloatType, "WriteInstance");
            SyncListIntWriteType = ResolveMethod(SyncListIntType, "WriteInstance");
            SyncListUIntWriteType = ResolveMethod(SyncListUIntType, "WriteInstance");
            SyncListBoolWriteType = ResolveMethod(SyncListBoolType, "WriteInstance");
            SyncListStringWriteType = ResolveMethod(SyncListStringType, "WriteInstance");


            NetworkBehaviourType = m_UNetAssemblyDefinition.MainModule.GetType("UnityEngine.Networking.NetworkBehaviour");
            NetworkBehaviourType2 = scriptDef.MainModule.ImportReference(NetworkBehaviourType);
            NetworkConnectionType = m_UNetAssemblyDefinition.MainModule.GetType("UnityEngine.Networking.NetworkConnection");

            MonoBehaviourType = m_UnityAssemblyDefinition.MainModule.GetType("UnityEngine.MonoBehaviour");
            ScriptableObjectType = m_UnityAssemblyDefinition.MainModule.GetType("UnityEngine.ScriptableObject");

            NetworkConnectionType = m_UNetAssemblyDefinition.MainModule.GetType("UnityEngine.Networking.NetworkConnection");
            NetworkConnectionType = scriptDef.MainModule.ImportReference(NetworkConnectionType);

            ULocalConnectionToServerType = m_UNetAssemblyDefinition.MainModule.GetType("UnityEngine.Networking.ULocalConnectionToServer");
            ULocalConnectionToServerType = scriptDef.MainModule.ImportReference(ULocalConnectionToServerType);

            ULocalConnectionToClientType = m_UNetAssemblyDefinition.MainModule.GetType("UnityEngine.Networking.ULocalConnectionToClient");
            ULocalConnectionToClientType = scriptDef.MainModule.ImportReference(ULocalConnectionToClientType);

            MessageBaseType = m_UNetAssemblyDefinition.MainModule.GetType("UnityEngine.Networking.MessageBase");
            SyncListStructType = m_UNetAssemblyDefinition.MainModule.GetType("UnityEngine.Networking.SyncListStruct`1");

            NetworkBehaviourDirtyBitsReference = ResolveProperty(NetworkBehaviourType, "syncVarDirtyBits");

            ComponentType = m_UnityAssemblyDefinition.MainModule.GetType("UnityEngine.Component");
            ClientSceneType = m_UNetAssemblyDefinition.MainModule.GetType("UnityEngine.Networking.ClientScene");
            FindLocalObjectReference = ResolveMethod(ClientSceneType, "FindLocalObject");
            RegisterBehaviourReference = ResolveMethod(NetworkCRCType, "RegisterBehaviour");
            ReadyConnectionReference = ResolveMethod(ClientSceneType, "get_readyConnection");

            // get specialized GetComponent<NetworkIdentity>()
            getComponentReference = ResolveMethodGeneric(ComponentType, "GetComponent", NetworkIdentityType);

            getUNetIdReference = ResolveMethod(unetViewTmp, "get_netId");

            gameObjectInequality = ResolveMethod(unityObjectType, "op_Inequality");

            UBehaviourIsServer  = ResolveMethod(NetworkBehaviourType, "get_isServer");
            getPlayerIdReference = ResolveMethod(NetworkBehaviourType, "get_playerControllerId");
            setSyncVarReference = ResolveMethod(NetworkBehaviourType, "SetSyncVar");
            setSyncVarHookGuard = ResolveMethod(NetworkBehaviourType, "set_syncVarHookGuard");
            getSyncVarHookGuard = ResolveMethod(NetworkBehaviourType, "get_syncVarHookGuard");

            setSyncVarGameObjectReference = ResolveMethod(NetworkBehaviourType, "SetSyncVarGameObject");
            registerCommandDelegateReference = ResolveMethod(NetworkBehaviourType, "RegisterCommandDelegate");
            registerRpcDelegateReference = ResolveMethod(NetworkBehaviourType, "RegisterRpcDelegate");
            registerEventDelegateReference = ResolveMethod(NetworkBehaviourType, "RegisterEventDelegate");
            registerSyncListDelegateReference = ResolveMethod(NetworkBehaviourType, "RegisterSyncListDelegate");
            getTypeReference = ResolveMethod(objectType, "GetType");
            getTypeFromHandleReference = ResolveMethod(typeType, "GetTypeFromHandle");
            logErrorReference = ResolveMethod(m_UnityAssemblyDefinition.MainModule.GetType("UnityEngine.Debug"), "LogError");
            logWarningReference = ResolveMethod(m_UnityAssemblyDefinition.MainModule.GetType("UnityEngine.Debug"), "LogWarning");
            sendCommandInternal = ResolveMethod(NetworkBehaviourType, "SendCommandInternal");
            sendRpcInternal = ResolveMethod(NetworkBehaviourType, "SendRPCInternal");
            sendTargetRpcInternal = ResolveMethod(NetworkBehaviourType, "SendTargetRPCInternal");
            sendEventInternal = ResolveMethod(NetworkBehaviourType, "SendEventInternal");

            SyncListType = scriptDef.MainModule.ImportReference(SyncListType);
            SyncListInitBehaviourReference = ResolveMethod(SyncListType, "InitializeBehaviour");
            SyncListInitHandleMsg = ResolveMethod(SyncListType, "HandleMsg");
            SyncListClear = ResolveMethod(SyncListType, "Clear");
        }

        static void SetupReadFunctions()
        {
            lists.readFuncs = new Dictionary<string, MethodReference>
            {
                { singleType.FullName, ResolveMethod(NetworkReaderType, "ReadSingle") },
                { doubleType.FullName, ResolveMethod(NetworkReaderType, "ReadDouble") },
                { boolType.FullName, ResolveMethod(NetworkReaderType, "ReadBoolean") },
                { stringType.FullName, ResolveMethod(NetworkReaderType, "ReadString") },
                { int64Type.FullName, NetworkReaderReadPacked64 },
                { uint64Type.FullName, NetworkReaderReadPacked64 },
                { int32Type.FullName, NetworkReaderReadPacked32 },
                { uint32Type.FullName, NetworkReaderReadPacked32 },
                { int16Type.FullName, NetworkReaderReadPacked32 },
                { uint16Type.FullName, NetworkReaderReadPacked32 },
                { byteType.FullName, NetworkReaderReadPacked32 },
                { sbyteType.FullName, NetworkReaderReadPacked32 },
                { charType.FullName, NetworkReaderReadPacked32 },
                { decimalType.FullName, ResolveMethod(NetworkReaderType, "ReadDecimal") },
                { vector2Type.FullName, ResolveMethod(NetworkReaderType, "ReadVector2") },
                { vector3Type.FullName, ResolveMethod(NetworkReaderType, "ReadVector3") },
                { vector4Type.FullName, ResolveMethod(NetworkReaderType, "ReadVector4") },
                { colorType.FullName, ResolveMethod(NetworkReaderType, "ReadColor") },
                { color32Type.FullName, ResolveMethod(NetworkReaderType, "ReadColor32") },
                { quaternionType.FullName, ResolveMethod(NetworkReaderType, "ReadQuaternion") },
                { rectType.FullName, ResolveMethod(NetworkReaderType, "ReadRect") },
                { planeType.FullName, ResolveMethod(NetworkReaderType, "ReadPlane") },
                { rayType.FullName, ResolveMethod(NetworkReaderType, "ReadRay") },
                { matrixType.FullName, ResolveMethod(NetworkReaderType, "ReadMatrix4x4") },
                { hashType.FullName, ResolveMethod(NetworkReaderType, "ReadNetworkHash128") },
                { gameObjectType.FullName, ResolveMethod(NetworkReaderType, "ReadGameObject") },
                { NetworkIdentityType.FullName, ResolveMethod(NetworkReaderType, "ReadNetworkIdentity") },
                { NetworkInstanceIdType.FullName, NetworkReaderReadNetworkInstanceId },
                { NetworkSceneIdType.FullName, NetworkReaderReadNetworkSceneId },
                { transformType.FullName, ResolveMethod(NetworkReaderType, "ReadTransform") },
                { "System.Byte[]", ResolveMethod(NetworkReaderType, "ReadBytesAndSize") },
            };

            lists.readByReferenceFuncs = new Dictionary<string, MethodReference>
            {
                {SyncListFloatType.FullName, SyncListFloatReadType},
                {SyncListIntType.FullName, SyncListIntReadType},
                {SyncListUIntType.FullName, SyncListUIntReadType},
                {SyncListBoolType.FullName, SyncListBoolReadType},
                {SyncListStringType.FullName, SyncListStringReadType}
            };
        }

        static void SetupWriteFunctions()
        {
            lists.writeFuncs = new Dictionary<string, MethodReference>
            {
                { singleType.FullName, ResolveMethodWithArg(NetworkWriterType, "Write", singleType) },
                { doubleType.FullName, ResolveMethodWithArg(NetworkWriterType, "Write", doubleType) },
                { boolType.FullName, ResolveMethodWithArg(NetworkWriterType, "Write", boolType) },
                { stringType.FullName, ResolveMethodWithArg(NetworkWriterType, "Write", stringType) },
                { int64Type.FullName, NetworkWriterWritePacked64 },
                { uint64Type.FullName, NetworkWriterWritePacked64 },
                { int32Type.FullName, NetworkWriterWritePacked32 },
                { uint32Type.FullName, NetworkWriterWritePacked32 },
                { int16Type.FullName, NetworkWriterWritePacked32 },
                { uint16Type.FullName, NetworkWriterWritePacked32 },
                { byteType.FullName, NetworkWriterWritePacked32 },
                { sbyteType.FullName, NetworkWriterWritePacked32 },
                { charType.FullName, NetworkWriterWritePacked32 },
                { decimalType.FullName, ResolveMethodWithArg(NetworkWriterType, "Write", decimalType) },
                { vector2Type.FullName, ResolveMethodWithArg(NetworkWriterType, "Write", vector2Type) },
                { vector3Type.FullName, ResolveMethodWithArg(NetworkWriterType, "Write", vector3Type) },
                { vector4Type.FullName, ResolveMethodWithArg(NetworkWriterType, "Write", vector4Type) },
                { colorType.FullName, ResolveMethodWithArg(NetworkWriterType, "Write", colorType) },
                { color32Type.FullName, ResolveMethodWithArg(NetworkWriterType, "Write", color32Type) },
                { quaternionType.FullName, ResolveMethodWithArg(NetworkWriterType, "Write", quaternionType) },
                { rectType.FullName, ResolveMethodWithArg(NetworkWriterType, "Write", rectType) },
                { planeType.FullName, ResolveMethodWithArg(NetworkWriterType, "Write", planeType) },
                { rayType.FullName, ResolveMethodWithArg(NetworkWriterType, "Write", rayType) },
                { matrixType.FullName, ResolveMethodWithArg(NetworkWriterType, "Write", matrixType) },
                { hashType.FullName, ResolveMethodWithArg(NetworkWriterType, "Write", hashType) },
                { gameObjectType.FullName, ResolveMethodWithArg(NetworkWriterType, "Write", gameObjectType) },
                { NetworkIdentityType.FullName, ResolveMethodWithArg(NetworkWriterType, "Write", NetworkIdentityType) },
                { NetworkInstanceIdType.FullName, NetworkWriterWriteNetworkInstanceId },
                { NetworkSceneIdType.FullName, NetworkWriterWriteNetworkSceneId },
                { transformType.FullName, ResolveMethodWithArg(NetworkWriterType, "Write", transformType) },
                { "System.Byte[]", ResolveMethod(NetworkWriterType, "WriteBytesFull") },
                { SyncListFloatType.FullName, SyncListFloatWriteType },
                { SyncListIntType.FullName, SyncListIntWriteType },
                { SyncListUIntType.FullName, SyncListUIntWriteType },
                { SyncListBoolType.FullName, SyncListBoolWriteType },
                { SyncListStringType.FullName, SyncListStringWriteType }
            };
        }

        static bool IsNetworkBehaviour(TypeDefinition td)
        {
            if (!td.IsClass)
                return false;

            // are ANY parent clasess unetbehaviours
            TypeReference parent = td.BaseType;
            while (parent != null)
            {
                if (parent.FullName == NetworkBehaviourType.FullName)
                {
                    return true;
                }
                try
                {
                    parent = parent.Resolve().BaseType;
                }
                catch (AssemblyResolutionException)
                {
                    // this can happen for pluins.
                    //Console.WriteLine("AssemblyResolutionException: "+ ex.ToString());
                    break;
                }
            }
            return false;
        }

        static public bool IsDerivedFrom(TypeDefinition td, TypeReference baseClass)
        {
            if (!td.IsClass)
                return false;

            // are ANY parent clasess unetbehaviours
            TypeReference parent = td.BaseType;
            while (parent != null)
            {
                var parentName = parent.FullName;

                // strip generic parameters
                int index = parentName.IndexOf('<');
                if (index != -1)
                {
                    parentName = parentName.Substring(0, index);
                }

                if (parentName == baseClass.FullName)
                {
                    return true;
                }
                try
                {
                    parent = parent.Resolve().BaseType;
                }
                catch (AssemblyResolutionException)
                {
                    // this can happen for pluins.
                    //Console.WriteLine("AssemblyResolutionException: "+ ex.ToString());
                    break;
                }
            }
            return false;
        }

        static public bool IsValidTypeToGenerate(TypeDefinition variable)
        {
            // a valid type is a simple class or struct. so we generate only code for types we dont know, and if they are not inside
            // this assembly it must mean that we are trying to serialize a variable outside our scope. and this will fail.

            string assembly = Weaver.scriptDef.MainModule.Name;
            if (variable.Module.Name != assembly)
            {
                Log.Error("parameter [" + variable.Name +
                    "] is of the type [" +
                    variable.FullName +
                    "] is not a valid type, please make sure to use a valid type.");
                Weaver.fail = true;
                fail = true;
                return false;
            }
            return true;
        }

        static void CheckMonoBehaviour(TypeDefinition td)
        {
            if (IsDerivedFrom(td, MonoBehaviourType))
            {
                ProcessMonoBehaviourType(td);
            }
        }

        static bool CheckNetworkBehaviour(TypeDefinition td)
        {
            if (!td.IsClass)
                return false;

            if (!IsNetworkBehaviour(td))
            {
                CheckMonoBehaviour(td);
                return false;
            }

            // process this and base classes from parent to child order

            List<TypeDefinition> behClasses = new List<TypeDefinition>();

            TypeDefinition parent = td;
            while (parent != null)
            {
                if (parent.FullName == NetworkBehaviourType.FullName)
                {
                    break;
                }
                try
                {
                    behClasses.Insert(0, parent);
                    parent = parent.BaseType.Resolve();
                }
                catch (AssemblyResolutionException)
                {
                    // this can happen for pluins.
                    //Console.WriteLine("AssemblyResolutionException: "+ ex.ToString());
                    break;
                }
            }

            bool didWork = false;
            foreach (var beh in behClasses)
            {
                didWork |= ProcessNetworkBehaviourType(beh);
            }
            return didWork;
        }

        static bool CheckMessageBase(TypeDefinition td)
        {
            if (!td.IsClass)
                return false;

            bool didWork = false;

            // are ANY parent clasess MessageBase
            TypeReference parent = td.BaseType;
            while (parent != null)
            {
                if (parent.FullName == MessageBaseType.FullName)
                {
                    didWork |= ProcessMessageType(td);
                    break;
                }
                try
                {
                    parent = parent.Resolve().BaseType;
                }
                catch (AssemblyResolutionException)
                {
                    // this can happen for pluins.
                    //Console.WriteLine("AssemblyResolutionException: "+ ex.ToString());
                    break;
                }
            }

            // check for embedded types
            foreach (var embedded in td.NestedTypes)
            {
                didWork |= CheckMessageBase(embedded);
            }

            return didWork;
        }

        static bool CheckSyncListStruct(TypeDefinition td)
        {
            if (!td.IsClass)
                return false;

            bool didWork = false;

            // are ANY parent clasess SyncListStruct
            TypeReference parent = td.BaseType;
            while (parent != null)
            {
                if (parent.FullName.Contains("SyncListStruct"))
                {
                    didWork |= ProcessSyncListStructType(td);
                    break;
                }
                try
                {
                    parent = parent.Resolve().BaseType;
                }
                catch (AssemblyResolutionException)
                {
                    // this can happen for pluins.
                    //Console.WriteLine("AssemblyResolutionException: "+ ex.ToString());
                    break;
                }
            }

            // check for embedded types
            foreach (var embedded in td.NestedTypes)
            {
                didWork |= CheckSyncListStruct(embedded);
            }

            return didWork;
        }

        static bool Weave(string assName, IEnumerable<string> dependencies, IAssemblyResolver assemblyResolver, string unityEngineDLLPath, string unityUNetDLLPath, string outputDir)
        {
            var readParams = Helpers.ReaderParameters(assName, dependencies, assemblyResolver, unityEngineDLLPath, unityUNetDLLPath);
            scriptDef = AssemblyDefinition.ReadAssembly(assName, readParams);

            SetupTargetTypes();
            SetupReadFunctions();
            SetupWriteFunctions();

            ModuleDefinition moduleDefinition = scriptDef.MainModule;
            Console.WriteLine("Script Module: {0}", moduleDefinition.Name);

            // Process each NetworkBehaviour
            bool didWork = false;

            // We need to do 2 passes, because SyncListStructs might be referenced from other modules, so we must make sure we generate them first.
            for (int pass = 0; pass < 2; pass++)
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                foreach (TypeDefinition td in moduleDefinition.Types)
                {
                    if (td.IsClass && CanBeResolved(td.BaseType))
                    {
                        try
                        {
                            if (pass == 0)
                            {
                                didWork |= CheckSyncListStruct(td);
                            }
                            else
                            {
                                didWork |= CheckNetworkBehaviour(td);
                                didWork |= CheckMessageBase(td);
                            }
                        }
                        catch (Exception ex)
                        {
                            if (scriptDef.MainModule.SymbolReader != null)
                                scriptDef.MainModule.SymbolReader.Dispose();
                            fail = true;
                            throw ex;
                        }
                    }

                    if (fail)
                    {
                        if (scriptDef.MainModule.SymbolReader != null)
                            scriptDef.MainModule.SymbolReader.Dispose();
                        return false;
                    }
                }
                watch.Stop();
                Console.WriteLine("Pass: " + pass + " took " + watch.ElapsedMilliseconds + " milliseconds");
            }

            if (didWork)
            {
                // build replacementMethods hash to speed up code site scan
                foreach (var m in lists.replacedMethods)
                {
                    lists.replacementMethodNames.Add(m.FullName);
                }

                // this must be done for ALL code, not just NetworkBehaviours
                try
                {
                    ProcessPropertySites();
                }
                catch (Exception e)
                {
                    Log.Error("ProcessPropertySites exception: " + e);
                    if (scriptDef.MainModule.SymbolReader != null)
                        scriptDef.MainModule.SymbolReader.Dispose();
                    return false;
                }


                if (fail)
                {
                    //Log.Error("Failed phase II.");
                    if (scriptDef.MainModule.SymbolReader != null)
                        scriptDef.MainModule.SymbolReader.Dispose();
                    return false;
                }

                string dest = Helpers.DestinationFileFor(outputDir, assName);
                //Console.WriteLine ("Output:" + dest);
                //Console.WriteLine ("Output:" + options.OutSymbolsFormat);

                var writeParams = Helpers.GetWriterParameters(readParams);

                // PdbWriterProvider uses ISymUnmanagedWriter2 COM interface but Mono can't invoke a method on it and crashes (actually it first throws the following exception and then crashes).
                // One solution would be to convert UNetWeaver to exe file and run it on .NET on Windows (I have tested that and it works).
                // However it's much more simple to just write mdb file.
                // System.NullReferenceException: Object reference not set to an instance of an object
                //   at(wrapper cominterop - invoke) Mono.Cecil.Pdb.ISymUnmanagedWriter2:DefineDocument(string, System.Guid &, System.Guid &, System.Guid &, Mono.Cecil.Pdb.ISymUnmanagedDocumentWriter &)
                //   at Mono.Cecil.Pdb.SymWriter.DefineDocument(System.String url, Guid language, Guid languageVendor, Guid documentType)[0x00000] in < filename unknown >:0
                if (writeParams.SymbolWriterProvider is PdbWriterProvider)
                {
                    writeParams.SymbolWriterProvider = new MdbWriterProvider();
                    // old pdb file is out of date so delete it. symbols will be stored in mdb
                    var pdb = Path.ChangeExtension(assName, ".pdb");
                    File.Delete(pdb);
                }

                scriptDef.Write(dest, writeParams);
            }

            if (scriptDef.MainModule.SymbolReader != null)
                scriptDef.MainModule.SymbolReader.Dispose();

            return true;
        }

        public static bool WeaveAssemblies(IEnumerable<string> assemblies, IEnumerable<string> dependencies, IAssemblyResolver assemblyResolver, string outputDir, string unityEngineDLLPath, string unityUNetDLLPath)
        {
            fail = false;
            lists = new WeaverLists();

            m_UnityAssemblyDefinition = AssemblyDefinition.ReadAssembly(unityEngineDLLPath);
            m_UNetAssemblyDefinition = AssemblyDefinition.ReadAssembly(unityUNetDLLPath);

            SetupUnityTypes();

            try
            {
                foreach (string ass in assemblies)
                {
                    if (!Weave(ass, dependencies, assemblyResolver, unityEngineDLLPath, unityUNetDLLPath, outputDir))
                    {
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("Exception :" + e);
                return false;
            }
            corLib = null;
            return true;
        }
    }
}
