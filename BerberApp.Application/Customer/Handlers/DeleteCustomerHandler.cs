using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BerberApp.Application.Common.Exceptions;
using BerberApp.Application.Common.Interfaces;
using BerberApp.Application.Customer.Commands;
using MediatR;

namespace BerberApp.Application.Customer.Handlers;

public class DeleteCustomerHandler : IRequestHandler<DeleteCustomerCommand, bool>
{
    private readonly IGenericRepository<CustomerEntity> _customerRepo;

    public DeleteCustomerHandler(IGenericRepository<CustomerEntity> customerRepo)
    {
        _customerRepo = customerRepo;
    }

    public async Task<bool> Handle(DeleteCustomerCommand request, CancellationToken ct)
    {
        var customer = await _customerRepo.GetAsync(
            x => x.Id == request.Id && x.TenantId == request.TenantId, ct);

        if (customer is null)
            throw new NotFoundException("Müşteri", request.Id);

        await _customerRepo.DeleteAsync(customer, ct);
        return true;
    }
}