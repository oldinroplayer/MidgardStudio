using MidgardStudio.Core.Lookup;
using MidgardStudio.Core.Workspace;

namespace MidgardStudio.Core.Validation;

/// <summary>
/// Ambient inputs a validator needs that aren't carried by the record itself: the active server
/// mode (renewal-aware caps) and a cross-database reference index. This is the seam that keeps Core
/// rules free of any WPF / GRF / client dependency — the App answers the abstract questions.
/// </summary>
public sealed class ValidationContext
{
    public required ServerMode Mode { get; init; }

    public required IReferenceIndex References { get; init; }

    public static ValidationContext Create(IReferenceIndex references, ServerMode mode = ServerMode.Renewal) =>
        new() { Mode = mode, References = references };
}
