using Scover.Psdc.Pseudocode;

namespace Scover.Psdc.Tests;

sealed class MiscTests
{
    [Test]
    [Arguments("hello", "`hello`")]
    [Arguments("`", "`` ` ``")]
    [Arguments("``", "``` `` ```")]
    [Arguments("```", "```` ``` ````")]
    [Arguments("`a`", "`` `a` ``")]
    [Arguments("`a", "`` `a ``")]
    [Arguments("a`", "`` a` ``")]
    [Arguments("a`b", "``a`b``")]
    [Arguments("`a` `b`", "`` `a` `b` ``")]
    [Arguments("``double``", "``` ``double`` ```")]
    [Arguments("`` `tricky` ``", "``` `` `tricky` `` ```")]
    [Arguments("", "``")] // empty string wrapped in two backticks
    [Arguments("no backticks here", "`no backticks here`")]
    [Arguments("starts`", "`` starts` ``")] // ends with backtick -> requires padding
    [Arguments("`ends", "`` `ends ``")]     // starts with backtick -> requires padding
    [Arguments("`both`", "`` `both` ``")]   // both ends -> still only one space each side
    public async Task WrapInCode(string input, string expected)
    {
        var result = input.WrapInCode();
        await Assert.That(result).IsEqualTo(expected);
    }
}
