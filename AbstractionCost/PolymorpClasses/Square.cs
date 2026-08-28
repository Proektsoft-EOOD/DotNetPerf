namespace AbstractionCost.PolymorphClasses
{
    internal class Square : Shape
    {
        private readonly double _side;
        internal override double Width => _side;
        internal override double Height => _side;


        internal Square(double side)
        {
            _side = side;
        }
    
        internal override double Area()
        {
            return _side * _side;
        }

    }
}
