// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine.Assertions;
using Assert = UnityEngine.Assertions.AssertCore;

namespace Unity.Entities.Serialization
{
    // What we want to achieve:
    //  - A flexible file format that will allow efficient streaming.
    //  - Generic file content navigation, whatever the version of the file or the parser.
    //  - <OPT> Possibility to split a file into many or merge many into a single one (or coarser compound).
    //
    // How:
    //  - A RIFF styled approach, but designed to meet our expectation performance-wise.
    //  - File is composed of Nodes, recursively stored. Each node is of a given type, can have an associated metadata and a raw data.
    //  - Nodes hierarchy and their metadata are meant to reside in memory all the time, the raw data is loaded on demand (preferably directly to its destination location).
    //
    // Why:
    //  - A generic/flexible/version agnostic is mandatory if we want to have files that are consumable for many years.
    //  - The generic navigation will be useful for debug/troubleshooting tools.
    //  - <OPT> A generic way to split/merge could save us the trouble of manually developing these operations several times, handled probably in different ways, with no much added value.
    //
    // The layout of the file format consists of four main sections:
    //   1. File Header: identify the file, give the offset/size of the Nodes Hierarchy and Nodes Metadata
    //   2. Nodes data section: Containing all the Nodes' data segments
    //   3. Nodes metadata section: Containing all the Metadata blocks of the Nodes.
    //   4. Nodes hierarchy section: A hierarchy of Nodes. Nodes are describing the node type, its header data, where to find its metadata and where to load the segment data
    //
    // FILE LAYOUT
    // ====================================================================================
    // | Header |          Nodes Data Segments         | Nodes Metadata| Nodes Hierarchy  |
    // ====================================================================================

    /// <summary>
    /// Dots Serialization main class
    /// </summary>
    /// <remarks>
    /// Used as an entry point to create readers/writers and interact with the general Dots File format API
    /// </remarks>
    internal static class DotsSerialization
    {
        /// <summary>
        /// Create a Dots File Writer to serialize data to a DOTS file
        /// </summary>
        /// <param name="writer">Stream writer to use for the serialization</param>
        /// <param name="fileId">A unique ID identifying the file, used for identification purpose and possibly future cross file references</param>
        /// <param name="fileType">Type of the file, should be user level.</param>
        /// <returns></returns>
        public static DotsSerializationWriter CreateWriter(BinaryWriter writer, Hash128 fileId, FixedString64Bytes fileType)
        {
            return new DotsSerializationWriter(writer, fileId, fileType);
        }

        /// <summary>
        /// Create a Dots File Reader to deserialize a file
        /// </summary>
        /// <param name="reader">The stream to use for reading the file's data</param>
        /// <returns>The reader object</returns>
        /// <remarks>
        /// This operation will check if the file is indeed a Dots File, load the nodes and metadata segments into memory
        /// </remarks>
        public static DotsSerializationReader CreateReader(BinaryReader reader)
        {
            return new DotsSerializationReader(reader);
        }

        public static DotsSerializationReader CreateReader(ref BlobHeader header)
        {
            return new DotsSerializationReader(ref header);
        }

        internal static readonly byte[] HeaderMagic = {(byte)'D', (byte)'O', (byte)'T', (byte)'S', (byte)'B', (byte)'I', (byte)'N', (byte)'!'};
        internal const ulong RootNodeHash = 0;

        [StructLayout(LayoutKind.Sequential)]
        public struct FileHeader
        {
            public long MagicValue;            // 8-bytes containing the ASCII value of 'DOTSBIN!'
            public int FileVersion;            // The version of the file format that was used to write this file
            public int HeaderSize;             // Size of this header, can ensure us a backward/forward compatibility through format changes
            public Hash128 FileId;             // A hash value that uniquely identifies this file
            public FixedString64Bytes FileType;     // 62-bytes of an UTF8 string that defines the purpose of the file
            public int FirstLevelNodesCount;   // The number of nodes on the first level of the node hierarchy.
            public long NodesSectionOffset;    // The offset of the node section from the start of the file.
            public int NodesSectionSize;       // The size of the node section in bytes.
            public long MetadataSectionOffset; // The offset of the metadata section from the start of the file.
            public int MetadataSectionSize;    // The size of the metadata section in bytes.
            public long DataSectionOffset;     // The offset of the data section from the start of the file.
            public long DataSectionSize;       // The size of the data section in bytes.
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct BlobHeader
        {
            public FileHeader FileHeader;
            public BlobArray<byte> NodeSection;
            public BlobArray<byte> MetadataSection;
        }

        // Node Header types definitions
        // We can't inherit structure, but we still want a way to process data in a generic way, so we will rely on composition.
        // All Node headers must use a .net sequential layout, each custom node must define a NodeHeader data member that is at the first position.

        [StructLayout(LayoutKind.Sequential)]
        public struct NodeHeader : IComponentData
        {
            public ulong NodeTypeHash;          // Type of the Node, a fixed ID.
            public Hash128 Id;                  // Should be a unique identifier
            public int Size;                    // Size of the Node Header data, used to navigate to the next Node Header (the first child, if any, of the next sibling, if any)
            public int NextSiblingOffset;       // Offset from the start of the Node section, of the next sibling, -1 if none
            public int ChildrenCount;           // Number of children inside this node
            public long MetadataStartingOffset; // The offset of the metadata for this node from the start of the file.
            public int MetadataSize;            // The size of the metadata for this node in bytes.
            public long DataStartingOffset;     // The offset of the data for this node from the start of the file.
            public long DataSize;               // The size of the data for this node in bytes.

            public bool HasMetadata => MetadataStartingOffset != -1;
        }

        // Just an empty type that will act as a folder, user will typically use the node Id to identify this folder
        [StructLayout(LayoutKind.Sequential)]
        public struct FolderNode : IComponentData
        {
            public NodeHeader Header;
        }

        // A node that stores one to many strings in the Raw Data section, each string is stored as UTF8 and is preceded by its size store in an integer
        [StructLayout(LayoutKind.Sequential)]
        public struct StringTableNode : IComponentData
        {
            public NodeHeader Header;
            public int StringCount;
        }

        // This node type is to be used where all the payload is stored the raw data section.
        // We add a Revision field because we still have to take care of dealing with revisions, to make sure the raw data will interpreted the way it should be.
        [StructLayout(LayoutKind.Sequential)]
        public struct RevisionedRawDataNode : IComponentData
        {
            public NodeHeader Header;
            public int Revision;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TypeNamesNode : IComponentData
        {
            public NodeHeader Header;
            public int TypeCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ChunkLayoutNode : IComponentData
        {
            public NodeHeader Header;
            public int ArchetypeCount;
        }
    }

