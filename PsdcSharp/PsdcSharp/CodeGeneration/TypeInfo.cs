using Scover.Psdc.Parsing.Nodes;

namespace Scover.Psdc.CodeGeneration;

internal interface TypeInfo
{
    string GenerateDeclaration(IEnumerable<Node.Identifier> names);
    string Generate();
}
