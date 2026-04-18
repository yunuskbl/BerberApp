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

public class UpdateCustomerHandler : IRequestHandler<UpdateCustomerCommand, CustomerDto>
{
    private readonly IGenericRepository<CustomerEntity> _customerRepo;

    public UpdateCustomerHandler(IGenericRepository<CustomerEntity> customerRepo)
    {
        _customerRepo = customerRepo;
    }

    public async Task<CustomerDto> Handle(UpdateCustomerCommand request, CancellationToken ct)
    {
        var customer = await _customerRepo.GetAsync(
            x => x.Id == request.Id && x.TenantId == request.TenantId, ct);

        if (customer is null)
            throw new NotFoundException("Müşteri", request.Id);

        customer.FullName = request.FullName;
        customer.Phone = request.Phone;
        customer.Email = request.Email;
        customer.Notes = request.Notes;

        await _customerRepo.UpdateAsync(customer, ct);

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