using UOM.Application.Common;
using UOM.Application.Features.Public.Models;

namespace UOM.Application.Features.Public;

public interface IContactFormService
{
    Task<IReadOnlyList<FacultyOptionDto>> GetFacultyOptionsAsync(CancellationToken cancellationToken = default);

    Task<Result> SubmitAsync(ContactFormRequest request, string? ipAddress, CancellationToken cancellationToken = default);
}