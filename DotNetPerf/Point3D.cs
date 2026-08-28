using System.Runtime.CompilerServices;

namespace DotNetPref
{
    public struct Point3D(double x, double y, double z)
    {
        public double X { get; set;} = x;
        public double Y { get; set;} = y;
        public double Z { get; set;} = z;

        //Branchless coordinate access by axis: 0 = X, 1 = Y, 2 = Z.
        //Relies on the same sequential layout that MemoryMarshal.Cast in BenchmarkLoop assumes.
        public readonly double this[int axis]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Unsafe.Add(ref Unsafe.As<Point3D, double>(ref Unsafe.AsRef(in this)), axis);
        }

        public static Point3D operator +(Point3D a, Point3D b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Point3D operator /(Point3D a, double d) => new(a.X / d, a.Y / d, a.Z / d);
    }
}
