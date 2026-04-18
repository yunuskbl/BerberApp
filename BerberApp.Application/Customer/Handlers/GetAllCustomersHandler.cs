using BerberApp.Application.Common.Interfaces;
using BerberApp.Application.Customer.DTOs;
using BerberApp.Application.Customer.Queries;
using MediatR;

namespace BerberApp.Application.Customer.Handlers;

public class GetAllCustomersHandler : IRequestHandler<GetAllCustomersQuery, List<CustomerDto>>
{
    private readonly IGenericRepository<CustomerEntity> _customerRepo;

    public GetAllCustomersHandler(IGenericRepository<CustomerEntity> customerRepo)
    {
        _customerRepo = customerRepo;
    }

    public async Task<List<CustomerDto>> Handle(GetAllCustomersQuery request, CancellationToken ct)
    {
        var list = await _customerRepo.GetAllAsync(x => x.TenantId == request.TenantId, ct);

        return list.Select(c => new CustomerDto
        {
            Id = c.Id,
            FullName = c.FullName,
            Phone = c.Phone,
            Email = c.Email,
            Notes = c.Notes,
            TotalVisits = c.TotalVisits
        }).ToList();
    }
}