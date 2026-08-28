using System;
using System.Runtime.CompilerServices;

namespace DotNetPref.PointSearch
{
    // Matches the closest point within a Tol distance, or returns -1, if outside
    internal static class PointMatch
    {
        internal static double Tol = 0.01;

        //L1 norm distance, or -1, if outside
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static double Distance(in Point3D a, in Point3D b)
        {
            var dx = Math.Abs(a.X - b.X);
            if (dx >= Tol)
                return -1;

            var dy = Math.Abs(a.Y - b.Y);
            if (dy >= Tol)
                return -1;

            var dz = Math.Abs(a.Z - b.Z);
            if (dz >= Tol)
                return -1;

            return dx + dy + dz;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Consider(int index, in Point3D point, Span<Point3D> inPoints,
            ref int best, ref double bestDistance)
        {
            var distance = Distance(point, inPoints[index]);
            if (distance < 0)
                return;

            if (best < 0 || distance < bestDistance || (distance == bestDistance && index < best))
            {
                best = index;
                bestDistance = distance;
            }
        }
    }
}
