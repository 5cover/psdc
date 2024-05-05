using System.Text;

namespace Scover.Psdc.CodeGeneration;

sealed class Indentation
{
    const char Character = ' ';
    const int TabSize = 4;
    int _level;

    public void Decrease() => _level--;

    public void Increase() => _level++;

    public StringBuilder Indent(StringBuilder output)
    {
        for (int i = 0; i < _level * TabSize; i++) {
            _ = output.Append(Character);
        }
        return output;
    }
}
