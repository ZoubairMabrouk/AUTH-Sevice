using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
namespace AUTH_Sevice.Application.Common.Behaviors
{

    public class ValidationBehavior<TRequest, TResponse>(
        IEnumerable<IValidator<TRequest>> validators,
        ILogger<ValidationBehavior<TRequest, TResponse>> logger)
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
        {
            if (!validators.Any())
                return await next();

            var context = new ValidationContext<TRequest>(request);
            var results = await Task.WhenAll(validators.Select(v => v.ValidateAsync(context, ct)));
            var failures = results.SelectMany(r => r.Errors).Where(f => f is not null).ToList();

            if (failures.Count == 0)
                return await next();

            logger.LogWarning("Validation failed for {Request}: {Errors}",
                typeof(TRequest).Name, string.Join(", ", failures.Select(f => f.ErrorMessage)));

            throw new FluentValidation.ValidationException(failures);
        }
    }

    public class LoggingBehavior<TRequest, TResponse>(
        ILogger<LoggingBehavior<TRequest, TResponse>> logger)
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
        {
            var requestName = typeof(TRequest).Name;
            logger.LogInformation("Handling {RequestName}", requestName);

            var response = await next();

            logger.LogInformation("Handled {RequestName}", requestName);
            return response;
        }
    }

}
