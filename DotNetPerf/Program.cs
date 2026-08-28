using DotNetPref;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

var switcher = BenchmarkSwitcher.FromTypes([
    typeof(BenchmarkSpan),
    typeof(BenchmarkLoop),
    typeof(BenchmarkSearch),
]);

#if DEBUG
switcher.Run(args, new DebugInProcessConfig());
#else
switcher.Run(args);
#endif
