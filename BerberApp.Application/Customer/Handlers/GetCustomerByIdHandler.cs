using BerberApp.Application.Common.Exceptions;
using BerberApp.Application.Common.Interfaces;
using BerberApp.Application.Customer.DTOs;
using BerberApp.Application.Customer.Queries;
using MediatR;

namespace BerberApp.Application.Customer.Handlers;

public class GetCustomerByIdHandler : IRequestHandler<GetCustomerByIdQuery, CustomerDto>
{
    private readonly IGenericRepository<CustomerEntity> _customerRepo;

    public GetCustomerByIdHandler(IGenericRepository<CustomerEntity> customerRepo)
    {
        _customerRepo = customerRepo;
    }

    public async Task<CustomerDto> Handle(GetCustomerByIdQuery request, CancellationToken ct)
    {
        var customer = await _customerRepo.GetAsync(
            x => x.Id == request.Id && x.TenantId == request.TenantId, ct);

        if (customer is null)
            throw new NotFoundException("Müşteri", request.Id);

        return new CustomerDto
        {
            Id = customer.Id,
            FullName = customer.FullName,
            Phone = customer.Phone,
            Email = customer.Email,
            Notes = customer.Notes,
            TotalVisits = customer.TotalVisits
        };
    }
}