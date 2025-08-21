/*
** SGI FREE SOFTWARE LICENSE B (Version 2.0, Sept. 18, 2008) 
** Copyright (C) 2011 Silicon Graphics, Inc.
** All Rights Reserved.
**
** Permission is hereby granted, free of charge, to any person obtaining a copy
** of this software and associated documentation files (the "Software"), to deal
** in the Software without restriction, including without limitation the rights
** to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
** of the Software, and to permit persons to whom the Software is furnished to do so,
** subject to the following conditions:
** 
** The above copyright notice including the dates of first publication and either this
** permission notice or a reference to http://oss.sgi.com/projects/FreeB/ shall be
** included in all copies or substantial portions of the Software. 
**
** THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
** INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A
** PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL SILICON GRAPHICS, INC.
** BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
** TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE
** OR OTHER DEALINGS IN THE SOFTWARE.
** 
** Except as contained in this notice, the name of Silicon Graphics, Inc. shall not
** be used in advertising or otherwise to promote the sale, use or other dealings in
** this Software without prior written authorization from Silicon Graphics, Inc.
*/
/*
** Original Author: Eric Veach, July 1994.
** libtess2: Mikko Mononen, http://code.google.com/p/libtess2/.
** LibTessDotNet: Remi Gillig, https://github.com/speps/LibTessDotNet
*/

using System;
using System.Diagnostics;

namespace LibTessDotNet
{
    internal struct PQHandle
    {
        public static readonly int Invalid = 0x0fffffff;
        internal int _handle;
    }

    internal class PriorityHeap<TValue> where TValue : class
    {
        public delegate bool LessOrEqual(TValue lhs, TValue rhs);

        protected class HandleElem
        {
            internal TValue _key;
            internal int _node;
        }

        private LessOrEqual _leq;
        private int[] _nodes;
        private HandleElem[] _handles;
        private int _size, _max;
        private int _freeList;
        private bool _initialized;

        public bool Empty { get { return _size == 0; } }

        public PriorityHeap(int initialSize, LessOrEqual leq)
        {
            _leq = leq;

            _nodes = new int[initialSize + 1];
            _handles = new HandleElem[initialSize + 1];

            _size = 0;
            _max = initialSize;
            _freeList = 0;
            _initialized = false;

            _nodes[1] = 1;
            _handles[1] = new HandleElem { _key = null };
        }

        private void FloatDown(int curr)
        {
            int child;
            int hCurr, hChild;

            hCurr = _nodes[curr];
            while (true)
            {
                child = curr << 1;
                if (child < _size && _leq(_handles[_nodes[child + 1]]._key, _handles[_nodes[child]]._key))
                {
                    ++child;
                }

                Debug.Assert(child <= _max);

                hChild = _nodes[child];
                if (child > _size || _leq(_handles[hCurr]._key, _handles[hChild]._key))
                {
                    _nodes[curr] = hCurr;
                    _handles[hCurr]._node = curr;
                    break;
                }

                _nodes[curr] = hChild;
                _handles[hChild]._node = curr;
                curr = child;
            }
        }

        private void FloatUp(int curr)
        {
            int parent;
            int hCurr, hParent;

            hCurr = _nodes[curr];
            while (true)
            {
                parent = curr >> 1;
                hParent = _nodes[parent];
                if (parent == 0 || _leq(_handles[hParent]._key, _handles[hCurr]._key))
                {
                    _nodes[curr] = hCurr;
                    _handles[hCurr]._node = curr;
                    break;
                }
                _nodes[curr] = hParent;
                _handles[hParent]._node = curr;
                curr = parent;
            }
        }

        public void Init()
        {
            for (int i = _size; i >= 1; --i)
            {
                FloatDown(i);
            }
            _initialized = true;
        }

        public PQHandle Insert(TValue value)
        {
            int curr = ++_size;
            if ((curr * 2) > _max)
            {
                _max <<= 1;
                Array.Resize(ref _nodes, _max + 1);
                Array.Resize(ref _handles, _max + 1);
            }

            int free;
            if (_freeList == 0)
            {
                free = curr;
            }
            else
            {
                free = _freeList;
                _freeList = _handles[free]._node;
            }

            _nodes[curr] = free;
            if (_handles[free] == null)
            {
                _handles[free] = new HandleElem { _key = value, _node = curr };
            }
            else
            {
                _handles[free]._node = curr;
                _handles[free]._key = value;
            }

            if (_initialized)
            {
                FloatUp(curr);
            }

            Debug.Assert(free != PQHandle.Invalid);
            return new PQHandle { _handle = free };
        }

        public TValue ExtractMin()
        {
            Debug.Assert(_initialized);

            int hMin = _nodes[1];
            TValue min = _handles[hMin]._key;

            if (_size > 0)
            {
                _nodes[1] = _nodes[_size];
                _handles[_nodes[1]]._node = 1;

                _handles[hMin]._key = null;
                _handles[hMin]._node = _freeList;
                _freeList = hMin;

                if (--_size > 0)
                {
                    FloatDown(1);
                }
            }

            return min;
        }

        public TValue Minimum()
        {
            Debug.Assert(_initialized);
            return _handles[_nodes[1]]._key;
        }

        public void Remove(PQHandle handle)
        {
            Debug.Assert(_initialized);

            int hCurr = handle._handle;
            Debug.Assert(hCurr >= 1 && hCurr <= _max && _handles[hCurr]._key != null);

            int curr = _handles[hCurr]._node;
            _nodes[curr] = _nodes[_size];
            _handles[_nodes[curr]]._node = curr;

            if (curr <= --_size)
            {
                if (curr <= 1 || _leq(_handles[_nodes[curr >> 1]]._key, _handles[_nodes[curr]]._key))
                {
                    FloatDown(curr);
                }
                else
                {
                    FloatUp(curr);
                }
            }

            _handles[hCurr]._key = null;
            _handles[hCurr]._node = _freeList;
            _freeList = hCurr;
        }
    }
}