    /// <summary>
    /// Writer class, instances are retrieved through <see cref="DotsSerialization.CreateWriter"/>
    /// </summary>
    internal unsafe class DotsSerializationWriter : IDisposable
    {
        public void Dispose()
        {
            if (_headerWritten == 0)
            {
                WriteHeader();
                _headerWritten = 1;
            }

            if (_isDisposed == 1)
            {
                return;
            }

            Memory.Unmanaged.Free(_rootNodeHeader, Allocator.Persistent);
            _metadataSection.Dispose();
            _nodesAllocation.Dispose();
            _nodesStack.Dispose();
            _isDisposed = 1;
        }

        public unsafe void WriteHeader()
        {
            var pos = _writer.Position;
            _header.FirstLevelNodesCount = _rootNode.NodeHeader.ChildrenCount;
            _header.DataSectionSize = pos - _header.DataSectionOffset;
            _header.MetadataSectionOffset = pos;
            _header.MetadataSectionSize = _metadataSection.Length;
            _header.NodesSectionOffset = _header.MetadataSectionOffset + _header.MetadataSectionSize;
            _header.NodesSectionSize = _nodesAllocation.CurrentGlobalOffset;

            // Write the header now it has all the info
            WriteHeaderToFile();

            // Write the metadata
            _writer.ImHexWriteArray<byte>("MetadataSection", _metadataSection.Length);
            _writer.WriteBytes(_metadataSection.Ptr, _metadataSection.Length);

            // Write the nodes data
            ref var pages = ref _nodesAllocation.Pages;
            for (int i = 0; i < pages.Length; i++)
            {
                _writer.ImHexWriteArray<byte>("NodePage", pages[i].FreeOffset);
                _writer.WriteBytes(pages[i].Buffer, pages[i].FreeOffset);
            }
        }

        public unsafe BlobAssetReference<DotsSerialization.BlobHeader> WriteHeaderAndBlob()
        {
            var blobBuilder = new BlobBuilder(Allocator.Persistent);
            ref var root = ref blobBuilder.ConstructRoot<DotsSerialization.BlobHeader>();

            var pos = _writer.Position;
            _header.FirstLevelNodesCount = _rootNode.NodeHeader.ChildrenCount;
            _header.DataSectionSize = pos - _header.DataSectionOffset;
            _header.MetadataSectionOffset = pos;
            _header.MetadataSectionSize = _metadataSection.Length;
            _header.NodesSectionOffset = _header.MetadataSectionOffset + _header.MetadataSectionSize;
            _header.NodesSectionSize = _nodesAllocation.CurrentGlobalOffset;

            root.FileHeader = _header;

            // Write the header now it has all the info
            WriteHeaderToFile();

            // Write the metadata
            _writer.ImHexWriteArray<byte>("MetaDataSection", _metadataSection.Length);
            _writer.WriteBytes(_metadataSection.Ptr, _metadataSection.Length);
            var metaSectionArray = blobBuilder.Allocate(ref root.MetadataSection, _metadataSection.Length);
            UnsafeUtility.MemCpy(metaSectionArray.GetUnsafePtr(), _metadataSection.Ptr, _metadataSection.Length);

            // Write the nodes data
            var nodeHeaderSize = UnsafeUtility.SizeOf<DotsSerialization.NodeHeader>();
            ref var pages = ref _nodesAllocation.Pages;
            int totalNodeSize = nodeHeaderSize;
            for (int i = 0; i < pages.Length; i++)
                totalNodeSize += pages[i].FreeOffset;

            var nodeSectionArray = blobBuilder.Allocate(ref root.NodeSection, totalNodeSize);
            var rootNode = (DotsSerialization.NodeHeader*)nodeSectionArray.GetUnsafePtr();

            UnsafeUtility.MemClear(rootNode, nodeHeaderSize);
            rootNode->NodeTypeHash = DotsSerialization.RootNodeHash;
            rootNode->Size = nodeHeaderSize;
            rootNode->ChildrenCount = _rootNode.NodeHeader.ChildrenCount;
            rootNode->NextSiblingOffset = -1;
            rootNode->DataStartingOffset = -1;
            rootNode->MetadataStartingOffset = -1;

            byte* nodes = (byte*)(rootNode + 1);

            for (int i = 0; i < pages.Length; i++)
            {
                _writer.ImHexWriteArray<byte>("NodePage", pages[i].FreeOffset);
                _writer.WriteBytes(pages[i].Buffer, pages[i].FreeOffset);
                UnsafeUtility.MemCpy(nodes, pages[i].Buffer, pages[i].FreeOffset);
                nodes += _metadataSection.Length;
            }

            var blobAssetRef = blobBuilder.CreateBlobAssetReference<DotsSerialization.BlobHeader>(Allocator.Persistent);
            blobBuilder.Dispose();
            return blobAssetRef;
        }


        /// <summary>
        /// Create a Node of the given type, with an optional unique id
        /// </summary>
        /// <param name="id">Unique Id to identify the node</param>
        /// <typeparam name="T">.net type corresponding to the node to serialize</typeparam>
        /// <returns>Handle that will allow serialization of data</returns>
        /// <remarks>This node will be created as a child of the current parent node but will immediately become the current parent until <see cref="Dispose"/> is called.
        /// It is recommended to use the <code>using (var myNode = CreateNode()){ // Child node create here } pattern</code>
        /// </remarks>
        public unsafe NodeHandle<T> CreateNode<T>(Hash128 id = default) where T : unmanaged, IComponentData
        {
           var curNodeHeader = (byte*)_nodesStack[_nodesStack.Length - 1];

            var nodeData = AllocateNodeData<T>(out var nodeOffset);
            var handle = new NodeHandle<T>(this, nodeData);

            var info = TypeManager.GetTypeInfo<T>();
            ref var h = ref handle.AsNodeHeader;
            h.NodeTypeHash = info.StableTypeHash;
            h.Size = info.TypeSize;
            h.Id = id;
            h.NextSiblingOffset = -1;
            h.DataStartingOffset = -1;
            h.MetadataStartingOffset = -1;

            ((DotsSerialization.NodeHeader*)curNodeHeader)->ChildrenCount++;

            // Set the NextSiblingOffset of the previous sibling
            SetNextSiblingOffset(curNodeHeader, nodeData, nodeOffset);

            // Push the new node on the stack
            _nodesStack.Add((IntPtr)nodeData);

            // Increment the tracked node counter to prevent user from doing interleaving write for a given node
            ++_trackedNodeCounter;

            return handle;
        }

