var templates = _faceTemplateRepository.GetAll();

if (templates.Count == 0)
{
    return CreateFailedResult(
        "W bazie nie ma zarejestrowanych szablonów twarzy. Najpierw zarejestruj próbki twarzy w profilu osoby.",
        "Nie znaleziono dopasowania",
        matchThreshold,
        minimumMargin);
}

var persons = _personRepository
    .GetAll()
    .ToDictionary(person => person.Id);

var rawScores = new List<RawFaceCandidateScore>();

foreach (var template in templates)
{
    if (!persons.TryGetValue(template.PersonId, out var person))
    {
        continue;
    }

    var templateEmbedding = DeserializeEmbedding(template.EmbeddingJson);



    if (templateEmbedding.Length == 0)
    {
        continue;
    }

    var similarity = CalculateSimilarityPercentage(inputEmbedding, templateEmbedding);

    rawScores.Add(new RawFaceCandidateScore
    {
        Person = person,
        Template = template,
        SimilarityScore = similarity
    });
}

if (rawScores.Count == 0)
{
    return CreateFailedResult(
        "Nie udało się odczytać żadnego poprawnego szablonu twarzy z bazy.",
        "Nie znaleziono dopasowania",
        matchThreshold,
        minimumMargin);
}

var candidates = rawScores
    .GroupBy(score => score.Person.Id)
    .Select(group => BuildCandidateForPerson(group.ToList()))
    .OrderByDescending(candidate => candidate.SimilarityScore)
    .Take(3)
    .Select((candidate, index) =>
    {
        candidate.Rank = index + 1;
        return candidate;
    })
    .ToList();