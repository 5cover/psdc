using BenchmarkDotNet.Attributes;
using Scover.Psdc.CodeGeneration;
using Scover.Psdc.StaticAnalysis;

namespace Scover.Psdc.Benchmark;

[MemoryDiagnoser]
public class CodeGenerationBenchmark
{
    SemanticNode.Algorithm _sast;

    [GlobalSetup]
    public void Setup()
    {
        StaticAnalysisBenchmark b = new();
        b.Setup();
        _sast = b.Run();
    }

    [Benchmark]
    public string RunC() => CodeGenerator.GenerateC(IgnoreMessenger.Instance, _sast);
}
