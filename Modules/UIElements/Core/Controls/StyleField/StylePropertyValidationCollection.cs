// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
    [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
    internal readonly partial struct StylePropertyValidationCollection : IEnumerable<StylePropertyValidation>
    {
        public static implicit operator StylePropertyValidationCollection(List<StylePropertyValidation> validation)
        {
            return new StylePropertyValidationCollection(validation);
        }

        internal struct Enumerator : IEnumerator<StylePropertyValidation>
        {
            List<StylePropertyValidation>.Enumerator m_PersistentValidation;
            List<StylePropertyValidation>.Enumerator m_Validation;

            private bool persistent;

            public StylePropertyValidation Current { get; private set; }

            object IEnumerator.Current => Current;

            internal Enumerator(
                List<StylePropertyValidation>.Enumerator persistentValidation,
                List<StylePropertyValidation>.Enumerator validation
                )
            {
                m_PersistentValidation = persistentValidation;
                m_Validation = validation;
                Current = null;
                persistent = true;
            }

            public bool MoveNext()
            {
                bool result;

                if (persistent)
                {
                    result = m_PersistentValidation.MoveNext();
                    Current = m_PersistentValidation.Current;

                    if (!result)
                    {
                        persistent = false;
                        result = m_Validation.MoveNext();
                        Current = m_Validation.Current;
                    }
                }
                else
                {
                    result = m_Validation.MoveNext();
                    Current = m_Validation.Current;
                }

                return result;
            }

            /// <summary>
            /// Sets the enumerator to its initial position, which is before the first element in the collection.
            /// </summary>
            public void Reset()
            {
                persistent = true;
                Current = null;
                ((IEnumerator) m_PersistentValidation).Reset();
                ((IEnumerator) m_Validation).Reset();
            }

            /// <summary>
            /// Disposes of the underlying enumerator.
            /// </summary>
            public void Dispose()
            {
                // If we try to invoke the dispose call here we incur a boxing cost.
                // Fortunately List<T>.Enumerator has no dispose implementation.
            }
        }

        private static readonly List<StylePropertyValidation> s_Empty = new();
        readonly List<StylePropertyValidation> m_PersistentValidation;
        readonly List<StylePropertyValidation> m_Validation;

        public static StylePropertyValidationCollection Empty { get; } = new ();

        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal StylePropertyValidationCollection(
            List<StylePropertyValidation> persistentValidation,
            List<StylePropertyValidation> validation
            )
        {
            m_PersistentValidation = persistentValidation;
            m_Validation = validation;
        }

        internal StylePropertyValidationCollection(
            List<StylePropertyValidation> validation
        )
        {
            m_PersistentValidation = null;
            m_Validation = validation;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        public Enumerator GetEnumerator()
        {
            return m_PersistentValidation != null
                ? new Enumerator(m_PersistentValidation.GetEnumerator(), m_Validation.GetEnumerator())
                : new Enumerator(s_Empty.GetEnumerator(), m_Validation.GetEnumerator());
        }

        /// <inheritdoc/>
        IEnumerator<StylePropertyValidation> IEnumerable<StylePropertyValidation>.GetEnumerator()
            => GetEnumerator();

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }
}
