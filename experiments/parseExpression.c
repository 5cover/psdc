/**
 * @file parseExpression.c
 * @brief This file contains the implementation of a simple expression parser.
 * The parser can parse arithmetic expressions consisting of literal integers,
 * addition and multiplication operators, and parentheses.
 * It uses a recursive descent parsing algorithm to build an abstract syntax tree (AST)
 * representing the expression.
 * The AST is then printed and freed.
 */
#include <assert.h>
#include <stdbool.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#define ARRAYSIZE(arr) (sizeof(arr) / sizeof(arr[0]))

/// @brief A stack-allocated string of maximum length 255.
/// @brief Used to take advantage of C's automatic memory management of stack-allocated objects.
typedef struct {
    char str[256];
} StackStr;

/// @brief Type of a token.
typedef enum {
    TokenType_LiteralInteger,
    TokenType_OperatorAdd,
    TokenType_OperatorMultiply,
    TokenType_BracketOpen,
    TokenType_BracketClose,
} TokenType;

/// @brief A token, smallest unit of the expression.
typedef struct {
    StackStr value;
    TokenType type;
} Token;

/// @brief Type of an expression.
typedef enum {
    ExpressionType_Binary,
    ExpressionType_Terminal,
} ExpressionType;

typedef struct ParseResult ParseResult;

/// @brief A node in the abstract syntax tree (AST) representing an expression.
typedef struct {
    ExpressionType type;
    union {
        struct {
            Token operator;
            ParseResult *operand1;
            ParseResult *operand2;
        } binary;
        struct {
            Token literal;
        } terminal;
    };
} NodeExpression;

/// @brief Result of a parsing operation.
struct ParseResult {
    bool hasValue;
    union {
        NodeExpression value;
        struct {
            StackStr msg;
        } error;
    };
};

/// @brief Macro to create a failed parse result.
/// @param message The error message of the parse result.
/// @return A new failed ParseResult.
#define result_fail(message)         \
    (ParseResult)                    \
    {                                \
        .hasValue = false,           \
        .error = {                   \
            .msg = {.str = message}, \
        },                           \
    }

/// @brief Macro to create a successful parse result.
/// @param expr The expression to create the parse result from.
/// @return A new successful ParseResult.
#define result_ok(expr)   \
    (ParseResult)         \
    {                     \
        .hasValue = true, \
        .value = (expr),  \
    }

