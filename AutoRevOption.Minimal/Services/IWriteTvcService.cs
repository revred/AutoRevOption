using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AutoRevOption.Minimal.Services;

public record ExecutionCard(
    string Mode, string TvcRef, string Symbol, string Strategy,
    IReadOnlyList<Leg> Legs, decimal IntendedCreditLimit,
    object Brackets, object Admissibility, object? BrokerPreview, object ActionResult);

public record ExecutionRequest(string Mode, string TvcPath);

public interface IWriteTvcService
{
    Task<ExecutionCard> ActAsync(ExecutionRequest req, CancellationToken ct);
}
