
using Scover.Psdc.Parsing;

namespace Scover.Psdc.CodeGeneration;

internal interface TypeInfo
{
    string GenerateDeclaration(IEnumerable<Identifier> names);
    string Generate();
}
