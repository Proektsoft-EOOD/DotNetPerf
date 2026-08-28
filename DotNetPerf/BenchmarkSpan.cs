using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using BenchmarkDotNet.Attributes;
using Calcpad.Core;

namespace DotNetPref
{
    [SimpleJob]
    [MemoryDiagnoser]
    public partial class BenchmarkSpan
    {
        private static readonly string _input = string.Join(Environment.NewLine,
        [
            "0.123345678;1.234567890;2.345678901",
            "3.456789012;4.567890123;5.678901234",
            "7.890123456;8.901234567;9.012345678",
            "0.123345678;1.234567890;2.345678901",
            "3.456789012;4.567890123;5.678901234",
            "7.890123456;8.901234567;9.012345678",
            "0.123345678;1.234567890;2.345678901",
            "3.456789012;4.567890123;5.678901234",
            "7.890123456;8.901234567;9.012345678"
        ]);

        private static double ParseFloat(ReadOnlySpan<char> span) =>
            double.Parse(span, NumberStyles.Float, CultureInfo.InvariantCulture);

        private static FormatException Invalid() => new("Invalid input format.");

        [GeneratedRegex(@"\d+\.?\d*|\.\d+")]
        private static partial Regex NumberRegex();

        [Benchmark]
        public List<Point3D> ParseRegex()
        {
            var points = new List<Point3D>(10);
            Span<double> coords = stackalloc double[3];
            var span = _input.AsSpan();
            int pos = 0, j = 0, len = span.Length;
            var matches = NumberRegex().EnumerateMatches(span);
            foreach (var match in matches)
            {
                if (match.Index != pos)
                    throw Invalid();

                coords[j] = ParseFloat(span.Slice(match.Index, match.Length));
                pos = match.Index + match.Length;
                var c = pos < len ? span[pos] : '\n';
                if (j < 2 ? c != ';' : c != '\r' && c != '\n')
                    throw Invalid();

                if (pos < len)
                {
                    ++pos;
                    if (c == '\r' && pos < len && span[pos] == '\n')
                        ++pos;
                }
                if (j < 2)
                    ++j;
                else
                {
                    points.Add(new Point3D(coords[0], coords[1], coords[2]));
                    j = 0;
                }
            }
            if (j != 0 || pos != len)
                throw Invalid();

            return points;
        }

        [Benchmark]
        public List<Point3D> ParseString()
        {
            var lines = _input.Split(Environment.NewLine);
            var points = new List<Point3D>(lines.Length);
            foreach (var line in lines) 
            {
                var coords = line.Split(';');
                if (coords.Length != 3)
                    throw Invalid();

                var x = ParseFloat(coords[0]);
                var y = ParseFloat(coords[1]);
                var z = ParseFloat(coords[2]);
                points.Add(new(x, y, z));
            }
            return points;
        }

        [Benchmark]
        public List<Point3D> ParseSpanIndex()
        {
            var lines = _input.EnumerateLines();
            var points = new List<Point3D>(10);
            foreach (var line in lines)
            {
                var i1 = line.IndexOf(';');
                if (i1 < 0)
                    throw Invalid();

                var yz = line[(i1 + 1)..];
                var i2 = yz.IndexOf(';');
                if (i2 < 0)
                    throw Invalid();

                var rest = yz[(i2 + 1)..];
                if (rest.IndexOf(';') >= 0)
                    throw Invalid();

                var x = ParseFloat(line[..i1]);
                var y = ParseFloat(yz[..i2]);
                var z = ParseFloat(rest);
                points.Add(new(x, y, z));
            }
            return points;
        }

        [Benchmark]
        public List<Point3D> ParseSpanSplit()
        {
            var lines = _input.EnumerateLines();
            var points = new List<Point3D>(10);
            Span<double> coords = stackalloc double[3];
            foreach (var line in lines)
            {
                var split = line.Split(';');
                int index = 0;
                foreach (var range in split)
                {
                    if (index > 2)
                        throw Invalid();

                    coords[index++] = ParseFloat(line[range]);
                }
                if (index < 3)
                    throw Invalid();

                points.Add(new Point3D(coords[0], coords[1], coords[2]));
            }
            return points;
        }

        [Benchmark]
        public List<Point3D> ParseCustomSplit()
        {
            var lines = _input.EnumerateLines();
            Span<double> coords = stackalloc double[3];
            var points = new List<Point3D>(10);
            foreach (var line in lines)
            {
                var split = new SplitEnumerator(line, ';');
                int index = 0;
                foreach (var span in split)
                {
                    if (index > 2)
                        throw Invalid();

                    var d = FixedParser.Parse(span, out var n);
                    if (n == 0 || n != span.Length || double.IsNaN(d))
                        throw Invalid();

                    coords[index++] = d;
                }
                if (index < 3)
                    throw Invalid();

                points.Add(new Point3D(coords[0], coords[1], coords[2]));
            }
            return points;
        } 

        [Benchmark]
        public List<Point3D> ParseCustomTextSpan()
        {
            Span<double> coords = stackalloc double[2];
            int index = 0;
            var ts = new TextSpan(_input);
            ts.Reset(0);
            int len = _input.Length;
            var points = new List<Point3D>(10);
            for (int i = 0; i < len; ++i)
            {
                var c = _input[i];
                if (c != ';' && c != '\r' && c != '\n')
                    continue;

                ts.ExpandTo(i);
                var span = ts.Cut();
                var d = FixedParser.Parse(span, out var n);
                if (n == 0 || n != span.Length || double.IsNaN(d))
                    throw Invalid();

                if (c == ';')
                {
                    //  Only x and y are kept - z goes straight into the point.
                    if (index > 1)
                        throw Invalid();

                    coords[index++] = d;
                    ts.Reset(i + 1);
                }
                else
                {
                    if (index != 2)
                        throw Invalid();

                    points.Add(new Point3D(coords[0], coords[1], d));
                    if (c == '\r' && i + 1 < len && _input[i + 1] == '\n')
                        ++i;

                    ts.Reset(i + 1);
                    index = 0;
                }
            }
            ts.ExpandTo(len);
            if (!ts.IsEmpty)
            {
                if (index != 2)
                    throw Invalid();

                var span = ts.Cut();
                var d = FixedParser.Parse(span, out var n);
                if (n != span.Length || double.IsNaN(d))
                    throw Invalid();

                points.Add(new Point3D(coords[0], coords[1], d));
            }
            else if (index != 0)
                throw Invalid();

            return points;
        }

        [Benchmark]
        public List<Point3D> ParseCustomScan()
        {
            var span = _input.AsSpan(); 
            Span<double> coords = stackalloc double[3];
            //Add some initial capacity to save allocations even when you don't know the exact length.
            var points = new List<Point3D>(10);
            while (!span.IsEmpty)
            {
                for (int j = 0; j < 3; ++j)
                {
                    var d = FixedParser.Parse(span, out var n);
                    if (n == 0 || double.IsNaN(d))
                        throw Invalid();

                    coords[j] = d;
                    span = span[n..];
                    var c = span.IsEmpty ? '\n' : span[0];
                    if (j < 2 ? c != ';' : c != '\r' && c != '\n')
                        throw Invalid();

                    if (!span.IsEmpty)
                        span = span[1..];
                }
                points.Add(new Point3D(coords[0], coords[1], coords[2]));
                if (!span.IsEmpty && span[0] == '\n')
                    span = span[1..];
            }
            return points;
        }
    }
}
