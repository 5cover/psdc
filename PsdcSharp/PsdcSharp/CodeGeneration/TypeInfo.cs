namespace Scover.Psdc.CodeGeneration;

internal interface TypeInfo
{
    string GenerateDeclaration(IEnumerable<string> names);
    string Generate();
}