        /// <summary>
        /// Expose a handle object that allows stream base writing to the node's raw data.
        /// </summary>
        /// <typeparam name="T">.net type of the Node</typeparam>
        /// <remarks>
        /// While you can use the <see cref="NodeHandle{T}"/> APIs to write directly to the raw data segment, this type allow you to access a Stream object to perform stream based writes.
        /// Call <see cref="Dispose()"/> on the instance to go one level up in the hierarchy of nodes
        /// </remarks>
        public struct WriterHandle<T> : IDisposable where T : unmanaged, IComponentData
        {
            private DotsSerializationWriter _owner;
            private readonly NodeHandle<T> _node;
            private readonly long _startPosition;

            internal WriterHandle(DotsSerializationWriter owner, NodeHandle<T> node)
            {
                _owner = owner;
                _node = node;

                _startPosition = _owner.InitWriteData(node);
            }

            public void Dispose()
            {
                _owner.EndWriteData(_node, _startPosition);
                _owner = null;
            }

            /// <summary>
            /// Access to the BinaryWriter object that must be used to serialize data in the raw data section
            /// </summary>
            public BinaryWriter Writer => _owner._writer;
        }

        /// <summary>
        /// Deferred Writer allows to combine dependent node serialization by deferring the write of the raw data upon dispose as opposed to immediate writing for <see cref="WriterHandle{T}"/>.
        /// </summary>
        /// <typeparam name="T">Type of the node</typeparam>
        /// <remarks>
        /// This writer will expose a <see cref="MemoryBinaryWriter"/> stream to capture data im memory and will serialize its content upon <see cref="Dispose()"/>
        /// </remarks>
        public struct DeferredWriterHandle<T> : IDisposable where T : unmanaged, IComponentData
        {
            private DotsSerializationWriter _owner;
            private readonly NodeHandle<T> _node;
            private readonly MemoryBinaryWriter _writer;

            internal DeferredWriterHandle(DotsSerializationWriter owner, NodeHandle<T> node)
            {
                _owner = owner;
                _node = node;
                _writer = new MemoryBinaryWriter();
            }

            /// <summary>
            /// Determine if the handle is valid or not
            /// </summary>
            public bool IsValid => _owner != null;

            public unsafe void Dispose()
            {
                _node.SubmitDeferredNodeData(_writer.Data, _writer.Length);
                _writer.Dispose();
                _owner = null;
            }

            /// <summary>
            /// Access to the BinaryWriter object that must be used to serialize data in the raw section.
            /// </summary>
            /// <remarks>
            /// This writer is a memory one, data will be first stored in memory, then written to the Data Segment upon dispose.
            /// </remarks>
            public BinaryWriter Writer => _writer;
        }

        /// <summary>
        /// Type handling a given Node being serialized
        /// </summary>
        /// <typeparam name="T">Type of the node being serialized</typeparam>
        public struct NodeHandle<T> : IDisposable where T : unmanaged, IComponentData
        {
            private readonly DotsSerializationWriter _owner;
            internal readonly unsafe byte* NodeHeaderAddress;
            internal unsafe int TrackedSegmentDataWrite
            {
                get => _owner.GetNodeTrackedSegmentDataWrite(NodeHeaderAddress);
                set => _owner.SetNodeTrackedSegmentDataWrite(NodeHeaderAddress, value);
            }

            public unsafe ref T NodeHeader => ref UnsafeUtility.AsRef<T>(NodeHeaderAddress);
            internal unsafe ref DotsSerialization.NodeHeader AsNodeHeader => ref UnsafeUtility.AsRef<DotsSerialization.NodeHeader>(NodeHeaderAddress);

            internal unsafe NodeHandle(DotsSerializationWriter owner, byte* nodeHeaderAddress)
            {
                _owner = owner;
                NodeHeaderAddress = nodeHeaderAddress;
                TrackedSegmentDataWrite = -1;
            }

            /// <summary>
            /// Disposing the node will go one level up into the hierarchy, its parent becomes the new current node.
            /// </summary>
            public void Dispose()
            {
                _owner.PopNode(ref this);
            }

            /// <summary>
            /// Set metadata associated to this node
            /// </summary>
            /// <param name="blobAssetReference">BlobAsset storing the metadata to set</param>
            /// <typeparam name="TB">BlobAsset type</typeparam>
            /// <returns>true if the call succeeded, false if the metadata was already set for this node.</returns>
            public bool SetMetadata<TB>(BlobAssetReference<TB> blobAssetReference) where TB : unmanaged
            {
                return _owner.SetNodeMetadata(this, blobAssetReference);
            }

            /// <summary>
            /// Write data into the raw data section for this node
            /// </summary>
            /// <param name="data">Source buffer</param>
            /// <param name="dataLength">Length to write</param>
            /// <remarks>
            /// This API can be called multiple times, it will write data sequentially. Notice that you can't call it multiple times before AND after creating children nodes (i.e. create this node A, write data for A, create child B, write data for A) for the reason the raw data section has to be a contiguous segment of data and writing data for other node would break this contiguity.
            /// </remarks>
            public unsafe void WriteData(void* data, int dataLength)
            {
                _owner.WriteData(this, data, dataLength);
            }

            /// <summary>
            /// Access the Writer Handle to perform stream based access.
            /// </summary>
            /// <returns></returns>
            /// The purpose of this object is to perform stream based writes to the node raw data and to mark the write sequence completed when <see cref="Dispose"/> is called.
            public WriterHandle<T> GetWriterHandle()
            {
                return new WriterHandle<T>(_owner, this);
            }

            /// <summary>
            /// Access the Deferred writer, to perform out of node sequence writes (multiple write operations across multiple node creation).
            /// </summary>
            /// <returns></returns>
            public DeferredWriterHandle<T> GetDeferredWriteHandle()
            {
                return new DeferredWriterHandle<T>(_owner, this);
            }

            internal unsafe void SubmitDeferredNodeData(byte* rawData, int rawDataLength)
            {
                WriteData(rawData, rawDataLength);
            }
        }

        private unsafe int GetNodeTrackedSegmentDataWrite(byte* nodeHeaderAddress)
        {
            if (!_trackedSegmentDataWriteByNode.TryGetValue((IntPtr)nodeHeaderAddress, out var id))
            {
                return -1;
            }

            return id;
        }

        private unsafe void SetNodeTrackedSegmentDataWrite(byte* nodeHeaderAddress, int value)
        {
            var headerAddress = (IntPtr) nodeHeaderAddress;
            if (!_trackedSegmentDataWriteByNode.ContainsKey(headerAddress))
            {
                _trackedSegmentDataWriteByNode.Add(headerAddress, value);
            }
            else
            {
                _trackedSegmentDataWriteByNode[headerAddress] = value;
            }
        }

