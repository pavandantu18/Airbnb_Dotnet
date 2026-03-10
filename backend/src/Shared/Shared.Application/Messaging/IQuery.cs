using MediatR;
using Shared.Domain.Common;

namespace Shared.Application.Messaging;

// A Query represents a READ — it fetches data without changing state.
// Queries: GetPropertyById, SearchProperties, GetUserProfile
// Rules:
//   - Never modifies any state
//   - Always returns Result<T> with data
//   - Can be cached (since they're read-only)
//   - Named as Get/Search/List + noun: GetBookingQuery, SearchPropertiesQuery
public interface IQuery<TResponse> : IRequest<Result<TResponse>>;
