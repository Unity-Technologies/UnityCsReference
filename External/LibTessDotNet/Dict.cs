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

namespace LibTessDotNet
{
    internal class Dict<TValue> where TValue : class
    {
        public class Node
        {
            internal TValue _key;
            internal Node _prev, _next;

            public TValue Key { get { return _key; } }
            public Node Prev { get { return _prev; } }
            public Node Next { get { return _next; } }
        }

        public delegate bool LessOrEqual(TValue lhs, TValue rhs);

        private LessOrEqual _leq;
        Node _head;

        public Dict(LessOrEqual leq)
        {
            _leq = leq;

            _head = new Node { _key = null };
            _head._prev = _head;
            _head._next = _head;
        }

        public Node Insert(TValue key)
        {
            return InsertBefore(_head, key);
        }

        public Node InsertBefore(Node node, TValue key)
        {
            do {
                node = node._prev;
            } while (node._key != null && !_leq(node._key, key));

            var newNode = new Node { _key = key };
            newNode._next = node._next;
            node._next._prev = newNode;
            newNode._prev = node;
            node._next = newNode;

            return newNode;
        }

        public Node Find(TValue key)
        {
            var node = _head;
            do {
                node = node._next;
            } while (node._key != null && !_leq(key, node._key));
            return node;
        }

        public Node Min()
        {
            return _head._next;
        }

        public void Remove(Node node)
        {
            node._next._prev = node._prev;
            node._prev._next = node._next;
        }
    }
}