        private unsafe bool SetNodeMetadata<T, TB>(NodeHandle<T> nodeHandle, BlobAssetReference<TB> blobAssetReference) where T : unmanaged, IComponentData where TB : unmanaged
        {
            ref var header = ref nodeHandle.AsNodeHeader;
            if (header.MetadataStartingOffset != -1)
            {
                return false;
            }

            var blobAssetLength = blobAssetReference.m_data.Header->Length;
            var serializeReadyHeader = BlobAssetHeader.CreateForSerialize(blobAssetLength, blobAssetReference.m_data.Header->Hash);

            header.MetadataStartingOffset = _metadataSection.Length;
            _metadataSection.AddRange(&serializeReadyHeader, sizeof(BlobAssetHeader));
            _metadataSection.AddRange(blobAssetReference.m_data.Header + 1, blobAssetLength);
            return true;
        }

        private long InitWriteData<T>(NodeHandle<T> nodeHandle) where T : unmanaged, IComponentData
        {
            ref var nodeHeader = ref nodeHandle.AsNodeHeader;
            nodeHandle.TrackedSegmentDataWrite = _trackedNodeCounter;
            nodeHeader.DataStartingOffset = _writer.Position;
            nodeHeader.DataSize = 0;

            return _writer.Position;
        }

        private void EndWriteData<T>(NodeHandle<T> nodeHandle, long startPosition) where T : unmanaged, IComponentData
        {
            ref var nodeHeader = ref nodeHandle.AsNodeHeader;

            // Check if the user is interleaving write operation across nodes: issue a write on A, then create children for A, then try to issue write on A again
            //  A would end up with a non consecutive data segment and we can't allow that.
            if (nodeHandle.TrackedSegmentDataWrite != _trackedNodeCounter)
            {
                throw new InvalidOperationException($"Can't write data for the node {nodeHeader.Id} before and after processing its children. You must pack your write before or after processing the children ");
            }

            // Write the data size
            nodeHeader.DataSize = _writer.Position - startPosition;
        }

        private unsafe void WriteData<T>(NodeHandle<T> nodeHandle, void* data, int dataLength) where T : unmanaged, IComponentData
        {
            ref var nodeHeader = ref nodeHandle.AsNodeHeader;

            // Detect first write operation for this node
            if (nodeHandle.TrackedSegmentDataWrite == -1)
            {
                nodeHandle.TrackedSegmentDataWrite = _trackedNodeCounter;
                nodeHeader.DataStartingOffset = _writer.Position;
                nodeHeader.DataSize = 0;
            }

            // Check if the user is interleaving write operation across nodes: issue a write on A, then create children for A, then try to issue write on A again
            //  A would end up with a non consecutive data segment and we can't allow that.
            if (nodeHandle.TrackedSegmentDataWrite != _trackedNodeCounter)
            {
                throw new InvalidOperationException($"Can't write data for the node {nodeHeader.Id} before and after processing its children. You must pack your write before or after processing the children ");
            }

            _writer.ImHexWrite<T>("data");
            _writer.WriteBytes(data, dataLength);
            nodeHeader.DataSize += dataLength;
        }

        private unsafe void SetNextSiblingOffset(byte* parentNode, byte* newChildNode, int newChildOffset)
        {
            bool isValid = _currentLastChildOffset.TryGetValue((IntPtr) parentNode, out var currentLastChild);
            Assert.IsTrue(isValid);

            // Add an entry for the new child node, which may have children to fix later on...
            _currentLastChildOffset.Add((IntPtr)newChildNode, IntPtr.Zero);

            // Will be zero if there is no child, so nothing to set
            if (currentLastChild != IntPtr.Zero)
            {
                ((DotsSerialization.NodeHeader*) currentLastChild)->NextSiblingOffset = newChildOffset;
            }
            _currentLastChildOffset[(IntPtr)parentNode] = (IntPtr)newChildNode;
        }

        private unsafe byte* AllocateNodeData<T>(out int nodeOffset) where T : unmanaged, IComponentData
        {
            var nodeSize = UnsafeUtility.SizeOf<T>();
            nodeOffset = _nodesAllocation.CurrentGlobalOffset;
            return _nodesAllocation.Reserve(nodeSize);
        }

        private unsafe void PopNode<T>(ref NodeHandle<T> nodeHandle) where T : unmanaged, IComponentData
        {
            _currentLastChildOffset.Remove((IntPtr) nodeHandle.NodeHeaderAddress);

            Assert.IsTrue(_nodesStack.Length > 1);
            Assert.AreEqual((IntPtr)nodeHandle.NodeHeaderAddress, _nodesStack[_nodesStack.Length-1]);
            _nodesStack.RemoveAt(_nodesStack.Length-1);
        }

        private readonly BinaryWriter _writer;
        private DotsSerialization.FileHeader _header;
        private readonly PagedAllocation _nodesAllocation;
        private UnsafeList<byte> _metadataSection;
        private byte _isDisposed;
        private byte _headerWritten;
        private int _trackedNodeCounter;
        private DotsSerialization.NodeHeader* _rootNodeHeader;
        private NodeHandle<DotsSerialization.NodeHeader> _rootNode;
        private readonly Dictionary<IntPtr, IntPtr> _currentLastChildOffset;
        private UnsafeList<IntPtr> _nodesStack;
        private readonly Dictionary<IntPtr, int> _trackedSegmentDataWriteByNode;

        internal unsafe DotsSerializationWriter(BinaryWriter writer, Hash128 fileId, FixedString64Bytes fileType)
        {
            _writer = writer;

            _currentLastChildOffset = new Dictionary<IntPtr, IntPtr>();
            _metadataSection = new UnsafeList<byte>(64*1024*1024, Allocator.Persistent);
            _nodesAllocation = new PagedAllocation(Allocator.Persistent);
            _trackedNodeCounter = 0;
            _trackedSegmentDataWriteByNode = new Dictionary<IntPtr, int>();

            _rootNodeHeader = (DotsSerialization.NodeHeader*) Memory.Unmanaged.Allocate(sizeof(DotsSerialization.NodeHeader), 16, Allocator.Persistent);
            UnsafeUtility.MemClear(_rootNodeHeader, sizeof(DotsSerialization.NodeHeader));
            _rootNodeHeader->NodeTypeHash = DotsSerialization.RootNodeHash;

            var rootAddr = (byte*)_rootNodeHeader;
            _rootNode = new NodeHandle<DotsSerialization.NodeHeader>(this, rootAddr);
            _currentLastChildOffset.Add((IntPtr)rootAddr, IntPtr.Zero);
            _nodesStack = new UnsafeList<IntPtr>(16, Allocator.Persistent);
            _nodesStack.Add((IntPtr)rootAddr);

            // Initialize the header with the info we already have
            fixed (byte* addr = DotsSerialization.HeaderMagic)
            {
                UnsafeUtility.MemCpy(UnsafeUtility.AddressOf(ref _header.MagicValue), addr, DotsSerialization.HeaderMagic.Length);
            }

            _header.FileVersion = SerializeUtility.CurrentFileFormatVersion;
            _header.FileId = fileId;
            _header.HeaderSize = UnsafeUtility.SizeOf<DotsSerialization.FileHeader>();
            _header.FileType = fileType;
            _header.DataSectionOffset = _header.HeaderSize;

            // Set the writer position at the Data Segment starting location
            _writer.ImHexWrite<DotsSerialization.FileHeader>("FileHeader");
            _writer.Position = _header.DataSectionOffset;
        }

