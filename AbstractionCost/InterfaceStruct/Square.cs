namespace AbstractionCost.InterfaceStruct
{
    internal readonly struct Square : IShape
    {
        private readonly double _side;
        double IShape.Width => _side;
        double IShape.Height => _side;


        internal Square(double side)
        {
            _side = side;
        }
    
        double IShape.Area() => _side * _side;
    }
}
