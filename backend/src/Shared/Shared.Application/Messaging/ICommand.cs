using MediatR;
using Shared.Domain.Common;

namespace Shared.Application.Messaging;

// A Command represents an INTENT TO CHANGE STATE.
// Commands: RegisterUser, CreateBooking, ProcessPayment
// Rules:
//   - Always returns Result (success or failure) — never void
//   - Named as verb + noun: CreateBookingCommand, CancelBookingCommand
//   - One handler per command (unlike events which can have many handlers)
//
// IRequest<Result<T>> is MediatR's interface — it wires this command
// to its handler through the MediatR pipeline (which runs our behaviors).
public interface ICommand<TResponse> : IRequest<Result<TResponse>>;

// For commands that don't return data (just success/failure), use ICommand
public interface ICommand : IRequest<Result>;
