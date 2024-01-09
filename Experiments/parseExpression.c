#include <assert.h>
#include <stdbool.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#define ARRAYSIZE(arr) (sizeof(arr) / sizeof(arr[0]))

typedef struct {
    char str[128];
} StackStr;

typedef enum {
    LiteralInteger,
    OperatorPlus,
    OperatorMultiply,
    OpenBracket,
    CloseBracket,
} TokenType;

typedef struct {
    StackStr value;
    TokenType type;
} Token;

typedef enum {
    ExpressionBinary,
    ExpressionTerminal,
} ExpressionType;

typedef struct ParseResult ParseResult;

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

struct ParseResult {
    bool success;
    union {
        NodeExpression result;
        struct {
            StackStr msg;
        } error;
    };
};

#define result_fail(message)         \
    (ParseResult)                    \
    {                                \
        .success = false,            \
        .error = {                   \
            .msg = {.str = message}, \
        },                           \
    }

ParseResult result_ok(NodeExpression expr)
{
    return (ParseResult){
        .success = true,
        .result = expr,
    };
}

char token_get_char(Token token);
ParseResult token_literal_create(Token token);
NodeExpression expression_create_binary(ParseResult operand1, Token operator, ParseResult operand2);
ParseResult expression_parse(Token const *tokens, int tokenCount);
void expression_free(NodeExpression expr);
void expression_show(NodeExpression expr);
void expression_show_depth(NodeExpression const expr, int depth);
int token_find(Token const *tokens, int tokenCount, TokenType search);
int token_find_last(Token const *tokens, int tokenCount, TokenType search);

