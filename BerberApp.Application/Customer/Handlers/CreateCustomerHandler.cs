using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BerberApp.Application.Common.Exceptions;
using BerberApp.Application.Common.Interfaces;
using BerberApp.Application.Customer.Commands;
using BerberApp.Application.Customer.DTOs;
using MediatR;

namespace BerberApp.Application.Customer.Handlers;

public class CreateCustomerHandler : IRequestHandler<CreateCustomerCommand, CustomerDto>
{
    private readonly IGenericRepository<CustomerEntity> _customerRepo;
    private readonly IGenericRepository<TenantEntity> _tenantRepo;

    public CreateCustomerHandler(
        IGenericRepository<CustomerEntity> customerRepo,
        IGenericRepository<TenantEntity> tenantRepo)
    {
        _customerRepo = customerRepo;
        _tenantRepo = tenantRepo;
    }

    public async Task<CustomerDto> Handle(CreateCustomerCommand request, CancellationToken ct)
    {
        var tenantExists = await _tenantRepo.AnyAsync(x => x.Id == request.TenantId, ct);
        if (!tenantExists)
            throw new NotFoundException("Tenant", request.TenantId);

        var phoneExists = await _customerRepo.AnyAsync(
            x => x.Phone == request.Phone && x.TenantId == request.TenantId, ct);
        if (phoneExists)
            throw new ConflictException($"'{request.Phone}' numaralı müşteri zaten kayıtlı.");

        var customer = new CustomerEntity
        {
            TenantId = request.TenantId,
            FullName = request.FullName,
            Phone = request.Phone,
            Email = request.Email,
            Notes = request.Notes
        };

        await _customerRepo.AddAsync(customer, ct);
        return ToDto(customer);
    }

    private static CustomerDto ToDto(CustomerEntity c) => new()
    {
        Id = c.Id,
        FullName = c.FullName,
        Phone = c.Phone,
        Email = c.Email,
        Notes = c.Notes,
        TotalVisits = c.TotalVisits
    };
}