using BenchmarkDotNet.Attributes;

using Scover.Psdc.Parsing;
using Scover.Psdc.StaticAnalysis;

namespace Scover.Psdc.Benchmarks;

[MemoryDiagnoser]
public sealed class StaticAnalysisBenchmark
{
    static readonly Parameters p = Program.Parameters;
    Node.Algorithm _ast;

    [GlobalSetup]
    public void Setup()
    {
        ParsingBenchmark b = new();
        b.Setup();
        _ast = b.Run();
    }

    [Benchmark]
    public SemanticNode.Algorithm Run()
        => StaticAnalyzer.Analyze(p.Msger, p.Input, _ast);
}
