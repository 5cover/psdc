﻿using BenchmarkDotNet.Attributes;
using Scover.Psdc.CodeGeneration;
using Scover.Psdc.StaticAnalysis;

namespace Scover.Psdc.Benchmark;

[MemoryDiagnoser]
public class CodeGenerationBenchmark
{
    private SemanticAst _ast;
    [GlobalSetup]
    public void Setup()
    {
        StaticAnalysisBenchmark b = new();
        b.Setup();
        _ast = b.Run();
    }

    [Benchmark]
    public string RunC() => CodeGenerator.GenerateC(IgnoreMessenger.Instance, _ast);
}
