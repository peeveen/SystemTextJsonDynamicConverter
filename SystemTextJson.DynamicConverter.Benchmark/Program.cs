using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace SystemTextJson.DynamicConverter.Benchmark;

[MemoryDiagnoser]
public static class Program {
	public static void Main() {
		_ = BenchmarkRunner.Run(typeof(Program).Assembly);
	}
}
