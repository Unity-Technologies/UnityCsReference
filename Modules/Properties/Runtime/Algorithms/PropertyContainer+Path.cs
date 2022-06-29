// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.Properties
{
    public static partial class PropertyContainer
    {
        class ValueAtPathVisitor : PathVisitor
        {
            public static readonly UnityEngine.Pool.ObjectPool<ValueAtPathVisitor> Pool = new UnityEngine.Pool.ObjectPool<ValueAtPathVisitor>(() => new ValueAtPathVisitor(), null, v => v.Reset());
            public IPropertyVisitor Visitor;

            public override void Reset()
            {
                base.Reset();
                Visitor = default;
                ReadonlyVisit = true;
            }

            protected override void VisitPath<TContainer, TValue>(Property<TContainer, TValue> property,
                ref TContainer container, ref TValue value)
            {
                ((IPropertyAccept<TContainer>) property).Accept(Visitor, ref container);
            }
        }

        class ExistsAtPathVisitor : PathVisitor
        {
            public static readonly UnityEngine.Pool.ObjectPool<ExistsAtPathVisitor> Pool = new UnityEngine.Pool.ObjectPool<ExistsAtPathVisitor>(() => new ExistsAtPathVisitor(), null, v => v.Reset());
            public bool Exists;

            public override void Reset()
            {
                base.Reset();
                Exists = default;
                ReadonlyVisit = true;
            }

            protected override void VisitPath<TContainer, TValue>(Property<TContainer, TValue> property, ref TContainer container, ref TValue value)
            {
                Exists = true;
            }
        }


        /// <summary>
        /// Returns <see langword="true"/> if a property exists at the specified <see cref="PropertyPath"/>.
        /// </summary>
        /// <param name="container">The container tree to search.</param>
        /// <param name="path">The property path to resolve.</param>
        /// <typeparam name="TContainer">The container type.</typeparam>
        /// <returns><see langword="true"/> if a property can be found at path.</returns>
        public static bool IsPathValid<TContainer>(TContainer container, string path)
            => IsPathValid(ref container, new PropertyPath(path));

        /// <summary>
        /// Returns <see langword="true"/> if a property exists at the specified <see cref="PropertyPath"/>.
        /// </summary>
        /// <param name="container">The container tree to search.</param>
        /// <param name="path">The property path to resolve.</param>
        /// <typeparam name="TContainer">The container type.</typeparam>
        /// <returns><see langword="true"/> if a property can be found at path.</returns>
        public static bool IsPathValid<TContainer>(TContainer container, in PropertyPath path)
            => IsPathValid(ref container, path);

        /// <summary>
        /// Returns <see langword="true"/> if a property exists at the specified <see cref="PropertyPath"/>.
        /// </summary>
        /// <param name="container">The container tree to search.</param>
        /// <param name="path">The property path to resolve.</param>
        /// <typeparam name="TContainer">The container type.</typeparam>
        /// <returns><see langword="true"/> if a property can be found at path.</returns>
        public static bool IsPathValid<TContainer>(ref TContainer container, string path)
        {
            var visitor = ExistsAtPathVisitor.Pool.Get();
            try
            {
                visitor.Path = new PropertyPath(path);
                TryAccept(visitor, ref container);
                return visitor.Exists;
            }
            finally
            {
                ExistsAtPathVisitor.Pool.Release(visitor);
            }
        }

        /// <summary>
        /// Returns <see langword="true"/> if a property exists at the specified <see cref="PropertyPath"/>.
        /// </summary>
        /// <param name="container">The container tree to search.</param>
        /// <param name="path">The property path to resolve.</param>
        /// <typeparam name="TContainer">The container type.</typeparam>
        /// <returns><see langword="true"/> if a property can be found at path.</returns>
        public static bool IsPathValid<TContainer>(ref TContainer container, in PropertyPath path)
        {
            var visitor = ExistsAtPathVisitor.Pool.Get();
            try
            {
                visitor.Path = path;
                TryAccept(visitor, ref container);
                return visitor.Exists;
            }
            finally
            {
                ExistsAtPathVisitor.Pool.Release(visitor);
            }
        }
    }
}
