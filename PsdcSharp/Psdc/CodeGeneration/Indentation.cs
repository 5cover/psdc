using System.Text;

namespace Scover.Psdc.CodeGeneration;

sealed class Indentation(int tabSize)
{
    private readonly int _tabSize = tabSize;
    const char Character = ' ';
    int _level;

    public void Decrease() => _level--;

    public void Increase() => _level++;

    public StringBuilder Indent(StringBuilder output)
    {
        for (int i = 0; i < _level * _tabSize; i++) {
            _ = output.Append(Character);
        }
        return output;
    }
}
