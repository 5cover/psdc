﻿using System.Globalization;
using System.Text;
using Scover.Psdc.Language;
using Scover.Psdc.Messages;

using static Scover.Psdc.Parsing.Node;

namespace Scover.Psdc.CodeGeneration.C;

partial class CodeGeneratorC
{
    (string format, IReadOnlyList<Expression> arguments) BuildFormatString(IEnumerable<Expression> parts)
    {
        List<Expression> arguments = [];
        StringBuilder format = new();

        foreach (var part in parts) {
            if (part is Expression.Literal literal) {
                format.Append(literal.ActualValue.ToString(CultureInfo.InvariantCulture)
                    .Replace("%", "%%") // escape C format specifiers
                    .Replace(@"\", @"\\")); // escape C escape sequences
            } else {
                var type = _ast.InferredTypes[part];
                // Don't show error for an unknown inferred type. They don't have a useful representation.
                if (type != EvaluatedType.Unknown.Inferred) {
                    CreateTypeInfo(type).FormatComponent.Match(fmtComp => {
                        format.Append(fmtComp);
                        arguments.Add(part);
                    }, () => _msger.Report(Message.ErrorTargetLanguage("C", part.SourceTokens,
                             $"type '{type}' cannot be used in a format string")));
                }
            }
        }

        return (format.ToString(), arguments);
    }
}
