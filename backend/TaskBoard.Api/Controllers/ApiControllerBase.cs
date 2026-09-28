using Microsoft.AspNetCore.Mvc;

namespace TaskBoard.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected ObjectResult NotFoundProblem(string title, string detail) =>
        Problem(statusCode: StatusCodes.Status404NotFound, title: title, detail: detail);

    /// <summary>
    /// The request conflicts with the current business state of the resource (409).
    /// </summary>
    protected ObjectResult ConflictProblem(string title, string detail) =>
        Problem(statusCode: StatusCodes.Status409Conflict, title: title, detail: detail);

    /// <summary>
    /// The request is structurally valid but semantically invalid, for example it references
    /// a resource that does not exist (422).
    /// </summary>
    protected ObjectResult UnprocessableProblem(string title, string detail) =>
        Problem(statusCode: StatusCodes.Status422UnprocessableEntity, title: title, detail: detail);
}
