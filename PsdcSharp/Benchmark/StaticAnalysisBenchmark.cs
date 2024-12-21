using BenchmarkDotNet.Attributes;
using Scover.Psdc.Library;
using Scover.Psdc.Parsing;
using Scover.Psdc.StaticAnalysis;

namespace Scover.Psdc.Benchmark;

[MemoryDiagnoser]
public class StaticAnalysisBenchmark
{
    static readonly Parameters p = Program.Parameters;
    Node.Algorithm _ast = null!;

    [GlobalSetup]
    public void Setup()
    {
        ParsingBenchmark b = new();
        b.Setup();
        _ast = b.Run().Unwrap();
    }

    [Benchmark]
    public SemanticNode.Algorithm Run()
     => StaticAnalyzer.Analyze(p.Msger, p.Input, _ast);
}
