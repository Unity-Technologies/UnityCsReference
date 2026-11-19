// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor
{
    // Test-only: writes data components to serialized files with chosen fileIDs, damages those files the way a broken or
    // outdated file would be, and loads them through PersistentManager. BeginFile starts the file the next WriteFile writes.
    [NativeHeader("Editor/Src/Utility/DataComponentSerializationTestBridge.bindings.h")]
    internal static class DataComponentSerializationTestBridge
    {
        [FreeFunction("DataComponentSerializationTestBridge::BeginFile")]
        internal static extern void BeginFile();

        [FreeFunction("DataComponentSerializationTestBridge::AddWrittenObject")]
        internal static extern void AddWrittenObject(EntityId id, long fileID);

        [FreeFunction("DataComponentSerializationTestBridge::MapUnwrittenObject")]
        internal static extern void MapUnwrittenObject(EntityId id, long fileID);

        [FreeFunction("DataComponentSerializationTestBridge::SetUnwrittenObjectFileIndex")]
        internal static extern void SetUnwrittenObjectFileIndex(int fileIndex);

        [FreeFunction("DataComponentSerializationTestBridge::RewriteRecordedSize")]
        internal static extern void RewriteRecordedSize(long fileID, uint size);

        [FreeFunction("DataComponentSerializationTestBridge::DropOwnerRow")]
        internal static extern void DropOwnerRow(long dataComponentFileID);

        [FreeFunction("DataComponentSerializationTestBridge::ReplaceEveryOwnerFileID")]
        internal static extern void ReplaceEveryOwnerFileID(long ownerFileID);

        [FreeFunction("DataComponentSerializationTestBridge::GetClassName")]
        internal static extern string[] GetClassName(uint componentTypeId);

        [FreeFunction("DataComponentSerializationTestBridge::RecordClassUnderName")]
        internal static extern void RecordClassUnderName(string klassName, string nameSpace, string assemblyName, string recordedKlassName, string recordedNameSpace, string recordedAssemblyName);

        [FreeFunction("DataComponentSerializationTestBridge::WriteFile")]
        internal static extern long[] WriteFile(string path, long describedFileID);

        [FreeFunction("DataComponentSerializationTestBridge::CopyFileWithOwnerCount")]
        internal static extern bool CopyFileWithOwnerCount(string sourcePath, string destinationPath, long dataFileID, long ownerFileID, int ownerCount);

        [FreeFunction("DataComponentSerializationTestBridge::GetOpenError")]
        internal static extern int GetOpenError(string path);

        [FreeFunction("DataComponentSerializationTestBridge::GetOwnerRow")]
        internal static extern long[] GetOwnerRow(string path, long dataComponentFileID);

        [FreeFunction("DataComponentSerializationTestBridge::GetOwnerRowCount")]
        internal static extern int GetOwnerRowCount(string path);

        [FreeFunction("DataComponentSerializationTestBridge::DescribeEntry")]
        internal static extern long[] DescribeEntry(string path, long fileID);

        [FreeFunction("DataComponentSerializationTestBridge::GetEntryClassName")]
        internal static extern string[] GetEntryClassName(string path, long fileID);

        [FreeFunction("DataComponentSerializationTestBridge::GetAllFileIDsByteSorted")]
        internal static extern long[] GetAllFileIDsByteSorted(string path);

        [FreeFunction("DataComponentSerializationTestBridge::ReadEntryInto")]
        internal static extern long[] ReadEntryInto(string path, long fileID, EntityId target);

        [FreeFunction("DataComponentSerializationTestBridge::CopyFileWithUpdatedObjectData")]
        internal static extern bool CopyFileWithUpdatedObjectData(string sourcePath, long fileID, string destinationPath);

        [FreeFunction("DataComponentSerializationTestBridge::WriteComponentBody")]
        internal static extern byte[] WriteComponentBody(EntityId componentEntityId);

        [FreeFunction("DataComponentSerializationTestBridge::ReadComponentBodyFromFile")]
        internal static extern void ReadComponentBodyFromFile(EntityId componentEntityId, byte[] body);

        [FreeFunction("DataComponentSerializationTestBridge::ReadComponentBodyFromFileWithStoredType")]
        internal static extern bool ReadComponentBodyFromFileWithStoredType(EntityId componentEntityId, uint writtenTypeId, byte[] body, bool throughRedirect);

        [FreeFunction("DataComponentSerializationTestBridge::LoadFileCompletely")]
        internal static extern int LoadFileCompletely(string path);

        [FreeFunction("DataComponentSerializationTestBridge::LoadFileObjects")]
        internal static extern EntityId[] LoadFileObjects(string path, long[] fileIDs);

        [FreeFunction("DataComponentSerializationTestBridge::UnloadFile")]
        internal static extern void UnloadFile(string path);

        [FreeFunction("DataComponentSerializationTestBridge::UnloadStream")]
        internal static extern void UnloadStream(string path);

        [FreeFunction("DataComponentSerializationTestBridge::OpenStream")]
        internal static extern bool OpenStream(string path);

        [FreeFunction("DataComponentSerializationTestBridge::GetSerializedFileIndex")]
        internal static extern int GetSerializedFileIndex(string path);

        [FreeFunction("DataComponentSerializationTestBridge::GetMappedEntityId")]
        internal static extern EntityId GetMappedEntityId(string path, long fileID);

        [FreeFunction("DataComponentSerializationTestBridge::GetEntityIdFromPathAndFileID")]
        internal static extern EntityId GetEntityIdFromPathAndFileID(string path, long fileID);

        [FreeFunction("DataComponentSerializationTestBridge::GetEntityIdFromSerializedFileIndexAndFileID")]
        internal static extern EntityId GetEntityIdFromSerializedFileIndexAndFileID(int fileIndex, long fileID);

        [FreeFunction("DataComponentSerializationTestBridge::MakeObjectPersistent")]
        internal static extern void MakeObjectPersistent(EntityId id, long fileID, string path);

        [FreeFunction("DataComponentSerializationTestBridge::AllocateUnusedEntityId")]
        internal static extern EntityId AllocateUnusedEntityId();

        [FreeFunction("DataComponentSerializationTestBridge::ForgetEntityIdMapping")]
        internal static extern bool ForgetEntityIdMapping(EntityId id);

        [FreeFunction("DataComponentSerializationTestBridge::GetAllFileIDs")]
        internal static extern long[] GetAllFileIDs(string path);

        [FreeFunction("DataComponentSerializationTestBridge::GetFileIDsOfNonNativeTypes")]
        internal static extern long[] GetFileIDsOfNonNativeTypes(string path);

        [FreeFunction("DataComponentSerializationTestBridge::IsDataComponentFileID")]
        internal static extern bool IsDataComponentFileID(string path, long fileID);

        [FreeFunction("DataComponentSerializationTestBridge::GetDataComponentType")]
        internal static extern EntityId GetDataComponentType(string path, long fileID);

        [FreeFunction("DataComponentSerializationTestBridge::GetDataComponentTypeEntityId")]
        internal static extern EntityId GetDataComponentTypeEntityId(uint componentTypeId);

        [FreeFunction("DataComponentSerializationTestBridge::GetStoredTypeState")]
        internal static extern long[] GetStoredTypeState(string path, long fileID);

        [FreeFunction("DataComponentSerializationTestBridge::GetEntityIdsAtPath")]
        internal static extern EntityId[] GetEntityIdsAtPath(string path);

        [FreeFunction("DataComponentSerializationTestBridge::PreallocatesObject")]
        internal static extern bool PreallocatesObject(EntityId id);

        [FreeFunction("DataComponentSerializationTestBridge::ReadObject")]
        internal static extern EntityId[] ReadObject(EntityId id);

        [FreeFunction("DataComponentSerializationTestBridge::ReadObjectThreaded")]
        internal static extern void ReadObjectThreaded(EntityId id);

        [FreeFunction("DataComponentSerializationTestBridge::GetComponentEntityIdAtIndex")]
        internal static extern EntityId GetComponentEntityIdAtIndex(GameObject go, int index);

        [FreeFunction("DataComponentSerializationTestBridge::GetObjectFileIDHint")]
        internal static extern long GetObjectFileIDHint(EntityId id);

        [FreeFunction("DataComponentSerializationTestBridge::SetObjectFileIDHint")]
        internal static extern void SetObjectFileIDHint(EntityId id, long hint);

        [FreeFunction("DataComponentSerializationTestBridge::GetComponentFileIDHint")]
        internal static extern long GetComponentFileIDHint(EntityId id);

        [FreeFunction("DataComponentSerializationTestBridge::SetComponentFileIDHint")]
        internal static extern void SetComponentFileIDHint(EntityId id, long hint);

        [FreeFunction("DataComponentSerializationTestBridge::GetComponentAtIndexFileIDHint")]
        internal static extern long GetComponentAtIndexFileIDHint(GameObject go, int index);

        [FreeFunction("DataComponentSerializationTestBridge::SetComponentAtIndexFileIDHint")]
        internal static extern void SetComponentAtIndexFileIDHint(GameObject go, int index, long hint);

        [FreeFunction("DataComponentSerializationTestBridge::GetStoredFileIDHint")]
        internal static extern long GetStoredFileIDHint(EntityId componentEntityId);

        [FreeFunction("DataComponentSerializationTestBridge::SetStoredFileIDHint")]
        internal static extern void SetStoredFileIDHint(EntityId componentEntityId, long hint);

        [FreeFunction("DataComponentSerializationTestBridge::GetChangeVersion")]
        internal static extern uint GetChangeVersion(EntityId componentEntityId);

        [FreeFunction("DataComponentSerializationTestBridge::GenerateFileIDs")]
        internal static extern void GenerateFileIDs(EntityId[] entityIds);

        [FreeFunction("DataComponentSerializationTestBridge::RemapReferences")]
        internal static extern void RemapReferences(GameObject go, EntityId[] sourceIds, EntityId[] destinationIds);
    }
}
