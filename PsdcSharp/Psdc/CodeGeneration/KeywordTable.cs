using Scover.Psdc.Pseudocode;
using Scover.Psdc.Messages;
using Scover.Psdc.Parsing;

namespace Scover.Psdc.CodeGeneration;

interface KeywordTable
{
    string Validate(Scope scope, Identifier ident, Messenger msger) => Validate(scope, ident.SourceTokens, ident.ToString(), msger);
    string Validate(Scope scope, SourceTokens sourceTokens, string ident, Messenger msger);
}
