using System;

namespace Unity.DataModel
{
    internal enum TraversalControl
    {
        Continue,
        StopCurrentLevel,
        StopAll
    }

    internal static class Traversal
    {
        internal static TraversalControl TraverseVector(ref Accessor accessor, Func<Accessor, TraversalControl> operation)
        {
            var vector = accessor.GetVectorValue();
            for (ulong i = 0; i < vector.GetLength(); i++)
            {
                var elementAccessor = vector.ElementAt(i);
                var control = TraverseAccessor(ref elementAccessor, operation);
                if (control == TraversalControl.StopAll)
                {
                    return TraversalControl.StopAll;
                }
            }

            return TraversalControl.Continue;
        }

        internal static TraversalControl TraverseFields(ref Accessor accessor, Func<Accessor, TraversalControl> operation)
        {
            var schema = accessor.GetSchema();
            var fields = schema.GetFields();
            for (ulong i = 0; i < schema.GetFieldCount(); i++)
            {
                var field = fields.GetFieldByIndex(i);
                var fieldAccessor = accessor.GetFieldAccessor(field);
                if (fieldAccessor.IsValid())
                {
                    var control = TraverseAccessor(ref fieldAccessor, operation);
                    if (control == TraversalControl.StopAll)
                    {
                        return TraversalControl.StopAll;
                    }
                }
            }

            return TraversalControl.Continue;
        }

        internal static TraversalControl TraverseAccessor(ref Accessor accessor, Func<Accessor, TraversalControl> operation)
        {
            var control = operation(accessor);
            if (control == TraversalControl.StopAll)
            {
                return TraversalControl.StopAll;
            }
            else if (control == TraversalControl.StopCurrentLevel)
            {
                return TraversalControl.Continue;
            }

            var schema = accessor.GetSchema();
            var schemaFlags = schema.GetFlags();

            if (schemaFlags.HasFlag(SchemaFlags.IsVector))
            {
                return TraverseVector(ref accessor, operation);
            }
            else if (!schemaFlags.HasFlag(SchemaFlags.IsBasic))
            {
                return TraverseFields(ref accessor, operation);
            }

            return TraversalControl.Continue;
        }

        internal static void TraverseReferences(ref Accessor accessor, Action<Accessor> operation)
        {
            // TODO cache the offsets of references so we don't need to traverse the schema
            Traversal.TraverseAccessor(ref accessor, currentAccessor =>
            {
                var schema = currentAccessor.GetSchema();
                var schemaFlags = schema.GetFlags();

                if (!schemaFlags.HasFlag(SchemaFlags.HasReferenceFields) && !schema.IsReference())
                {
                    return TraversalControl.StopCurrentLevel;
                }

                if (schema.IsReference())
                {
                    operation(currentAccessor);
                }

                return TraversalControl.Continue;
            });
        }

        internal static TraversalControl TraverseVector(ref ConstAccessor ConstAccessor, Func<ConstAccessor, TraversalControl> operation)
        {
            var vector = ConstAccessor.GetVectorValue();
            for (ulong i = 0; i < vector.GetLength(); i++)
            {
                var elementConstAccessor = vector.ElementAt(i);
                var control = TraverseConstAccessor(ref elementConstAccessor, operation);
                if (control == TraversalControl.StopAll)
                {
                    return TraversalControl.StopAll;
                }
            }

            return TraversalControl.Continue;
        }

        internal static TraversalControl TraverseFields(ref ConstAccessor ConstAccessor, Func<ConstAccessor, TraversalControl> operation)
        {
            var schema = ConstAccessor.GetSchema();
            var fields = schema.GetFields();
            for (ulong i = 0; i < schema.GetFieldCount(); i++)
            {
                var field = fields.GetFieldByIndex(i);
                var fieldConstAccessor = ConstAccessor.GetFieldAccessor(field);
                if (fieldConstAccessor.IsValid())
                {
                    var control = TraverseConstAccessor(ref fieldConstAccessor, operation);
                    if (control == TraversalControl.StopAll)
                    {
                        return TraversalControl.StopAll;
                    }
                }
            }

            return TraversalControl.Continue;
        }

