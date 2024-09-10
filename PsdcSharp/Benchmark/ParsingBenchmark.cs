using BenchmarkDotNet.Attributes;
using Scover.Psdc.Library;
using Scover.Psdc.Parsing;
using Scover.Psdc.Tokenization;

namespace Scover.Psdc.Benchmark;

[MemoryDiagnoser]
public class ParsingBenchmark
{
    static readonly Parameters p = Program.Parameters;
    Token[] _tokens = null!;

    [GlobalSetup]
    public void Setup() => _tokens = new TokenizationBenchmark().Run();

    [Benchmark]
    public ValueOption<Node.Program> Run()
     => Parser.Parse(p.Msger, _tokens);
}
