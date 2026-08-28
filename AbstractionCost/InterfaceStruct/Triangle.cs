namespace AbstractionCost.InterfaceStruct
{
    internal readonly struct Triangle : IShape
    {
        private readonly double _width;
        private readonly double _height;
        double IShape.Width => _width;
        double IShape.Height => _height; 

        internal Triangle(double width, double height)
        {
            _width = width;
            _height = height;
        }

        double IShape.Area() => _width * _height / 2d;

    }
}
