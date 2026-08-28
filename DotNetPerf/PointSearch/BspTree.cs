using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace DotNetPref.PointSearch
{
    // A BSP tree, stored in flat arrays.
    // Every point becomes exactly one node, so the node id is the point index 
    // and no per-node object, List or int[] is ever allocated.
    // Child links are stored biased by +1 so that the zero-initialized array 
    // already means "no child" and no fill pass is needed.
    public sealed class BspTree
    {
        private readonly double[] _div;     // split coordinate of the node, along its own axis
        private readonly int[] _left;       // child index + 1, 0 = none
        private readonly int[] _right;      // child index + 1, 0 = none
        private int[] _next;                // chain of points sharing a node, index + 1, 0 = end. Allocated on demand
        private readonly int _count;
        private int _root;                  // root node index + 1, 0 = empty tree

        private BspTree(int count)
        {
            _count = count;
            _div = new double[count];
            _left = new int[count];
            _right = new int[count];
        }

        internal static BspTree Fill(List<Point3D> points)
        {
            var span = CollectionsMarshal.AsSpan(points);
            var tree = new BspTree(span.Length);
            if (span.Length == 0)
                return tree;

            var div = tree._div;
            var left = tree._left;
            var right = tree._right;
            tree._root = 1;
            div[0] = span[0][0];
            for (int i = 1; i < span.Length; ++i)
            {
                var point = span[i];
                int node = 0, dir = 0;
                while (true)
                {
                    var d = div[node];
                    var c = point[dir];
                    var nextDir = dir == 2 ? 0 : dir + 1;
                    if (c < d - PointMatch.Tol)
                    {
                        var child = left[node];
                        if (child == 0)
                        {
                            left[node] = i + 1;
                            div[i] = point[nextDir];
                            break;
                        }
                        node = child - 1;
                    }
                    else if (c > d + PointMatch.Tol)
                    {
                        var child = right[node];
                        if (child == 0)
                        {
                            right[node] = i + 1;
                            div[i] = point[nextDir];
                            break;
                        }
                        node = child - 1;
                    }
                    else
                    {
                        tree.Chain(node, i);
                        break;
                    }
                    dir = nextDir;
                }
            }
            return tree;
        }

        internal int Search(Point3D point, Span<Point3D> inPoints)
        {
            int best = -1;
            double bestDistance = 0;
            if (_root == 0)
                return best;

            var band = 2 * PointMatch.Tol;
            int node = _root - 1, dir = 0;
            while (true)
            {
                var delta = point[dir] - _div[node];
                if (delta < band && delta > -band)
                    SearchNode(node, point, inPoints, ref best, ref bestDistance);

                int child;
                if (delta < 0)
                    child = _left[node];
                else if (delta > 0)
                    child = _right[node];
                else
                    break;      //neither side can hold a candidate

                if (child == 0)
                    break;

                node = child - 1;
                dir = dir == 2 ? 0 : dir + 1;
            }
            return best;
        }

        private void SearchNode(int node, Point3D point, Span<Point3D> inPoints,
            ref int best, ref double bestDistance)
        {
            var next = _next;
            var i = node;
            while (true)
            {
                PointMatch.Consider(i, point, inPoints, ref best, ref bestDistance);
                if (next is null)
                    return;

                var link = next[i];
                if (link == 0)
                    return;

                i = link - 1;
            }
        }

        private void Chain(int node, int index)
        {
            _next ??= new int[_count];
            _next[index] = _next[node];
            _next[node] = index + 1;
        }
    }
}
