using System;

namespace AbstractionCost.InterfaceStruct
{
    internal readonly struct Circle : IShape
    {
        private readonly double _radius;
        double IShape.Width => 2d * _radius;
        double IShape.Height => 2d * _radius;

        internal Circle(double radius)
        {
            _radius = radius;
        }

        double IShape.Area()
        {
            return Math.PI * _radius * _radius;
        }

    }
}
