using BenchmarkDotNet.Attributes;
using System;
namespace AbstractionCost
{
    [SimpleJob]
    public class Benchmarks
    {
        private const int ShapeCount = 1000;
        private PolymorphClasses.Shape[] _shapesP;
        private InterfaceStruct.IShape[] _shapesI;
        private Shape[] _shapesF;
        private Shapes _shapesSoA;

        [GlobalSetup]
        public void Setup()
        {
            _shapesP = new PolymorphClasses.Shape[ShapeCount];
            _shapesI = new InterfaceStruct.IShape[ShapeCount];
            _shapesF = new Shape[ShapeCount];
            _shapesSoA = new(ShapeCount);
            Random random = new();
            for (int i = 0; i < ShapeCount; i += 4)
            {
                var r = random.NextDouble();
                var w = random.NextDouble();
                var h = 2d * random.NextDouble();
                var a = random.NextDouble();
                _shapesP[i] = new PolymorphClasses.Circle(r);
                _shapesP[i + 1] = new PolymorphClasses.Rectangle(w, h);
                _shapesP[i + 2] = new PolymorphClasses.Square(a);
                _shapesP[i + 3] = new PolymorphClasses.Triangle(w, h);
                _shapesI[i] = new InterfaceStruct.Circle(r);
                _shapesI[i + 1] = new InterfaceStruct.Rectangle(w, h);
                _shapesI[i + 2] = new InterfaceStruct.Square(a);
                _shapesI[i + 3] = new InterfaceStruct.Triangle(w, h);
                _shapesF[i] = Shape.CreateCircle(r);
                _shapesF[i + 1] = Shape.CreateRectangle(w, h);
                _shapesF[i + 2] = Shape.CreateSquare(a);
                _shapesF[i + 3] = Shape.CreateTriangle(w, h);
                _shapesSoA.AddCircle(r);
                _shapesSoA.AddRectangle(w, h);
                _shapesSoA.AddSquare(a);
                _shapesSoA.AddTriangle(w, h);
            }
        }

        [Benchmark]
        public double AreaPolymorphClass()
        {
            double area = 0d;
            for (int i = 0, len = _shapesP.Length; i < len; i++)
                area += _shapesP[i].Area();

            return area;
        }

        [Benchmark]
        public double AreaInterfaceStruct()
        {
            double area = 0d;
            for (int i = 0, len = _shapesI.Length; i < len; i++)
                area += _shapesI[i].Area();

            return area;
        }

        [Benchmark]
        public double AreaStructFactory()
        {
            double area = 0d;
            for (int i = 0, len = _shapesF.Length; i < len; i++)
                area += _shapesF[i].Area();

            return area;
        }

        [Benchmark]
        public double AreaPolymorphClassDoubleAccum()
        {
            double a1 = 0d, a2 = 0d;
            int i = 0, len = _shapesP.Length;
            for (; i + 1 < len; i += 2)
            {
                a1 += _shapesP[i].Area();
                a2 += _shapesP[i + 1].Area();
            }
            if (i < len)
                a1 += _shapesP[i].Area();

            return a1 + a2;
        }

        [Benchmark]
        public double AreaStructFactoryDoubleAccum()
        {
            double a1 = 0d, a2 = 0d;
            int i = 0, len = _shapesF.Length;
            for (; i + 1 < len; i += 2)
            {
                a1 += _shapesF[i].Area();
                a2 += _shapesF[i + 1].Area();
            }
            if (i < len)
                a1 += _shapesF[i].Area();

            return a1 + a2;
        }

        [Benchmark]
        public double AreaShapesSoA() => _shapesSoA.Area();

        [Benchmark]
        public double AreaShapesSoASimd() => _shapesSoA.AreaSimd();
    }
}
