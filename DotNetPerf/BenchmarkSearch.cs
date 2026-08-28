using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using DotNetPref.PointSearch;

namespace DotNetPref
{
    [SimpleJob]
    [MemoryDiagnoser]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    public class BenchmarkSearch
    {
        private const int _pointCount = 100_000;
        private readonly List<Point3D> _points = [];
        private readonly List<Point3D> _queries = [];

        private BspTree _bspTree;
        private PointHashGrid _grid;

        [GlobalSetup]
        public void Setup()
        {
            var random = new Random(42);
            for (int i = 0; i < _pointCount; i++)
                _points.Add(new Point3D(RandomDouble(), RandomDouble(), RandomDouble()));

            //Query every 16th point, so each lookup is a hit at a known index.
            for (int i = 0; i < _pointCount; i += 16)
                _queries.Add(_points[i]);

            _bspTree = BspTree.Fill(_points);
            _grid = PointHashGrid.Fill(_points);
            Verify();

            double RandomDouble() => 50 - 100 * random.NextDouble();
        }

        private void Verify()
        {
            var points = CollectionsMarshal.AsSpan(_points);
            var queries = CollectionsMarshal.AsSpan(_queries);
            for (int i = 0; i < queries.Length; ++i)
            {
                var expected = i * 16;
                Check(PointListSearch.Search(queries[i], points), expected, "List");
                Check(_bspTree.Search(queries[i], points), expected, "Flat");
                Check(_grid.Search(queries[i], points), expected, "Grid");

                var jittered = new Point3D(
                    queries[i].X + 0.004, queries[i].Y - 0.004, queries[i].Z + 0.004);
                var reference = PointListSearch.Search(jittered, points);
                Check(_bspTree.Search(jittered, points), reference, "Flat (jittered)");
                Check(_grid.Search(jittered, points), reference, "Grid (jittered)");
            }

            static void Check(int actual, int expected, string name)
            {
                if (actual != expected)
                    throw new InvalidOperationException($"{name} returned {actual}, expected {expected}.");
            }
        }


        [BenchmarkCategory("Fill"), Benchmark]
        public BspTree FillBSP() => BspTree.Fill(_points);

        [BenchmarkCategory("Fill"), Benchmark]
        public PointHashGrid FillGrid() => PointHashGrid.Fill(_points);

        [BenchmarkCategory("Search"), Benchmark]
        public int SearchList()
        {
            var points = CollectionsMarshal.AsSpan(_points);
            var queries = CollectionsMarshal.AsSpan(_queries);
            var sum = 0;
            for (int i = 0; i < queries.Length; ++i)
                sum += PointListSearch.Search(queries[i], points);

            return sum;
        }

        [BenchmarkCategory("Search"), Benchmark]
        public int SearchBSP()
        {
            var points = CollectionsMarshal.AsSpan(_points);
            var queries = CollectionsMarshal.AsSpan(_queries);
            var sum = 0;
            for (int i = 0; i < queries.Length; ++i)
                sum += _bspTree.Search(queries[i], points);

            return sum;
        }

        [BenchmarkCategory("Search"), Benchmark]
        public int SearchGrid()
        {
            var points = CollectionsMarshal.AsSpan(_points);
            var queries = CollectionsMarshal.AsSpan(_queries);
            var sum = 0;
            for (int i = 0; i < queries.Length; ++i)
                sum += _grid.Search(queries[i], points);

            return sum;
        }
    }
}
