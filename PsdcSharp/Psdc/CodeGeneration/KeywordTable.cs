using Scover.Psdc.Language;
using Scover.Psdc.Messages;
using Scover.Psdc.Parsing;

namespace Scover.Psdc.CodeGeneration;

interface KeywordTable
{
    string Validate(ReadOnlyScope scope, Identifier ident, Messenger msger) => Validate(scope, ident.SourceTokens, ident.Name, msger);
    string Validate(ReadOnlyScope scope, SourceTokens sourceTokens, string ident, Messenger msger);
}
