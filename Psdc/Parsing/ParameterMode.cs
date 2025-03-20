namespace Scover.Psdc.Parsing;

#region Terminals

sealed record ParameterMode
{
    ParameterMode(string formal, string actual) => (RepresentationFormal, RepresentationActual) = (formal, actual);

    public string RepresentationFormal { get; }
    public string RepresentationActual { get; }

    public static ParameterMode In { get; } = new("entF", "entE");
    public static ParameterMode Out { get; } = new("sortF", "sortE");
    public static ParameterMode InOut { get; } = new("entF/sortF", "entE/sortE");
}

#endregion Terminals
