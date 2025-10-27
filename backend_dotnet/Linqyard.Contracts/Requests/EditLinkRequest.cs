using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linqyard.Contracts.Requests
{
    /// <summary>Partial update payload for Edit; all fields optional.</summary>
    public sealed record EditLinkRequest(
        string? Name = null,
        string? Url = null,
        string? Description = null,
        IReadOnlyList<string>? Tags = null,
        Guid? GroupId = null,   // null => ungroup, Guid.Empty => ungroup (treated the same)
        int? Sequence = null,
        bool? IsActive = null
    );

    /// <summary>Single resequence instruction.</summary>
    public sealed record ResequenceItem(
        Guid Id,
        Guid? GroupId,
        int Sequence
    );

    /// <summary>Returned after resequencing to confirm persisted state.</summary>
    public sealed record LinkSequenceState(
        Guid Id,
        int Sequence,
        Guid? GroupId
    );

    public enum DeleteLinkResult
    {
        NotFound = 0,
        Forbidden = 1,
        Deleted = 2
    }
}
