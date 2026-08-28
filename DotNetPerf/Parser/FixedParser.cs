using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Calcpad.Core
{
    internal static class FixedParser
    {
        private static readonly double[] Pow10 =
        [
            1e00,
            1e01,
            1e02,
            1e03,
            1e04,
            1e05,
            1e06,
            1e07,
            1e08,
            1e09,
            1e10,
            1e11,
            1e12,
            1e13,
            1e14,
            1e15,
            1e16,
            1e17,
            1e18,
            1e19,
            1e20,
            1e21,
            1e22
        ];
        const int Pow10Length = 23;
        const long MaxExactInt = 9007199254740992L;

        // Simple floating point number parser using the Horner rule

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static double Parse(ReadOnlySpan<char> s, out int count)
        {
            long k = 0;
            int i = 0, len = s.Length, digits = 0, dot = -1;
            for (; i < len; ++i)
            {
                var d = (uint)(s[i] - '0');
                if (d <= 9)
                {
                    ++digits;
                    k = k * 10 + d;
                }
                else if (s[i] != '.')
                    break;
                else if (dot < 0)
                    dot = i;
                else
                {
                    count = i;
                    return double.NaN;
                }
            }
            count = i;
            var scale = dot < 0 ? 0 : i - dot - 1;
            if (digits < 19 && k <= MaxExactInt && scale < Pow10Length)
                return k / Pow10[scale];

            return ParseExact(s[..count]);
        }

        private static double ParseExact(ReadOnlySpan<char> s) =>
            double.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture);
    }
}
