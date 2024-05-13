using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using Scover.Psdc.Tokenization;

namespace Scover.Psdc.Benchmark;

[MemoryDiagnoser]
public class TokenizationBenchmark
{
    [Benchmark]
    [SuppressMessage("Performance", "CA1822", Justification = "benchmark")]
    public IReadOnlyCollection<Token> Run() => Tokenizer.Tokenize(IgnoreMessenger.Instance, Program.Code).ToImmutableArray();
}
