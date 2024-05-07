using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Scover.Psdc.Parsing;
using Scover.Psdc.Tokenization;

namespace Scover.Psdc.Benchmark;

[MemoryDiagnoser]
public class ParsingBenchmark
{
    private IReadOnlyCollection<Token> _tokens;
    [GlobalSetup]
    public void Setup() => _tokens = new TokenizationBenchmark().Run();

    [Benchmark]
    public ParseResult<Node.Algorithm> Run() => Parser.Parse(IgnoreMessenger.Instance, _tokens);
}
