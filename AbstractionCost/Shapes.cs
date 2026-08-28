using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
namespace AbstractionCost
{
    internal struct Shapes
    {
        internal readonly int Count = -1;
        private int _index = -1;
        private static readonly double[] factors = [ Math.PI / 4d, 1d, 1d, 0.5 ];
        private readonly int[] _type = [];
        private readonly double[] _width = [];
        private readonly double[] _height = [];
        private readonly double[] _factor = [];

        internal Shapes ( int count )
        {
            Count = count;
            _type = new int[count];
            _width = new double[count];
            _height = new double[count];
            _factor = new double[count];
        }

        private void AddShape(double width, double height, int type)
        {
            if (++_index >= Count)
                throw new InvalidOperationException($"Cannot add more than {Count} shapes.");

            _width[_index] = width;
            _height[_index] = height;
            _type[_index] = type;
            _factor[_index] = factors[type];
        }

        internal double Area()
        {
            ref double rf = ref MemoryMarshal.GetReference(_factor);
            ref double rw = ref MemoryMarshal.GetReference(_width);
            ref double rh = ref MemoryMarshal.GetReference(_height);
            double a1 = 0d, a2 = 0d;
            int i = 0;
            for (; i < _index; i += 2)
            {
                a1 += Unsafe.Add(ref rf, i) * Unsafe.Add(ref rw, i) * Unsafe.Add(ref rh, i);
                a2 += Unsafe.Add(ref rf, i + 1) * Unsafe.Add(ref rw, i + 1) * Unsafe.Add(ref rh, i + 1);
            }
            if (i < _index)
                a1 += Unsafe.Add(ref rf, i) * Unsafe.Add(ref rw, i) * Unsafe.Add(ref rh, i);

            return a1 + a2;
        }

        internal double AreaSimd()
        {
            const int W512 = 8; // Vector512<double>.Count
            const int W256 = 4; // Vector256<double>.Count

            int count = _index + 1;
            ref double rf = ref MemoryMarshal.GetArrayDataReference(_factor);
            ref double rw = ref MemoryMarshal.GetArrayDataReference(_width);
            ref double rh = ref MemoryMarshal.GetArrayDataReference(_height);
            double area = 0d;
            int i = 0;
            if (Vector512.IsHardwareAccelerated && count >= 2 * W512)
            {
                Vector512<double> v1 = Vector512<double>.Zero, v2 = v1;
                for (; i + 2 * W512 <= count; i += 2 * W512)
                {
                    v1 += Vector512.LoadUnsafe(ref rf, (nuint)i)
                        * Vector512.LoadUnsafe(ref rw, (nuint)i)
                        * Vector512.LoadUnsafe(ref rh, (nuint)i);
                    v2 += Vector512.LoadUnsafe(ref rf, (nuint)(i + W512))
                        * Vector512.LoadUnsafe(ref rw, (nuint)(i + W512))
                        * Vector512.LoadUnsafe(ref rh, (nuint)(i + W512));
                }
                area = Vector512.Sum(v1 + v2);
            }
            else if (Vector256.IsHardwareAccelerated && count >= 2 * W256)
            {
                Vector256<double> v1 = Vector256<double>.Zero, v2 = v1;
                for (; i + 2 * W256 <= count; i += 2 * W256)
                {
                    v1 += Vector256.LoadUnsafe(ref rf, (nuint)i)
                        * Vector256.LoadUnsafe(ref rw, (nuint)i)
                        * Vector256.LoadUnsafe(ref rh, (nuint)i);
                    v2 += Vector256.LoadUnsafe(ref rf, (nuint)(i + W256))
                        * Vector256.LoadUnsafe(ref rw, (nuint)(i + W256))
                        * Vector256.LoadUnsafe(ref rh, (nuint)(i + W256));
                }
                area = Vector256.Sum(v1 + v2);
            }
            for (; i < count; i++)
                area += Unsafe.Add(ref rf, i) * Unsafe.Add(ref rw, i) * Unsafe.Add(ref rh, i);

            return area;
        }

        internal void AddCircle(double radius) => 
            AddShape(2d * radius, 2d * radius, 0);

        internal void AddRectangle(double width, double height) => 
            AddShape(width, height, 1);

        internal void AddSquare(double side) => 
            AddShape(side, side, 2);

        internal void AddTriangle(double width, double height) => 
            AddShape(width, height, 3);
    }
}
