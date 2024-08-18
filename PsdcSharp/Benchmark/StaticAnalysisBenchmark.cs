﻿using BenchmarkDotNet.Attributes;
using Scover.Psdc.Library;
using Scover.Psdc.Parsing;
using Scover.Psdc.StaticAnalysis;

namespace Scover.Psdc.Benchmark;

[MemoryDiagnoser]
public class StaticAnalysisBenchmark
{
    Node.Algorithm _ast;
    [GlobalSetup]
    public void Setup()
    {
        ParsingBenchmark b = new();
        b.Setup();
        _ast = b.Run().Unwrap();
    }

    [Benchmark]
    public SemanticNode.Algorithm Run() => StaticAnalyzer.Analyze(IgnoreMessenger.Instance, _ast);
}
