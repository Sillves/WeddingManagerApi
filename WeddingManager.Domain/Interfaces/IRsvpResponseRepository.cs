using WeddingManager.Domain.Entities;

namespace WeddingManager.Domain.Interfaces;

public interface IRsvpResponseRepository
{
    Task<IReadOnlyDictionary<Guid, int>> GetCountsByWeddingAsync(Guid weddingId);
    Task<int> CountByFlowAsync(Guid flowId);
    Task<IEnumerable<RsvpResponse>> GetByWeddingIdAsync(Guid weddingId, Guid? flowId);

    /// <summary>
    /// Persists all guests and responses for one submission in a single transaction.
    /// Returns false if a unique-constraint (dedupe) violation occurred; rethrows other errors.
    /// </summary>
    Task<bool> SubmitAsync(IReadOnlyCollection<Guest> guests, IReadOnlyCollection<RsvpResponse> responses);
}
