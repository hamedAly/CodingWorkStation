using SemanticSearch.Domain.ValueObjects;

namespace SemanticSearch.Application.Quality;

public static class QualityScoringRules
{
    public static QualityGrade CalculateGrade(double duplicationPercent)
    {
        if (duplicationPercent <= 2d)
            return QualityGrade.A;
        if (duplicationPercent <= 5d)
            return QualityGrade.B;
        if (duplicationPercent <= 10d)
            return QualityGrade.C;
        if (duplicationPercent <= 20d)
            return QualityGrade.D;

        return QualityGrade.E;
    }

    public static DuplicationSeverity CalculateSeverity(int matchingLineCount)
    {
        if (matchingLineCount >= 50)
            return DuplicationSeverity.High;
        if (matchingLineCount >= 20)
            return DuplicationSeverity.Medium;

        return DuplicationSeverity.Low;
    }
}