        private unsafe void WriteHeaderToFile()
        {
            var pos = _writer.Position;
            _writer.Position = 0L;
            _writer.ImHexWrite<DotsSerialization.FileHeader>("header");
            _writer.WriteBytes(UnsafeUtility.AddressOf(ref _header), UnsafeUtility.SizeOf<DotsSerialization.FileHeader>());
            _writer.Position = pos;
        }
    }

    /// <summary>
    /// This type allows a friendly API to the <see cref="DotsSerialization.StringTableNode"/>
    /// </summary>
    internal struct StringTableWriterHandle : IDisposable
    {
        public StringTableWriterHandle(DotsSerializationWriter.NodeHandle<DotsSerialization.StringTableNode> handle)
        {
            _nodeHandle = handle;
            _nodeHandle.NodeHeader.StringCount = 0;

            // Seems weird to dispose immediately but the Dispose is used to control hierarchy in the Dots File
            // We can still use the Node Header as it's targeting native memory that is written to disk at the dispose of the Dots Writer
            _nodeHandle.Dispose();

            _writerHandle = _nodeHandle.GetDeferredWriteHandle();
        }

        /// <summary>
        /// Write a string to the string table
        /// </summary>
        /// <param name="str">The string to write to</param>
        /// <returns>The index of the string into the string table</returns>
        public unsafe int WriteString(string str)
        {
            ++_nodeHandle.NodeHeader.StringCount;
            var bytes = Encoding.UTF8.GetBytes(str);
            fixed (void* b = bytes)
            {
                var pos = (int)_writerHandle.Writer.Position;
                _writerHandle.Writer.ImHexWrite<int>("stringLength");
                _writerHandle.Writer.Write(bytes.Length);
                _writerHandle.Writer.ImHexWriteArray<byte>("stringData", bytes.Length);
                _writerHandle.Writer.WriteBytes(b, bytes.Length);
                return pos;
            }
        }

        private DotsSerializationWriter.NodeHandle<DotsSerialization.StringTableNode> _nodeHandle;
        private DotsSerializationWriter.DeferredWriterHandle<DotsSerialization.StringTableNode> _writerHandle;
        public void Dispose()
        {
            _writerHandle.Dispose();
        }
    }


    internal struct StringTableReaderHandle : IDisposable
    {
        public unsafe StringTableReaderHandle(DotsSerializationReader.NodeHandle stringTableNode)
        {
            _stringTableDataSize = (uint)stringTableNode.DataSize;
            _stringTableData = (byte*)Memory.Unmanaged.Allocate(stringTableNode.DataSize, 4, Allocator.TempJob);
            stringTableNode.ReadData(_stringTableData);
        }

        public unsafe void Dispose()
        {
            Memory.Unmanaged.Free(_stringTableData, Allocator.TempJob);
        }

        public unsafe string GetString(int offset)
        {
            var l = GetStringLength(offset);
            if (l == -1)
            {
                return $"Offset out of range {offset}";
            }
            return Encoding.UTF8.GetString(_stringTableData + offset + 4, l);
        }

        /// <summary>
        /// Get the string as a FixedString32Bytes
        /// </summary>
        /// <param name="offset">Offset of the string into the String Table</param>
        /// <returns>The string</returns>
        /// <remarks>If the string is more than 29 bytes encoded as utf8, it will be truncated.</remarks>
        public unsafe FixedString32Bytes GetString32(int offset)
        {
            var l = GetStringLength(offset);
            if (l == -1)
            {
                return new FixedString32Bytes($"Invalid Offset {offset}");
            }

            var res = new FixedString32Bytes();
            res.Append(_stringTableData + offset + 4, Math.Min(l, 32));
            return res;
        }

        /// <summary>
        /// Get the string as a FixedString64Bytes
        /// </summary>
        /// <param name="offset">Offset of the string into the String Table</param>
        /// <returns>The string</returns>
        /// <remarks>If the string is more than 61 bytes encoded as utf8, it will be truncated.</remarks>
        public unsafe FixedString64Bytes GetString64(int offset)
        {
            var l = GetStringLength(offset);
            if (l == -1)
            {
                return new FixedString64Bytes($"Invalid Offset {offset}");
            }

            var res = new FixedString64Bytes();
            res.Append(_stringTableData + offset + 4, Math.Min(l, 64));
            return res;
        }

        /// <summary>
        /// Get the string as a FixedString128Bytes
        /// </summary>
        /// <param name="offset">Offset of the string into the String Table</param>
        /// <returns>The string</returns>
        /// <remarks>If the string is more than 125 bytes encoded as utf8, it will be truncated.</remarks>
        public unsafe FixedString128Bytes GetString128(int offset)
        {
            var l = GetStringLength(offset);
            if (l == -1)
            {
                return new FixedString128Bytes($"Invalid Offset {offset}");
            }

            var res = new FixedString128Bytes();
            res.Append(_stringTableData + offset + 4, Math.Min(l, 128));
            return res;
        }

        /// <summary>
        /// Get the string as a FixedString512Bytes
        /// </summary>
        /// <param name="offset">Offset of the string into the String Table</param>
        /// <returns>The string</returns>
        /// <remarks>If the string is more than 509 bytes encoded as utf8, it will be truncated.</remarks>
        public unsafe FixedString512Bytes GetString512(int offset)
        {
            var l = GetStringLength(offset);
            if (l == -1)
            {
                return new FixedString512Bytes($"Invalid Offset {offset}");
            }

            var res = new FixedString512Bytes();
            res.Append(_stringTableData + offset + 4, Math.Min(l, 512));
            return res;
        }

