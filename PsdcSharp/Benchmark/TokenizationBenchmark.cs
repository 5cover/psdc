using BenchmarkDotNet.Attributes;
using Scover.Psdc.Tokenization;

namespace Scover.Psdc.Benchmark;

[MemoryDiagnoser]
public class TokenizationBenchmark
{
    static readonly Parameters p = Program.Parameters;
    [Benchmark]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822", Justification = "benchmark")]
    public Token[] Run()
     => Tokenizer.Tokenize(p.Msger, p.Input).ToArray();
}
