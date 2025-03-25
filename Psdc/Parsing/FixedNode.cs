namespace Scover.Psdc.Parsing;

public interface FixedNode : Node
{
    FixedRange Extent { get; }
}