        /// <summary>
        /// Get the string as a FixedString4096Bytes
        /// </summary>
        /// <param name="offset">Offset of the string into the String Table</param>
        /// <returns>The string</returns>
        /// <remarks>If the string is more than 4093 bytes encoded as utf8, it will be truncated.</remarks>
        public unsafe FixedString4096Bytes GetString4096(int offset)
        {
            var l = GetStringLength(offset);
            if (l == -1)
            {
                return new FixedString4096Bytes($"Invalid Offset {offset}");
            }

            var res = new FixedString4096Bytes();
            res.Append(_stringTableData + offset + 4, Math.Min(l, 4096));
            return res;
        }

        /// <summary>
        /// Retrieve the length of the string
        /// </summary>
        /// <param name="offset"></param>
        /// <returns>Length in bytes</returns>
        /// <remarks>
        /// The returned length in not the string character count, the string is stored as UTF8 and the length is the number of bytes it takes to store the string.
        /// There is no null terminated character
        /// </remarks>
        public unsafe int GetStringLength(int offset)
        {
            if (offset < 0 || offset >= _stringTableDataSize)
            {
                return -1;
            }
            return *(int*) (_stringTableData + offset);
        }

        private readonly unsafe byte* _stringTableData;
        private readonly uint _stringTableDataSize;
    }

    /// <summary>
    /// Helper class to allow access to specific node type implemented in the package
    /// </summary>
    internal static class DotsSerializationHelpers
    {
        /// <summary>
        /// Create a StringTable node
        /// </summary>
        /// <param name="writer">The writer where the node is created in</param>
        /// <param name="id">Id of the node</param>
        /// <returns>The handle object of the string table</returns>
        public static StringTableWriterHandle CreateStringTableNode(this DotsSerializationWriter writer, Hash128 id)
        {
            return new StringTableWriterHandle(writer.CreateNode<DotsSerialization.StringTableNode>(id));
        }

        /// <summary>
        /// Open a string table node from a reader
        /// </summary>
        /// <param name="reader">The reader the node is stored into</param>
        /// <param name="stringTableNode">Handle of the String Table node</param>
        /// <returns>String Table handle</returns>
        public static StringTableReaderHandle OpenStringTableNode(this DotsSerializationReader reader, DotsSerializationReader.NodeHandle stringTableNode)
        {
            return new StringTableReaderHandle(stringTableNode);
        }
    }

    /// <summary>
    /// Reader of a Dots Serialization file, get instance using the <see cref="DotsSerialization"/> class.
    /// </summary>
    internal class DotsSerializationReader : IDisposable
    {
        private readonly BinaryReader _reader;
        private readonly unsafe DotsSerialization.NodeHeader* _rootNodeHeader;
        private readonly unsafe byte* _nodesSection;
        private readonly unsafe byte* _metadataSection;

        internal unsafe DotsSerializationReader(BinaryReader reader)
        {
            DotsSerialization.FileHeader fileHeader;
            _reader = reader;
            fixed (byte* m = DotsSerialization.HeaderMagic)
            {
                _reader.ReadBytes(&fileHeader, UnsafeUtility.SizeOf<DotsSerialization.FileHeader>());

                // Check for the header magic
                if (UnsafeUtility.MemCmp(&fileHeader, m, DotsSerialization.HeaderMagic.Length) != 0)
                {
                    throw new Exception("The file doesn't seem to be a DOTS Serialization file.");
                }
            }

            if (fileHeader.FileVersion != SerializeUtility.CurrentFileFormatVersion)
                throw new Exception($"Version mismatch! The file was written with version {fileHeader.FileVersion} but the latest version is {SerializeUtility.CurrentFileFormatVersion}.");

            // Read Node and metadata sections
            var nodeHeaderSize = UnsafeUtility.SizeOf<DotsSerialization.NodeHeader>();
            _rootNodeHeader = (DotsSerialization.NodeHeader*)Memory.Unmanaged.Allocate(fileHeader.NodesSectionSize + nodeHeaderSize, 16, Allocator.Persistent);
            _nodesSection = (byte*)(_rootNodeHeader + 1);
            _reader.Position = fileHeader.NodesSectionOffset;
            _reader.ReadBytes(_nodesSection, fileHeader.NodesSectionSize);

            // All nodes are stored in the same memory area, however the root is not serialized, so we've allocated extra memory for it and now we have to initialize it
            // It has to be the first node of the allocated memory heap so we can seamlessly step into its first child, however all the other nodes are offset from _nodeSection
            UnsafeUtility.MemClear(_rootNodeHeader, nodeHeaderSize);
            _rootNodeHeader->NodeTypeHash = DotsSerialization.RootNodeHash;
            _rootNodeHeader->Size = nodeHeaderSize;
            _rootNodeHeader->ChildrenCount = fileHeader.FirstLevelNodesCount;
            _rootNodeHeader->NextSiblingOffset = -1;
            _rootNodeHeader->DataStartingOffset = -1;
            _rootNodeHeader->MetadataStartingOffset = -1;

            _metadataSection = (byte*)Memory.Unmanaged.Allocate(fileHeader.MetadataSectionSize, 16, Allocator.Persistent);
            _reader.Position = fileHeader.MetadataSectionOffset;
            _reader.ReadBytes(_metadataSection, fileHeader.MetadataSectionSize);
        }

        internal unsafe DotsSerializationReader(ref DotsSerialization.BlobHeader blobHeader)
        {
            _rootNodeHeader = (DotsSerialization.NodeHeader*)blobHeader.NodeSection.GetUnsafePtr();
            UnityEngine.Assertions.AssertCore.AreEqual(DotsSerialization.RootNodeHash, _rootNodeHeader->NodeTypeHash);
            _nodesSection = (byte*)(_rootNodeHeader + 1);
            _metadataSection = (byte*) blobHeader.MetadataSection.GetUnsafePtr();
        }

        /// <summary>
        /// Access to the root node of the file
        /// </summary>
        /// <remarks>
        /// The root is the top level node, the whole file content is store in its (in)direct children
        /// </remarks>
        public NodeHandle RootNode => new NodeHandle(this, -UnsafeUtility.SizeOf<DotsSerialization.NodeHeader>());
        public BinaryReader Reader => _reader;

        /// <summary>
        /// Free the reader, all memory associated to it will be released.
        /// </summary>
        public unsafe void Dispose()
        {
            Memory.Unmanaged.Free(_rootNodeHeader, Allocator.Persistent);
            Memory.Unmanaged.Free(_metadataSection, Allocator.Persistent);
        }

        /// <summary>
        /// Handle exposing a stream based access to the Node's Raw Data
        /// </summary>
        public struct ReaderHandle : IDisposable
        {
            private readonly unsafe byte* _buffer;
            private readonly MemoryBinaryReader _reader;

