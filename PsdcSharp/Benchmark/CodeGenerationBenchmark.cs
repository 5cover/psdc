using BenchmarkDotNet.Attributes;

using Scover.Psdc.CodeGeneration;
using Scover.Psdc.Library;
using Scover.Psdc.StaticAnalysis;

namespace Scover.Psdc.Benchmark;

[MemoryDiagnoser]
public sealed class CodeGenerationBenchmark
{
    static readonly Parameters p = Program.Parameters;
    SemanticNode.Algorithm? _sast;

    [GlobalSetup]
    public void Setup()
    {
        StaticAnalysisBenchmark b = new();
        b.Setup();
        _sast = b.Run();
    }

    [Benchmark]
    public string RunC()
    {
        CodeGenerator.TryGet(Language.CliOption.C, out var cg);
        return cg.NotNull()(p.Msger, _sast.NotNull());
    }
}
