namespace Linqyard.Contracts.Responses;

public sealed record GroupSequenceStateResponse(
    Guid Id,
    int Sequence,
    string Name
);

public sealed record GroupResequenceResult(
    string Message,
    IReadOnlyList<GroupSequenceStateResponse> FinalState
);

