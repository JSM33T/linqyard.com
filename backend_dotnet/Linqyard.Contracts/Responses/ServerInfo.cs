using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linqyard.Contracts.Responses
{
    public sealed record ServerInfo(
          string Machine,
          string OS,
          string OSArchitecture,
          string ProcessArchitecture,
          string Framework,
          TimeSpan Uptime,
          DateTimeOffset ProcessStartUtc,
          string? RequestId,
          string? CorrelationId
      );

    public sealed record HealthStatus(
        string Status,
        DateTimeOffset Time,
        string? Test
        );
}
