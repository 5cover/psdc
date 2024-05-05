
using Scover.Psdc.Parsing;

namespace Scover.Psdc.CodeGeneration;

interface TypeInfo
{
    public string GenerateDeclaration(IEnumerable<Identifier> names);
    public string Generate();
    public string DecorateExpression(string expr);
}