/// @brief Macro to create an integer literal token from a character.
/// @param repr The character to create the token from.
/// @return A new integer literal Token.
#define token_int(val)                    \
    (Token)                               \
    {                                     \
        .type = TokenType_LiteralInteger, \
        .value = {.str = #val},           \
    }

/// @brief Get the character representation of a token.
/// @param token The token to get the character representation of.
/// @return The character representation of the token.
char token_get_char(Token token);

/// @brief Create a parse result from a token.
/// @param token The token to create the parse result from.
/// @return The parse result.
ParseResult token_literal_create(Token token);

/// @brief Create a binary expression node.
/// @param operand1 The first operand of the binary expression.
/// @param operator The operator of the binary expression.
/// @param operand2 The second operand of the binary expression.
/// @return The binary expression node.
NodeExpression expression_create_binary(ParseResult operand1, Token operator, ParseResult operand2);

/// @brief Parse an expression from a array of tokens.
/// @param tokens The array of tokens to parse the expression from.
/// @param tokenCount The amount of tokens in the array.
/// @return The parse result of the expression.
ParseResult expression_parse(Token const *tokens, int tokenCount);

/// @brief Free an expression node.
/// @param expr The expression node to free.
void expression_free(NodeExpression expr);

/// @brief Show an expression.
/// @param expr The expression to show.
void expression_show(NodeExpression expr);

/// @brief Show an expression with depth.
/// @param expr The expression to show.
/// @param depth The depth of the expression.
void expression_show_depth(NodeExpression const expr, int depth);

/// @brief Find the index of the first token of a specific type in an array of tokens.
/// @param tokens The array of tokens to search in.
/// @param startIndex The index to start searching from.
/// @param tokenCount The amount of tokens in the array.
/// @param search The type of token to search for.
int token_find(Token const *tokens, int startIndex, int tokenCount, TokenType search);

/// @brief Find the index of the last token of a specific type in an array of tokens.
/// @param tokens The array of tokens to search in.
/// @param startIndex The index to start searching from.
/// @param tokenCount The amount of tokens in the array.
/// @param search The type of token to search for.
int token_find_last(Token const *tokens, int startIndex, int tokenCount, TokenType search);

/// @brief Create a token from a character.
/// @param repr The character to create the token from.
/// @return A new Token.
Token token(char repr)
{
    Token tok;
    switch (repr) {
    case '+': tok.type = TokenType_OperatorAdd; break;
    case '*': tok.type = TokenType_OperatorMultiply; break;
    case '(': tok.type = TokenType_BracketOpen; break;
    case ')': tok.type = TokenType_BracketClose; break;
    default:
        fprintf(stderr, "Unsupported token representation : '%c' (%d)\n", repr, repr);
        abort();
    }

    return tok;
}

Token const *start;

int main()
{
    Token test[] =
        // 4 * 3 * 5
        //{token_int(4), token('*'), token_int(3), token('*'), token_int(5)};

        // 4 * 3 + 5 * 6
        //{token_int(4), token('*'), token_int(3), token('+'), token_int(5), token('*'), token_int(6)};

        // 4 + 3
        //{token_int(4), token('+'), token_int(3)};

        // (4)
        //{token('('), token_int(4), token(')')};

        // (4 + 3)
        //{token('('), token_int(4), token('+'), token_int(3), token(')')};

        // 4 + 3 * 5
        //{token_int(4), token('+'), token_int(3), token('*'), token_int(5)};

        // 4 + 3 * 5 + 6
        //{token_int(4), token('+'), token_int(3), token('*'), token_int(5), token('+'), token_int(6)};

        // 4 + (3 + 5) + 6
        //{token_int(4), token('+'), token('('), token_int(3), token('+'), token_int(5), token(')'), token('+'), token_int(6)};

        // 4 + (3 * 5 + 6)
        //{token_int(4), token('+'), token('('), token_int(3), token('*'), token_int(5), token('+'), token_int(6), token(')')};

        // 4 * (3 + 5) * 6
        //{token_int(4), token('*'), token('('), token_int(3), token('+'), token_int(5), token(')'), token('*'), token_int(6)};

        // 4 * (3 + 5)
        //{token_int(4), token('*'), token('('), token_int(3), token('+'), token_int(5), token(')')};

        // (3 + 5) * 6
        {token('('), token_int(3), token('+'), token_int(5), token(')'), token('*'), token_int(6)};

    ParseResult result = expression_parse(test, ARRAYSIZE(test));

    if (result.hasValue) {
        expression_show_depth(result.value, 0);
        putchar('\n');
        expression_show(result.value);
        putchar('\n');
        expression_free(result.value);
    } else {
        printf("Parsing failed: %s\n", result.error.msg.str);
    }

#undef TEST

    return EXIT_SUCCESS;
}

ParseResult expression_parse(Token const *tokens, int tokenCount)
{
    switch (tokenCount) {
    case 0: return result_fail("Empty expression");
    case 1: return token_literal_create(tokens[0]);
    case 2: return result_fail("Invalid expression");
    case 3:
        // Parse a simple expression
        if (tokens[1].type == TokenType_OperatorMultiply || tokens[1].type == TokenType_OperatorAdd) {
            return result_ok(expression_create_binary(
                token_literal_create(tokens[0]),
                tokens[1],
                token_literal_create(tokens[2])));
        } else if (tokens[0].type == TokenType_BracketOpen && tokens[2].type == TokenType_BracketClose) {
            return expression_parse(tokens + 1, 1);
        }
        return result_fail("Invalid expression");
    default:
        // Check if the expression contains brackets
        int const iOpenBracket = token_find(tokens, 0, tokenCount, TokenType_BracketOpen);
        if (iOpenBracket != -1) {
            int const iCloseBracket = token_find_last(tokens, iOpenBracket + 1, tokenCount, TokenType_BracketClose);
            if (iCloseBracket == -1) {
                return result_fail("Unmatched parentheses");
            }

            ParseResult expr_before;
            bool const has_expr_before = iOpenBracket > 1;
            if (has_expr_before) {
                expr_before = expression_parse(tokens, iOpenBracket - 1);
            }

            ParseResult const expr_inside = expression_parse(tokens + iOpenBracket + 1, iCloseBracket - iOpenBracket - 1);

            if (iCloseBracket < tokenCount - 2) {
                ParseResult expr_after = expression_parse(tokens + iCloseBracket + 2, tokenCount - iCloseBracket - 2);
                ParseResult expr_insideAfter = result_ok(expression_create_binary(
                    expr_inside,
                    tokens[iCloseBracket + 1], // operator after )
                    expr_after));

                return has_expr_before
                         ? result_ok(expression_create_binary(
                             expr_before,
                             tokens[iOpenBracket - 1],
                             expr_insideAfter))
                         : expr_insideAfter;
            }

            return has_expr_before
                     ? result_ok(expression_create_binary(
                         expr_before,
                         tokens[iOpenBracket - 1], // operator before (
                         expr_inside))
                     : expr_inside;
        }

        int const iPlus = token_find(tokens, 0, tokenCount, TokenType_OperatorAdd);
        if (iPlus == -1) {
            // plus not found: parse using associativity
            return result_ok(expression_create_binary(
                token_literal_create(tokens[0]),
                tokens[1],
                expression_parse(tokens + 2, tokenCount - 2)));
        }

        // plus found: parse left and right plus operands separately
        return result_ok(expression_create_binary(
            expression_parse(tokens, iPlus),
            tokens[iPlus],
            expression_parse(tokens + iPlus + 1, tokenCount - iPlus - 1)));
    }
}

NodeExpression expression_create_binary(ParseResult operand1, Token operator, ParseResult operand2)
{
    NodeExpression expr = {
        .type = ExpressionType_Binary,
        .binary = {
            .operand1 = malloc(sizeof operand1),
            .operand2 = malloc(sizeof operand2),
            .operator= operator, },
        };

    if (expr.binary.operand1 == NULL || expr.binary.operand2 == NULL) {
        fprintf(stderr, "malloc failed\n");
        exit(EXIT_FAILURE);
    }

    *expr.binary.operand1 = operand1;
    *expr.binary.operand2 = operand2;

    return expr;
}

ParseResult token_literal_create(Token token)
{
    if (token.type != TokenType_LiteralInteger) {
        return result_fail("Literal token is not an integer");
    }

    return result_ok(((NodeExpression){
        .type = ExpressionType_Terminal,
        .terminal = {
            .literal = token,
        }
    }));
}

int token_find(Token const *tokens, int startIndex, int tokenCount, TokenType search)
{
    int i = startIndex;
    while (i < tokenCount && tokens[i].type != search) {
        ++i;
    }
    return i < tokenCount ? i : -1;
}

int token_find_last(Token const *tokens, int startIndex, int tokenCount, TokenType search)
{
    int i = tokenCount - 1;
    while (i >= startIndex && tokens[i].type != search) {
        --i;
    }
    return i >= startIndex ? i : -1;
}

void expression_show(NodeExpression const expr)
{
    switch (expr.type) {
    case ExpressionType_Binary:
        putchar('(');
        if (expr.binary.operand1->hasValue) {
            expression_show(expr.binary.operand1->value);
        } else {
            printf("{%s}", expr.binary.operand1->error.msg.str);
        }
        printf(" %c ", token_get_char(expr.binary.operator));
        if (expr.binary.operand2->hasValue) {
            expression_show(expr.binary.operand2->value);
        } else {
            printf("{%s}", expr.binary.operand2->error.msg.str);
        }
        putchar(')');
        break;
    case ExpressionType_Terminal:
        printf("%s", expr.terminal.literal.value.str);
        break;
    }
}

void expression_show_depth(NodeExpression const expr, int depth)
{
    for (int i = 0; i < depth * 2; ++i) {
        putchar(' ');
    }

    switch (expr.type) {
    case ExpressionType_Binary:
        printf("%c\n", token_get_char(expr.binary.operator));
        if (expr.binary.operand1->hasValue) {
            expression_show_depth(expr.binary.operand1->value, depth + 1);
        } else {
            printf("{%s}", expr.binary.operand1->error.msg.str);
        }
        putchar('\n');
        if (expr.binary.operand2->hasValue) {
            expression_show_depth(expr.binary.operand2->value, depth + 1);
        } else {
            printf("{%s}", expr.binary.operand2->error.msg.str);
        }
        break;

    case ExpressionType_Terminal:
        printf("%s", expr.terminal.literal.value.str);
        break;
    }
}

char token_get_char(Token token)
{
    switch (token.type) {
    case TokenType_OperatorAdd:
        return '+';
    case TokenType_OperatorMultiply:
        return '*';
    case TokenType_BracketOpen:
        return '(';
    case TokenType_BracketClose:
        return ')';
    default:
        return '?';
    }
}

void expression_free(NodeExpression expr)
{
    if (expr.type == ExpressionType_Binary) {
        if (expr.binary.operand1->hasValue) {
            expression_free(expr.binary.operand1->value);
        }
        if (expr.binary.operand2->hasValue) {
            expression_free(expr.binary.operand2->value);
        }

        free(expr.binary.operand1);
        free(expr.binary.operand2);
    }
}
