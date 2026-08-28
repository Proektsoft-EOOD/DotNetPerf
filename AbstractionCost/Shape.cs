using System;
namespace AbstractionCost
{
    internal readonly struct Shape
    {
        internal enum ShapeTypes
        {
            Circle,
            Rectangle,
            Square,
            Triangle,
        }
        internal readonly ShapeTypes Type;
        private static readonly double[] factors = [ Math.PI / 4d, 1d, 1d, 0.5 ];
        private readonly double _factor;
        internal readonly double Width;
        internal readonly double Height;

        private Shape(double width, double height, ShapeTypes type)
        {
            Width = width;
            Height = height;
            Type = type;
            _factor = factors[(int)Type];
        }

        internal double Area() => Width * Height * _factor;

        internal static Shape CreateCircle(double radius) => 
            new(2d * radius, 2d * radius, ShapeTypes.Circle);

        internal static Shape CreateRectangle(double width, double height) => 
            new(width, height, ShapeTypes.Rectangle);

        internal static Shape CreateSquare(double side) => 
            new(side, side, ShapeTypes.Square);

        internal static Shape CreateTriangle(double width, double height) => 
            new(width, height, ShapeTypes.Triangle);
    }
}
