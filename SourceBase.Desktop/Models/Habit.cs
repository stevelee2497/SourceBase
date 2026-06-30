namespace SourceBase.Desktop.Models;

/// <summary>
/// A single habit shown as a card on the rest overlay. Phase 1 is local-only,
/// so this is a plain serializable model — not the API entity.
/// </summary>
public sealed class Habit
{
    public Guid Id { get; init; }
    public required string Name { get; set; }

    /// <summary>Emoji shown on the card, e.g. "💧". Optional if ImagePath is set.</summary>
    public string? Emoji { get; set; }

    /// <summary>Optional path to an image file; takes priority over Emoji when present.</summary>
    public string? ImagePath { get; set; }

    /// <summary>Whether this habit appears in the overlay.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>Accent color for the card (hex, e.g. "#3B82F6"). Optional.</summary>
    public string? Accent { get; set; }
}
