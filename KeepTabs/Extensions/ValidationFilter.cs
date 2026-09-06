using FluentValidation;

namespace KeepTabs.Extensions;

/// <summary>
/// Minimal-API validation filter that returns RFC 7807 validation problems.
/// </summary>
public sealed class ValidationFilter<T> : IEndpointFilter where T : class
{
    private readonly IValidator<T> _validator;

    public ValidationFilter(IValidator<T> validator)
    {
        _validator = validator;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var request = context.Arguments.OfType<T>().FirstOrDefault();
        if (request is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["request"] = ["Request body is required."]
            });
        }

        var validation = await _validator.ValidateAsync(request, context.HttpContext.RequestAborted);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        return await next(context);
    }
}
