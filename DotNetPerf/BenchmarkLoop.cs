using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using BenchmarkDotNet.Attributes;


namespace DotNetPref
{
    [SimpleJob]
    [MemoryDiagnoser]
    public class BenchmarkLoop
    {
        private const int PointCount = 1000;
        private readonly List<Point3D> _points = new(PointCount);

        [GlobalSetup]
        public void Setup()
        {
            var random = new Random();

            for (int i = 0; i < PointCount; i++)
                _points.Add(new Point3D(RandomDouble(), RandomDouble(), RandomDouble()));
        
            double RandomDouble() => 50 - 100 * random.NextDouble();
        }

        [Benchmark]
        public Point3D LinqForeach()
        {
            Point3D sum = new(0, 0, 0);
            _points.ForEach(point => sum += point);
            return sum / _points.Count;
        }
        
        [Benchmark]
        public Point3D ListForeach()
        {
            Point3D sum = new(0, 0, 0);
            foreach (var point in _points)
                sum += point;
            
            return sum / _points.Count;
        }

        [Benchmark]
        public Point3D SpanFor()
        {
            Point3D sum = new(0, 0, 0);
            var pointsSpan = CollectionsMarshal.AsSpan(_points);
            for (int i = 0, len = pointsSpan.Length; i < len; ++i)
                sum += pointsSpan[i];

            return sum / PointCount;
        }


        [Benchmark]
        public Point3D SpanForDoubleAccum()
        {
            Point3D s1 = new(0, 0, 0), s2 = new(0, 0, 0);
            var pointsSpan = CollectionsMarshal.AsSpan(_points);
            int i = 0, len = pointsSpan.Length;
            for (; i + 1 < len; i += 2)
            {
                s1 += pointsSpan[i];
                s2 += pointsSpan[i + 1];
            }
            if (i < len)
                s1 += pointsSpan[i];

            return new Point3D(s1.X + s2.X, s1.Y + s2.Y, s1.Z + s2.Z) / len;
        }

        [Benchmark]
        public Point3D SpanForSimd()
        {
            var points = CollectionsMarshal.AsSpan(_points);

            if (Vector512.IsHardwareAccelerated && points.Length >= 8)
                return Simd512(points);
            if (Vector256.IsHardwareAccelerated && points.Length >= 4)
                return Simd256(points);

            return Scalar(points);
        }

        private static Point3D Simd512(Span<Point3D> points)
        {
            var d = MemoryMarshal.Cast<Point3D, double>(points);
            ref double r = ref MemoryMarshal.GetReference(d);

            const int W = 8; // Vector512<double>.Count
            int n = d.Length, block = n - n % (3 * W), j = 0;

            Vector512<double> a = Vector512<double>.Zero, b = a, c = a;
            for (; j < block; j += 3 * W)
            {
                a += Vector512.LoadUnsafe(ref r, (nuint)j);
                b += Vector512.LoadUnsafe(ref r, (nuint)(j + W));
                c += Vector512.LoadUnsafe(ref r, (nuint)(j + 2 * W));
            }
            Span<double> lanes = stackalloc double[3 * W];
            a.StoreUnsafe(ref lanes[0]);
            b.StoreUnsafe(ref lanes[W]);
            c.StoreUnsafe(ref lanes[2 * W]);
            return Reduce(lanes, ref r, j, n, points.Length);
        }

        private static Point3D Simd256(Span<Point3D> points)
        {
            var d = MemoryMarshal.Cast<Point3D, double>(points);
            ref double r = ref MemoryMarshal.GetReference(d);

            const int W = 4; // Vector256<double>.Count
            int n = d.Length, block = n - n % (3 * W), j = 0;

            Vector256<double> a = Vector256<double>.Zero, b = a, c = a;
            for (; j < block; j += 3 * W)
            {
                a += Vector256.LoadUnsafe(ref r, (nuint)j);
                b += Vector256.LoadUnsafe(ref r, (nuint)(j + W));
                c += Vector256.LoadUnsafe(ref r, (nuint)(j + 2 * W));
            }
            Span<double> lanes = stackalloc double[3 * W];
            a.StoreUnsafe(ref lanes[0]);
            b.StoreUnsafe(ref lanes[W]);
            c.StoreUnsafe(ref lanes[2 * W]);
            return Reduce(lanes, ref r, j, n, points.Length);
        }

        private static Point3D Scalar(Span<Point3D> points)
        {
            var d = MemoryMarshal.Cast<Point3D, double>(points);
            ref double r = ref MemoryMarshal.GetReference(d);
            return Reduce([], ref r, 0, d.Length, points.Length);
        }

        private static Point3D Reduce(Span<double> lanes, ref double r, int j, int n, int count)
        {
            double x = 0, y = 0, z = 0;
            for (int k = 0; k < lanes.Length; k += 3)
            {
                x += lanes[k];
                y += lanes[k + 1];
                z += lanes[k + 2];
            }
            for (; j < n; j += 3)
            {
                x += Unsafe.Add(ref r, j);
                y += Unsafe.Add(ref r, j + 1);
                z += Unsafe.Add(ref r, j + 2);
            }
            return new Point3D(x , y , z) / count;
        }
    }
}