        internal static TraversalControl TraverseConstAccessor(ref ConstAccessor ConstAccessor, Func<ConstAccessor, TraversalControl> operation)
        {
            var control = operation(ConstAccessor);
            if (control == TraversalControl.StopAll)
            {
                return TraversalControl.StopAll;
            }
            else if (control == TraversalControl.StopCurrentLevel)
            {
                return TraversalControl.Continue;
            }

            var schema = ConstAccessor.GetSchema();
            var schemaFlags = schema.GetFlags();

            if (schemaFlags.HasFlag(SchemaFlags.IsVector))
            {
                return TraverseVector(ref ConstAccessor, operation);
            }
            else if (!schemaFlags.HasFlag(SchemaFlags.IsBasic))
            {
                return TraverseFields(ref ConstAccessor, operation);
            }

            return TraversalControl.Continue;
        }

        internal static void TraverseReferences(ref ConstAccessor ConstAccessor, Action<ConstAccessor> operation)
        {
            // TODO cache the offsets of references so we don't need to traverse the schema
            Traversal.TraverseConstAccessor(ref ConstAccessor, currentConstAccessor =>
            {
                var schema = currentConstAccessor.GetSchema();
                var schemaFlags = schema.GetFlags();

                if (!schemaFlags.HasFlag(SchemaFlags.HasReferenceFields) && !schema.IsReference())
                {
                    return TraversalControl.StopCurrentLevel;
                }

                if (schema.IsReference())
                {
                    operation(currentConstAccessor);
                }

                return TraversalControl.Continue;
            });
        }

        internal static TraversalControl ParallelTraverseVectors(ref ConstAccessor lhsAccessor, ref ConstAccessor rhsAccessor, Func<ConstAccessor, ConstAccessor, TraversalControl> operation)
        {
            var lhsVector = lhsAccessor.GetVectorValue();
            var rhsVector = rhsAccessor.GetVectorValue();

            if (lhsVector.GetLength() != rhsVector.GetLength())
            {
                throw new InvalidOperationException("Size mismatch");
            }

            for (ulong i = 0; i < lhsVector.GetLength(); i++)
            {
                var lhsElement = lhsVector.ElementAt(i);
                var rhsElement = rhsVector.ElementAt(i);

                var control = ParallelTraverseAccessors(ref lhsElement, ref rhsElement, operation);
                if (control == TraversalControl.StopAll)
                {
                    return TraversalControl.StopAll;
                }
            }

            return TraversalControl.Continue;
        }

        internal static TraversalControl ParallelTraverseFields(ref ConstAccessor lhsAccessor, ref ConstAccessor rhsAccessor, Func<ConstAccessor, ConstAccessor, TraversalControl> operation)
        {
            var schema = lhsAccessor.GetSchema();
            var fields = schema.GetFields();

            for (ulong i = 0; i < schema.GetFieldCount(); i++)
            {
                var field = fields.GetFieldByIndex(i);

                var lhsFieldAccessor = lhsAccessor.GetFieldAccessor(field);
                var rhsFieldAccessor = rhsAccessor.GetFieldAccessor(field);

                var control = ParallelTraverseAccessors(ref lhsFieldAccessor, ref rhsFieldAccessor, operation);
                if (control == TraversalControl.StopAll)
                {
                    return TraversalControl.StopAll;
                }
            }

            return TraversalControl.Continue;
        }

        internal static TraversalControl ParallelTraverseAccessors(ref ConstAccessor lhsAccessor, ref ConstAccessor rhsAccessor, Func<ConstAccessor, ConstAccessor, TraversalControl> operation)
        {
            if (lhsAccessor.GetSchema() != rhsAccessor.GetSchema())
            {
                throw new InvalidOperationException("Schema mismatch");
            }

            var control = operation(lhsAccessor, rhsAccessor);
            if (control == TraversalControl.StopAll)
            {
                return TraversalControl.StopAll;
            }
            else if (control == TraversalControl.StopCurrentLevel)
            {
                return TraversalControl.Continue;
            }

            var schema = lhsAccessor.GetSchema();
            var schemaFlags = schema.GetFlags();

            if (schemaFlags.HasFlag(SchemaFlags.IsVector))
            {
                return ParallelTraverseVectors(ref lhsAccessor, ref rhsAccessor, operation);
            }
            else if (!schemaFlags.HasFlag(SchemaFlags.IsBasic))
            {
                return ParallelTraverseFields(ref lhsAccessor, ref rhsAccessor, operation);
            }

            return TraversalControl.Continue;
        }
    }
}
