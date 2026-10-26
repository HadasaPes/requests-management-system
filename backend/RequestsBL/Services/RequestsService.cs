using RequestsBL.Dtos;
using RequestsBL.Exceptions;
using RequestsBL.Mapping;
using RequestsBL.Validation;
using RequestsDAL.Repositories;

namespace RequestsBL.Services;

public class RequestsService : IRequestsService
{
    private readonly IRequestsRepository _repository;

    public RequestsService(IRequestsRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedResponse<RequestDto>> GetRequestsAsync(
        RequestQueryParameters parameters, CancellationToken cancellationToken)
    {
        var query = RequestQueryValidator.ToQuery(parameters);

        var result = await _repository.GetPagedAsync(query, cancellationToken);

        return new PagedResponse<RequestDto>
        {
            Items = result.Items.Select(r => r.ToDto()).ToList(),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = result.TotalCount,
            TotalPages = (int)Math.Ceiling(result.TotalCount / (double)query.PageSize)
        };
    }

    public async Task<RequestDto> GetRequestByIdAsync(int id, CancellationToken cancellationToken)
    {
        var request = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Request {id} was not found.");

        return request.ToDto();
    }
}
