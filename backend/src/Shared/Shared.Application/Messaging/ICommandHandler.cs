using MediatR;
using Shared.Domain.Common;

namespace Shared.Application.Messaging;

// Typed handler interfaces so module handlers don't need to write
// IRequestHandler<XCommand, Result<Y>> every time — less boilerplate, more readable.
public interface ICommandHandler<TCommand, TResponse>
    : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse>;

public interface ICommandHandler<TCommand>
    : IRequestHandler<TCommand, Result>
    where TCommand : ICommand;
