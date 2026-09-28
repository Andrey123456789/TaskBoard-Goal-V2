namespace TaskBoard.Infrastructure.Persistence.Configurations;

/// <summary>
/// Explicit column collations so uniqueness semantics do not depend on the database default.
/// </summary>
internal static class Collations
{
    public const string CaseInsensitive = "Latin1_General_100_CI_AS";
    public const string CaseSensitive = "Latin1_General_100_CS_AS";
}
