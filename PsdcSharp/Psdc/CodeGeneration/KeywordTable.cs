using Scover.Psdc.Messages;
using Scover.Psdc.Parsing;

namespace Scover.Psdc.CodeGeneration;

interface KeywordTable
{
    string Validate(Identifier ident, Messenger msger) => Validate(ident.SourceTokens, ident.Name, msger);
    string Validate(SourceTokens sourceTokens, string ident, Messenger msger);
}
