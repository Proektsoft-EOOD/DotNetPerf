using System;

namespace DotNetPref.PointSearch
{
    public static class PointListSearch
    {
        internal static int Search(Point3D point, Span<Point3D> inPoints)
        {
            int best = -1;
            double bestDistance = 0;
            for (int i = 0, len = inPoints.Length; i < len; ++i)
                PointMatch.Consider(i, point, inPoints, ref best, ref bestDistance);

            return best;
        }
    }
}
