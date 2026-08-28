using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DotNetPref.PointSearch
{
    // Spatial hash grid.
    // Points are bucketed into cubic cells of side cellFactor * PointMatch.Tol,
    // keyed by the hash of their integer cell coordinates.
    public sealed class PointHashGrid
    {
        private readonly double _inv;   //1 / cell size
        private readonly int _mask;     //bucket count - 1, bucket count is a power of two
        private readonly int[] _heads;  //point index + 1, 0 = empty cell
        private readonly int[] _next;   //point index + 1, 0 = end of chain

        private PointHashGrid(int count, double cellSize)
        {
            _inv = 1 / cellSize;
            var size = 4;
            while (size < count * 2)
                size <<= 1;

            _mask = size - 1;
            _heads = new int[size];
            _next = new int[count];
        }

        internal static double AutoCellSize(Span<Point3D> points)
        {
            if (points.Length == 0)
                return 2 * PointMatch.Tol;

            var min = points[0];
            var max = points[0];
            for (int i = 1; i < points.Length; ++i)
            {
                var p = points[i];
                if (p.X < min.X) min.X = p.X; else if (p.X > max.X) max.X = p.X;
                if (p.Y < min.Y) min.Y = p.Y; else if (p.Y > max.Y) max.Y = p.Y;
                if (p.Z < min.Z) min.Z = p.Z; else if (p.Z > max.Z) max.Z = p.Z;
            }
            var volume = (max.X - min.X) * (max.Y - min.Y) * (max.Z - min.Z);
            var cell = Math.Cbrt(volume / points.Length);
            return cell > 2 * PointMatch.Tol ? cell : 2 * PointMatch.Tol;
        }

        internal static PointHashGrid Fill(List<Point3D> points)
        {
            var span = CollectionsMarshal.AsSpan(points);
            var floor = 2 * PointMatch.Tol;
            var cell = AutoCellSize(span);
            var grid = Fill(points, cell / PointMatch.Tol);
            for (int retry = 0; retry < 4 && cell > floor; ++retry)
            {
                var chain = grid.AverageChain;
                if (chain <= 3)
                    break;

                cell /= Math.Cbrt(chain);
                if (cell < floor)
                    cell = floor;

                grid = Fill(points, cell / PointMatch.Tol);
            }
            return grid;
        }

        internal double CellSize => 1 / _inv;

        private double AverageChain
        {
            get
            {
                var occupied = 0;
                var heads = _heads;
                for (int i = 0; i < heads.Length; ++i)
                    if (heads[i] != 0)
                        ++occupied;

                return occupied == 0 ? 0 : (double)_next.Length / occupied;
            }
        }

        internal static PointHashGrid Fill(List<Point3D> points, double cellFactor)
        {
            var span = CollectionsMarshal.AsSpan(points);
            var grid = new PointHashGrid(span.Length, cellFactor * PointMatch.Tol);
            var heads = grid._heads;
            var next = grid._next;
            var inv = grid._inv;
            var mask = grid._mask;
            for (int i = 0; i < span.Length; ++i)
            {
                var point = span[i];
                var h = Hash(Floor(point.X * inv), Floor(point.Y * inv), Floor(point.Z * inv)) & mask;
                next[i] = heads[h];
                heads[h] = i + 1;
            }
            return grid;
        }

        internal int Search(Point3D point, Span<Point3D> inPoints)
        {
            var inv = _inv;
            var tol = PointMatch.Tol;
            int x0 = Floor((point.X - tol) * inv), x1 = Floor((point.X + tol) * inv),
                y0 = Floor((point.Y - tol) * inv), y1 = Floor((point.Y + tol) * inv),
                z0 = Floor((point.Z - tol) * inv), z1 = Floor((point.Z + tol) * inv);

            int best = -1;
            double bestDistance = 0;
            for (var ix = x0; ix <= x1; ++ix)
                for (var iy = y0; iy <= y1; ++iy)
                    for (var iz = z0; iz <= z1; ++iz)
                        SearchCell(Hash(ix, iy, iz) & _mask, point, inPoints, ref best, ref bestDistance);

            return best;
        }

        private void SearchCell(int bucket, Point3D point, Span<Point3D> inPoints,
            ref int best, ref double bestDistance)
        {
            var link = _heads[bucket];
            while (link != 0)
            {
                var i = link - 1;
                PointMatch.Consider(i, point, inPoints, ref best, ref bestDistance);
                link = _next[i];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int Hash(int x, int y, int z)
        {
            unchecked
            {
                var h = (uint)x * 73856093u ^ (uint)y * 19349663u ^ (uint)z * 83492791u;
                h *= 0x9E3779B1u;
                return (int)(h ^ (h >> 15));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int Floor(double value) => (int)Math.Floor(value);
    }
}