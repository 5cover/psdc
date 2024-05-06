using System.Globalization;

using Scover.Psdc.Language;

using static Scover.Psdc.Parsing.Node;

namespace Scover.Psdc.StaticAnalysis;

static partial class AstExtensions
{
    public static Expression.BinaryOperation Alter(this Expression expr, BinaryOperator @operator, int value)
    // This feels like cheating, creatig AST nodes with placeholder source tokens during code generation
    // But this is the only way without massive abstraction
    // This will be improved if it ever becomes a problem.
     => new(SourceTokens.Empty, expr, @operator,
            new Expression.Literal.Integer(SourceTokens.Empty, value.ToString(CultureInfo.InvariantCulture)));
}
