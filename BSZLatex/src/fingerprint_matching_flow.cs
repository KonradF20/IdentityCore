var probeTemplate = new SourceAFIS.FingerprintTemplate(probeExtraction.TemplateData);
var matcher = new FingerprintMatcher(probeTemplate);

var rawCandidates = new List<FingerprintCandidateMatch>();

foreach (var storedTemplate in storedTemplates)
{
    if (!persons.TryGetValue(storedTemplate.PersonId, out var person))
    {
        continue;
    }

    if (storedTemplate.TemplateData.Length == 0)
    {
        continue;
    }

    var candidateTemplate = new SourceAFIS.FingerprintTemplate(storedTemplate.TemplateData);
    var score = matcher.Match(candidateTemplate);

    rawCandidates.Add(new FingerprintCandidateMatch
    {
        PersonId = person.Id,
        PersonFullName = person.FullName,
        PersonCode = person.PersonCode,
        Department = person.Department,
        SourceImagePath = storedTemplate.SourceImagePath,
        SimilarityScore = Math.Round(score, 2)
    });
}