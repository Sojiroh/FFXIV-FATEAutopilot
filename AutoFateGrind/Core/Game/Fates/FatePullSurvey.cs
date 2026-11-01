namespace AutoFateGrind.Core.Game.Fates;

internal readonly record struct FatePullSurvey(int OnPlayer, int Candidates, FateMobTarget Next)
{
    public bool HasCandidate => Candidates > 0;
}