            internal unsafe ReaderHandle(NodeHandle node)
            {
                _buffer = (byte*)Memory.Unmanaged.Allocate(node.DataSize, 16, Allocator.TempJob);
                node.ReadData(_buffer);
                _reader = new MemoryBinaryReader(_buffer, node.DataSize);
            }

            /// <summary>
            /// Release the node being read and the memory associated with it
            /// </summary>
            public unsafe void Dispose()
            {
                Memory.Unmanaged.Free(_buffer, Allocator.TempJob);
                _reader.Dispose();
            }

            /// <summary>
            /// Access to the stream exposing the raw data associated with the node
            /// </summary>
            public BinaryReader Reader => _reader;
        }

        /// <summary>
        /// Handle manipulating a given node as part of the reader
        /// </summary>
        public struct NodeHandle
        {
            private readonly DotsSerializationReader _owner;
            private readonly int _nodeHeaderOffset;

            private unsafe byte* SectionBaseAddress => _owner._nodesSection;

            private unsafe ref DotsSerialization.NodeHeader GetHeader(int offset) => ref UnsafeUtility.AsRef<DotsSerialization.NodeHeader>(SectionBaseAddress+offset);
            public ref DotsSerialization.NodeHeader AsNodeHeader => ref GetHeader(_nodeHeaderOffset);

            /// <summary>
            ///  Determine if the handle is valid or not
            /// </summary>
            public bool IsValid => _owner != null;
            /// <summary>
            /// Count of direct children
            /// </summary>
            public int ChildrenCount => AsNodeHeader.ChildrenCount;
            /// <summary>
            /// Node type
            /// </summary>
            public ulong NodeTypeHash => AsNodeHeader.NodeTypeHash;
            /// <summary>
            /// .net type of the node
            /// </summary>
            public Type NodeDotNetType
            {
                get
                {
                    ref var h = ref AsNodeHeader;
                    return TypeManager.GetType(TypeManager.GetTypeIndexFromStableTypeHash(h.NodeTypeHash));
                }
            }

            /// <summary>
            /// Cast the node to its node header type
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <returns></returns>
            /// <exception cref="Exception"></exception>
            public unsafe ref T As<T>() where T : unmanaged
            {
                Assert.AreEqual(NodeDotNetType, typeof(T));
                return ref UnsafeUtility.AsRef<T>(SectionBaseAddress+_nodeHeaderOffset);
            }

            /// <summary>
            /// Enumerate the direct children of a given node
            /// </summary>
            /// <param name="node">Handle of the node to enumerate the children from</param>
            /// <returns>true if the NodeHandle is valid and the enumeration completed, false otherwise</returns>
            public bool MoveToNextChild(ref NodeHandle node)
            {
                // Default = no previous sibling = we have to return the first one
                if (node.IsValid == false)
                {
                    if (AsNodeHeader.ChildrenCount == 0)
                    {
                        return false;
                    }
                    node = new NodeHandle(_owner, _nodeHeaderOffset+AsNodeHeader.Size);
                    return true;
                }

                var nextSiblingOffset = node.AsNodeHeader.NextSiblingOffset;
                if (nextSiblingOffset == -1L)
                {
                    node = default;
                    return false;
                }
                node = new NodeHandle(_owner, nextSiblingOffset);
                return true;
            }

            /// <summary>
            /// Find a node from given criteria
            /// </summary>
            /// <param name="nestedLevel">The number of nested levels the search has to be performed, 1 for direct children, 2 for direct children and their direct children, etc</param>
            /// <typeparam name="T">Type of the node to find</typeparam>
            /// <returns>Return a valid Node Handle if found, an invalid one otherwise</returns>
            public NodeHandle FindNode<T>(int nestedLevel = Int32.MaxValue) where T : unmanaged
            {
                return FindByType(TypeManager.GetTypeInfo<T>().StableTypeHash, nestedLevel);
            }

            /// <summary>
            /// Access the Reader Handle for this node
            /// </summary>
            /// <returns>The reader handle</returns>
            public ReaderHandle GetReaderHandle()
            {
                return new ReaderHandle(this);
            }

            public struct PrefetchState : IDisposable
            {
                public unsafe void Dispose()
                {
                    Memory.Unmanaged.Free(_buffer, Allocator.Persistent);
                    _buffer = null;
                }

                [NativeDisableUnsafePtrRestriction]
                internal unsafe void* _buffer;
                internal long _size;

                public unsafe MemoryBinaryReader CreateStream()
                {
                    return new MemoryBinaryReader((byte*)_buffer, _size);
                }
            }

            internal unsafe PrefetchState PrefetchRawDataRead(out ReadCommand readCommand)
            {
                PrefetchState state = default;
                state._buffer = Memory.Unmanaged.Allocate(DataSize, 16, Allocator.Persistent);
                state._size = DataSize;

                ref var nodeHeader = ref AsNodeHeader;
                readCommand.Buffer = state._buffer;
                readCommand.Offset = nodeHeader.DataStartingOffset;
                readCommand.Size = state._size;

                return state;
            }

            private NodeHandle FindByType(ulong nodeType, int nestedLevel)
            {
                var child = default(NodeHandle);
                if (nestedLevel < 0)
                {
                    return child;
                }

                // First look in the current level
                while (MoveToNextChild(ref child))
                {
                    ref var h = ref child.AsNodeHeader;
                    if (h.NodeTypeHash == nodeType)
                    {
                        return new NodeHandle(_owner, child._nodeHeaderOffset);
                    }
                }

                // Couldn't find on the direct children: recurse, if we can
                if (nestedLevel == 0)
                {
                    return default;
                }

                child = default;
                while (MoveToNextChild(ref child))
                {
                    var res = child.FindByType(nodeType, nestedLevel - 1);
                    if (res.IsValid)
                    {
                        return res;
                    }
                }

                return default;
            }

            public bool TryFindNode(Hash128 id, out NodeHandle node, int nestedLevel = Int32.MaxValue)
            {
                node = FindNode(id, nestedLevel);
                return node.IsValid;
            }

