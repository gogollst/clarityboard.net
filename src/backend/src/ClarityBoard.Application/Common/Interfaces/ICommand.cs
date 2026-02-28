using MediatR;

namespace ClarityBoard.Application.Common.Interfaces;

public interface ICommand<out TResponse> : IRequest<TResponse>;
public interface ICommand : IRequest;
