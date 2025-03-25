using Scover.Psdc.Pseudocode;

namespace Scover.Psdc.StaticAnalysis;

public readonly record struct SemanticMetadata(Scope Scope, FixedRange Extent);
