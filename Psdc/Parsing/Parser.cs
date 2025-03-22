using Scover.Psdc.Messages;
using Scover.Psdc.Lexing;

using static Scover.Psdc.Parsing.Node;

namespace Scover.Psdc.Parsing;

public static class Parser
{
    // Parsing starts here with the "Algorithm" production rule
    public static ValueOption<Algorithm> Parse(Messenger messenger, IEnumerable<Token> tokens) => default;
}