            /// <summary>
            /// Find a node by id.
            /// </summary>
            /// <param name="id">The unique Id of the node</param>
            /// <param name="nestedLevel">The number of nested levels the search has to be performed, 1 for direct children, 2 for direct children and their direct children, etc</param>
            /// <returns>Valid node handle if found, invalid one otherwise</returns>
            public NodeHandle FindNode(Hash128 id, int nestedLevel = Int32.MaxValue)
            {
                var child = default(NodeHandle);
                if (nestedLevel <= 0)
                {
                    return child;
                }

                // First look in the current level
                while (MoveToNextChild(ref child))
                {
                    ref var h = ref child.AsNodeHeader;
                    if (h.Id == id)
                    {
                        return new NodeHandle(_owner, child._nodeHeaderOffset);
                    }
                }

                // Couldn't find on the direct children: recurse, if we can
                if (nestedLevel == 1)
                {
                    return default;
                }

                child = default;
                while (MoveToNextChild(ref child))
                {
                    var res = child.FindNode(id, nestedLevel - 1);
                    if (res.IsValid)
                    {
                        return res;
                    }
                }

                return default;
            }

            internal NodeHandle(DotsSerializationReader owner, int nodeHeaderOffset)
            {
                _owner = owner;
                _nodeHeaderOffset = nodeHeaderOffset;
            }

            /// <summary>
            /// Access the metadata associated to the node
            /// </summary>
            /// <typeparam name="T">Type of BlobAsset the metadata is stored with</typeparam>
            /// <returns>BlobAsset reference of the metadata</returns>
            public BlobAssetReference<T> GetMetadata<T>() where T : unmanaged
            {
                return _owner.GetMetadata<T>(this);
            }

            /// <summary>
            /// Determine if the node contains some metadata
            /// </summary>
            public bool HasMetadata => AsNodeHeader.MetadataStartingOffset != -1;

            /// <summary>
            /// Offset of the raw data segment in the file
            /// </summary>
            public long DataStartingOffset => AsNodeHeader.DataStartingOffset;

            /// <summary>
            /// Size of the raw data associated to the node
            /// </summary>
            public long DataSize => AsNodeHeader.DataSize;

            public unsafe bool ReadData(byte* buffer, long startingOffset = 0, long readSize = -1)
            {
                return _owner.ReadData(this, buffer, startingOffset, readSize);
            }
        }

        private unsafe bool ReadData(NodeHandle nodeHandle, byte* buffer, long startingOffset, long readSize)
        {
            if (readSize == -1)
            {
                readSize = nodeHandle.DataSize;
            }

            if (startingOffset + readSize > nodeHandle.DataSize)
            {
                return false;
            }

            ref var nodeHeader = ref nodeHandle.AsNodeHeader;
            _reader.Position = nodeHeader.DataStartingOffset + startingOffset;
            _reader.ReadBytes(buffer, (int)readSize);

            return true;
        }

        private unsafe BlobAssetReference<T> GetMetadata<T>(NodeHandle nodeHandle) where T : unmanaged
        {
            ref var header = ref nodeHandle.AsNodeHeader;
            if (!header.HasMetadata)
            {
                return default;
            }

            byte* data = _metadataSection + header.MetadataStartingOffset;
            var bufferHeader = (BlobAssetHeader*) data;
            bufferHeader->Allocator = Allocator.None;
            bufferHeader->ValidationPtr = data + sizeof(BlobAssetHeader);

            BlobAssetReference<T> blobAssetReference;
            blobAssetReference.m_data.m_Align8Union = 0;
            blobAssetReference.m_data.m_Ptr = data + sizeof(BlobAssetHeader);

            return blobAssetReference;
        }
    }

    // Allow to reserve memory segments and keep the address of the reserved region by avoiding a grow cycle that would re-alloc/copy/release-old in favor of a paged based allocation.
    // Why? Because we will need to allocate lots of little memory segment that will contain the Node's header data and we want the address to be immutable.
    // We can't rely on an UnsafeList<T> because the grow would invalidate all the segments that were allocated before it happens.
    // So we have a list of Pages, we fill the current page until it can't hold the new Reserve() to make and allocate a new page when it happens.
    // This way each address returned by Reserve() is immutable and can be used as long as the PagedAllocation instance is alive.
    // We also maintain a sense of a "global offset" to allow interpreting the data stored in this class in a contiguous, linear fashion.
    // Which is why we don't try to fit new Reserve in former pages, we only work on the current page (which is the last of the list).
    // Disposing an instance of this class will free all the allocated memory blocks.
    internal unsafe class PagedAllocation : IDisposable
    {
        public PagedAllocation(AllocatorManager.AllocatorHandle allocator, int pageSize = 65536)
        {
            PageSize = pageSize;
            Allocator = allocator;
            CurrentGlobalOffset = 0;
            Pages.Add(new PageInfo((byte*)Memory.Unmanaged.Allocate(PageSize,16, Allocator), 0));
        }

        public byte* Reserve(int size, bool clearMemory=true)
        {
            CurrentGlobalOffset += size;

            byte* buffer;
            // Dedicate page allocation if it can't fit into a single page
            if (size > PageSize)
            {
                buffer = (byte*)Memory.Unmanaged.Allocate(size, 16, Allocator);

                // The previous page might be as filled as...empty, but so be it, this code path is not supposed to happen must and we absolutely need to maintain a Global Offset
                Pages.Add(new PageInfo(buffer, size));
            } else {
                // Reserve to current page, if possible
                var page = &Pages.Ptr[Pages.Length - 1];

                // Check if the memory to reserve fit in the current page
                // Yes, maybe a previous page could fit the data but we need to maintain a Global Offset.
                if ((page->FreeOffset + size) <= PageSize)
                {
                    buffer = page->Buffer + page->FreeOffset;
                    page->FreeOffset += size;
                }
                else
                {
                    // Allocate a new page that will contains the block to reserve because it couldn't fit in the previous page
                    buffer = (byte*) Memory.Unmanaged.Allocate(PageSize, 16, Allocator);
                    var pi = new PageInfo(buffer, size);
                    Pages.Add(pi);
                }
            }

            if (clearMemory)
            {
                UnsafeUtility.MemClear(buffer, size);
            }

            return buffer;
        }

        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            for (int i = 0; i < Pages.Length; i++)
            {
                Memory.Unmanaged.Free(Pages[i].Buffer, Allocator);
            }
            Pages.Dispose();
            IsDisposed = true;
        }

        public ref UnsafeList<PageInfo> Pages => ref _pages;

        public int PageSize { get; }
        public AllocatorManager.AllocatorHandle Allocator { get; }
        public int CurrentGlobalOffset { get; private set; }
        public bool IsDisposed { get; private set; }

        private UnsafeList<PageInfo> _pages = new UnsafeList<PageInfo>(16, Unity.Collections.Allocator.Persistent);

        public struct PageInfo
        {
            public PageInfo(byte* buffer, int freeOffset)
            {
                FreeOffset = freeOffset;
                Buffer = buffer;
            }

            public int FreeOffset;
            public byte* Buffer;
        }
    }
}
