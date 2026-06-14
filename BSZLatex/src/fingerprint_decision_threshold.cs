var best = candidates[0];
var secondScore = candidates.Count > 1 ? candidates[1].SimilarityScore : 0;

var margin = candidates.Count > 1
    ? Math.Round(best.SimilarityScore - secondScore, 2)
    : double.PositiveInfinity;

var scoreAccepted = best.SimilarityScore >= matchThreshold;
var marginAccepted = candidates.Count <= 1 || margin >= minimumMatchMargin;
var isMatch = scoreAccepted && marginAccepted;