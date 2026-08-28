using System;

namespace AbstractionCost.PolymorphClasses
{
    internal class Circle : Shape
    {
        private readonly double _radius;
        internal override double Width => 2d * _radius;
        internal override double Height => 2d * _radius;

        internal Circle(double radius)
        {
            _radius = radius;
        }

        internal override double Area()
        {
            return Math.PI * _radius * _radius;
        }

    }
}
