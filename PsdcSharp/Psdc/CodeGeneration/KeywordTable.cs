using Scover.Psdc.Pseudocode;
using Scover.Psdc.Messages;
using Scover.Psdc.Parsing;

namespace Scover.Psdc.CodeGeneration;

interface KeywordTable
{
    string Validate(Scope scope, Identifier ident, Messenger msger) => Validate(scope, ident.Location, ident.Name, msger);
    string Validate(Scope scope, Range location, string ident, Messenger msger);
}