#define token_int(val)          \
    (Token)                     \
    {                           \
        .type = LiteralInteger, \
        .value = {.str = #val}, \
    }

Token token(char repr)
{
    Token tok;
    switch (repr) {
    case '+':
        tok.type = OperatorPlus;
        break;
    case '*':
        tok.type = OperatorMultiply;
        break;
    case '(':
        tok.type = OpenBracket;
        break;
    case ')':
        tok.type = CloseBracket;
        break;
    default:
        fprintf(stderr, "Unsupported token representation : '%c' (%d)\n", repr, repr);
        assert(false);
    }

    return tok;
}

Token const *start;

int main()
{
    // 4 * 3 * 5
    // Token test1[] = {token_int(4), token('*'), token_int(3), token('*'), token_int(5)};

    // 4 * 3 + 5 * 6
    // Token test2[] = {token_int(4), token('*'), token_int(3), token('+'), token_int(5), token('*'), token_int(6)};

    // 4 + 3
    // Token test3[] = {token_int(4), token('+'), token_int(3)};

    // 4 + 3 * 5
    // Token test4[] = {token_int(4), token('+'), token_int(3), token('*'), token_int(5)};

    // 4 + 3 * 5 + 6
    // Token test5[] = {token_int(4), token('+'), token_int(3), token('*'), token_int(5), token('+'), token_int(6)};

    // 4 + (3 * 5 + 6)
    // Token ptest1[] = {token_int(4), token('+'), token('('), token_int(3), token('*'), token_int(5), token('+'), token_int(6), token(')')};

    // 4 * (3 + 5) * 6
    Token ptest2[] = {token_int(4), token('*'), token('('), token_int(3), token('+'), token_int(5), token(')'), token('*'), token_int(6)};

    // 4 * (3 + 5)
    // Token ptest3[] = {token_int(4), token('*'), token('('), token_int(3), token('+'), token_int(5), token(')')};

    // (3 + 5) * 6
    // Token ptest4[] = {token('('), token_int(3), token('+'), token_int(5), token(')'), token('*'), token_int(6)};

#define TEST ptest2
    start = TEST;
    ParseResult r1 = expression_parse(TEST, ARRAYSIZE(TEST));

    if (r1.success) {
        expression_show(r1.result);
        putchar('\n');
        expression_show_depth(r1.result, 0);
        putchar('\n');
        expression_free(r1.result);
    } else {
        printf("Parsing failed: %s\n", r1.error.msg.str);
    }

#undef TEST

    return EXIT_SUCCESS;
}

ParseResult expression_parse(Token const *tokens, int tokenCount)
{
    if (tokenCount == 0) {
        return result_fail("Empty expression");
    }
    if (tokenCount == 1) {
        return token_literal_create(tokens[0]);
    }
    if (tokenCount == 2) {
        return result_fail("Invalid expression");
    }

    // Parse a simple expression
    if (tokenCount == 3) {
        if (tokens[1].type != OperatorMultiply && tokens[1].type != OperatorPlus) {
            return result_fail("Non-operator operator token");
        }

        return result_ok(expression_create_binary(
            token_literal_create(tokens[0]),
            tokens[1],
            token_literal_create(tokens[2])));
    }

    int iOpenBracket = token_find(tokens, tokenCount, OpenBracket);

    if (iOpenBracket != -1) {
        int iCloseBracket = token_find_last(tokens + iOpenBracket, tokenCount - iOpenBracket, CloseBracket);
        if (iCloseBracket == -1) {
            return result_fail("Unmatched parentheses");
        }
        return result_ok(expression_create_binary(
            expression_parse(tokens, iOpenBracket - 1),
            tokens[iOpenBracket - 1],
            expression_parse(tokens + iOpenBracket + 1, iCloseBracket - 1)));
    }

    int iPlus = token_find(tokens, tokenCount, OperatorPlus);

    if (iPlus == -1) {
        // parse using associativity
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

NodeExpression expression_create_binary(ParseResult operand1, Token operator, ParseResult operand2)
{
    NodeExpression expr = {
        .type = ExpressionBinary,
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
    if (token.type != LiteralInteger) {
        return result_fail("Literal token is not an integer");
    }

    return result_ok((NodeExpression){
        .type = ExpressionTerminal,
        .terminal = {
            .literal = token,
        },
    });
}

int token_find(Token const *tokens, int tokenCount, TokenType search)
{
    int i = 0;
    for (; i < tokenCount && tokens[tokenCount].type != search; ++i)
        ;
    return i == tokenCount ? -1 : i;
}
int token_find_last(Token const *tokens, int tokenCount, TokenType search)
{
    int i = tokenCount - 1;
    for (; i >= 0 && tokens[tokenCount].type != search; --i)
        ;
    return i;
}

void expression_show(NodeExpression const expr)
{
    switch (expr.type) {
    case ExpressionBinary:
        putchar('(');
        if (expr.binary.operand1->success) {
            expression_show(expr.binary.operand1->result);
        } else {
            printf("{%s}", expr.binary.operand1->error.msg.str);
        }
        printf(" %c ", token_get_char(expr.binary.operator));
        if (expr.binary.operand2->success) {
            expression_show(expr.binary.operand2->result);
        } else {
            printf("{%s}", expr.binary.operand2->error.msg.str);
        }
        putchar(')');
        break;
    case ExpressionTerminal:
        printf("%s", expr.terminal.literal.value.str);
        break;
    }
}

void expression_show_depth(NodeExpression const expr, int depth)
{
    for (int i = 0; i < depth * 4; ++i) {
        putchar(' ');
    }

    switch (expr.type) {
    case ExpressionBinary:
        printf("%c\n", token_get_char(expr.binary.operator));
        if (expr.binary.operand1->success) {
            expression_show_depth(expr.binary.operand1->result, depth + 1);
        } else {
            printf("{%s}", expr.binary.operand1->error.msg.str);
        }
        putchar('\n');
        if (expr.binary.operand2->success) {
            expression_show_depth(expr.binary.operand2->result, depth + 1);
        } else {
            printf("{%s}", expr.binary.operand2->error.msg.str);
        }
        break;

    case ExpressionTerminal:
        printf("%s", expr.terminal.literal.value.str);
        break;
    }
}

char token_get_char(Token token)
{
    switch (token.type) {
    case OperatorPlus:
        return '+';
    case OperatorMultiply:
        return '*';
    case OpenBracket:
        return '(';
    case CloseBracket:
        return ')';
    default:
        return '?';
    }
}

void expression_free(NodeExpression expr)
{
    if (expr.type == ExpressionBinary) {
        if (expr.binary.operand1->success) {
            expression_free(expr.binary.operand1->result);
        }
        if (expr.binary.operand2->success) {
            expression_free(expr.binary.operand2->result);
        }

        free(expr.binary.operand1);
        free(expr.binary.operand2);
    }
}