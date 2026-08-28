namespace AbstractionCost.PolymorphClasses
{
    internal class Rectangle : Shape
    {
        private readonly double _width;
        private readonly double _height;
        internal override double Width => _width;
        internal override double Height => _height;

        internal Rectangle(double width, double height)
        {
            _width = width;
            _height = height;
        }

        internal override double Area()
        {
            return _width * _height;
        }

    }
}
