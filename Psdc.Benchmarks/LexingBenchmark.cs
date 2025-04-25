using BenchmarkDotNet.Attributes;

using Scover.Psdc.Lexing;

namespace Scover.Psdc.Benchmarks;

[MemoryDiagnoser]
public sealed class LexingBenchmark
{
    static readonly Parameters p = Program.Parameters;

    [Benchmark]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822", Justification = "benchmark")]
    public Token[] Run()
        => new Lexer(p.Msger).Lex(p.Input).ToArray();
}
