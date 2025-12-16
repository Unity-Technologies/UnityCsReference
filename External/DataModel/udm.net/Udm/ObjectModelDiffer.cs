#nullable enable
using System;
using System.Collections.Generic;

namespace Unity.DataModel
{
    internal static class ObjectModelDiffer
    {
        internal enum ObjectModelDifferenceType
        {
            Default,
            DifferentSchemaType,
            DifferentValueType
        }

        internal class ObjectModelDifferReport
        {
            internal struct ObjectModelSingleDifference
            {
                internal ObjectModelDifferenceType type;
                internal string message;
            }

            internal new string ToString()
            {
                string res = $"Found {entries.Count} difference(s)/error(s):\n";
                for (int i = 0; i < entries.Count; i++)
                {
                    res += $"Difference/Error type: {entries[i].type}, \nMessage: {entries[i].message}\n";
                }
                return res;
            }
            internal List<ObjectModelSingleDifference> entries = new List<ObjectModelSingleDifference>();
        }

        private unsafe static bool MemCmp(void* data1, void* data2, long size)
        {

            byte* data1Byte = (byte*)data1;
            byte* data2Byte = (byte*)data2;
            for (long index = 0; index < size; ++index)
            {
                if (data1Byte[index] != data2Byte[index])
                    return true;
            }
            // MemCmp return false when the data is equal, true otherwise
            return false;
        }

        private static TraversalControl DifferBetweenTwoObjectModelsRecursive(
            DocumentModel sourceDocument,
            DocumentModel targetDocument,
            string fieldName,
            ConstAccessor currentSrcAccessor,
            ConstAccessor currentTargetAccessor,
            ref ObjectModelDifferReport report)
        {
            var srcSchema = currentSrcAccessor.GetSchema();
            var srcSchemaFlags = srcSchema.GetFlags();

            if (srcSchemaFlags.HasFlag(SchemaFlags.IsBasic))
            {
                if (srcSchemaFlags.HasFlag(SchemaFlags.IsFundamental))
                {
                    ulong srcSize = currentSrcAccessor.Schema.GetSize();
                    unsafe
                    {
                        if (MemCmp((void*)currentSrcAccessor.Data, (void*)currentTargetAccessor.Data, (long)srcSize))
                        {
                            report.entries.Add(new ObjectModelDifferReport.ObjectModelSingleDifference
                            {
                                type = ObjectModelDifferenceType.DifferentValueType,
                                message = $"A fundamental value differs between the source and target for the fundamental field: {fieldName}."
                            });
                        }
                    }
                }
                else if (srcSchema.IsReference())
                {
                    Reference srcReference = currentSrcAccessor.GetReferenceValue();
                    Reference targetReference = currentTargetAccessor.GetReferenceValue();

                    if (srcReference.DocumentId != targetReference.DocumentId)
                    {
                        report.entries.Add(new ObjectModelDifferReport.ObjectModelSingleDifference
                        {
                            type = ObjectModelDifferenceType.DifferentValueType,
                            message = $"The references of the source and the target for the field: {fieldName} are in different documents. Source DocumentModelID {srcReference.DocumentId}, Target DocumentModelID {targetReference.DocumentId}."
                        });
                    }

                    if (srcReference.UdmObjectId.Id != targetReference.UdmObjectId.Id)
                    {
                        report.entries.Add(new ObjectModelDifferReport.ObjectModelSingleDifference
                        {
                            type = ObjectModelDifferenceType.DifferentValueType,
                            message = $"The references of the source and the target for the field: {fieldName} are different object models. Source ObjectModelID {srcReference.UdmObjectId}, Target ObjectModelID {targetReference.UdmObjectId}."
                        });
                    }
                }
                else if (srcSchema.IsUTF8String())
                {
                    string srcString = currentSrcAccessor.GetUTF8StringValue().ToString();
                    string targetString = currentTargetAccessor.GetUTF8StringValue().ToString();

                    if (srcString != targetString)
                    {
                        report.entries.Add(new ObjectModelDifferReport.ObjectModelSingleDifference
                        {
                            type = ObjectModelDifferenceType.DifferentValueType,
                            message = $"A UTF8String Value differs between the source and target for the field {fieldName}. Source: {srcString}, target: {targetString}."
                        });
                    }
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            else if (srcSchemaFlags.HasFlag(SchemaFlags.IsVector))
            {
                var srcVector = currentSrcAccessor.GetVectorValue();
                var targetVector = currentTargetAccessor.GetVectorValue();

                ulong srcVectorLength = srcVector.GetLength();
                ulong targetVectorLength = targetVector.GetLength();

                if (srcVectorLength != targetVectorLength)
                {
                    report.entries.Add(new ObjectModelDifferReport.ObjectModelSingleDifference
                    {
                        type = ObjectModelDifferenceType.DifferentValueType,
                        message = $"The number of elements in a vector between the source and the target for the field {fieldName} is different. The source has {srcVectorLength} elements and the target {targetVectorLength} elements."
                    });

                    return TraversalControl.StopCurrentLevel;
                }
            }

            return TraversalControl.Continue;
        }

        /// <summary>
        /// Returns a list of differences (in schema types, fundamentals, strings, References, and vector values) between 2 Object models
        /// </summary>
        /// <param name="sourceDocument"></param>
        /// <param name="source"></param>
        /// <param name="targetDocument"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        internal static ObjectModelDifferReport DifferBetweenTwoObjectModels(DocumentModel sourceDocument, ConstObjectModel source, DocumentModel targetDocument, ConstObjectModel target)
        {
            var report = new ObjectModelDifferReport();
            var sourceAccessor = source.GetAccessor();
            var targetAccessor = target.GetAccessor();

            var sourceSchema = sourceAccessor.GetSchema();
            var targetSchema = targetAccessor.GetSchema();

            // Are they the same type?
            if (sourceSchema != targetSchema)
            {
                // If not, exit and report different object types
                report.entries.Add(new ObjectModelDifferReport.ObjectModelSingleDifference
                {
                    type = ObjectModelDifferenceType.DifferentSchemaType,
                    message = $"The schema type between the source and target is different. Source type: {sourceSchema.GetTypeName()}, target type: {targetSchema.GetTypeName()}"
                });
            }
            else
            {
                SchemaFields fields = sourceSchema.GetFields();
                for (ulong i = 0ul; i < sourceSchema.GetFieldCount(); i++)
                {
                    var field = fields.GetFieldByIndex(i);
                    var srcfieldAccessor = sourceAccessor.GetFieldAccessor(field);
                    var targetfieldAccessor = targetAccessor.GetFieldAccessor(field);
                    Traversal.ParallelTraverseAccessors(ref srcfieldAccessor, ref targetfieldAccessor, (currentSrcAccessor, currentTargetAccessor) =>
                    {
                        return DifferBetweenTwoObjectModelsRecursive(sourceDocument, targetDocument, field.GetName().ToString(), currentSrcAccessor, currentTargetAccessor, ref report);
                    });
                }
            }
            return report;
        }
    }
}
