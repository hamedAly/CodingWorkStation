namespace SemanticSearch.Domain.ValueObjects;

public static class ReviewQuality
{
    public const int CompleteBlackout = 0;
    public const int Incorrect = 1;
    public const int IncorrectEasy = 2;
    public const int CorrectDifficult = 3;
    public const int CorrectHesitation = 4;
    public const int Perfect = 5;

    public static IReadOnlyList<(int Value, string Label)> All { get; } =
    [
        (CompleteBlackout, "Complete blackout"),
        (Incorrect, "Incorrect"),
        (IncorrectEasy, "Incorrect but easy"),
        (CorrectDifficult, "Correct with difficulty"),
        (CorrectHesitation, "Correct with hesitation"),
        (Perfect, "Perfect recall")
    ];
}
