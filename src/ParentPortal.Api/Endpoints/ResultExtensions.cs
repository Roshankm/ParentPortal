using ParentPortal.Domain.Common;

namespace ParentPortal.Api.Endpoints;

internal static class ResultExtensions
{
    public static IResult ToProblem(this Error error) => error.Type switch
    {
        ErrorType.Validation => TypedResults.Problem(error.Message, statusCode: StatusCodes.Status400BadRequest, title: error.Code),
        ErrorType.NotFound => TypedResults.Problem(error.Message, statusCode: StatusCodes.Status404NotFound, title: error.Code),
        ErrorType.Conflict => TypedResults.Problem(error.Message, statusCode: StatusCodes.Status409Conflict, title: error.Code),
        ErrorType.Forbidden => TypedResults.Problem(error.Message, statusCode: StatusCodes.Status403Forbidden, title: error.Code),
        _ => TypedResults.Problem(error.Message),
    };
}
