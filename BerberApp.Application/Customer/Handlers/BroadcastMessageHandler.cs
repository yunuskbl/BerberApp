using BerberApp.Application.Common.Interfaces;
using BerberApp.Application.Customer.Commands;
using MediatR;

namespace BerberApp.Application.Customer.Handlers;

public class BroadcastMessageHandler : IRequestHandler<BroadcastMessageCommand, BroadcastMessageResult>
{
    private readonly IGenericRepository<CustomerEntity> _customerRepo;
    private readonly IWhatsAppService _whatsAppService;

    public BroadcastMessageHandler(
        IGenericRepository<CustomerEntity> customerRepo,
        IWhatsAppService whatsAppService)
    {
        _customerRepo = customerRepo;
        _whatsAppService = whatsAppService;
    }

    public async Task<BroadcastMessageResult> Handle(BroadcastMessageCommand request, CancellationToken ct)
    {
        var customers = await _customerRepo.GetAllAsync(x => x.TenantId == request.TenantId, ct);

        int sent = 0, failed = 0;

        foreach (var customer in customers)
        {
            if (string.IsNullOrWhiteSpace(customer.Phone)) { failed++; continue; }
            try
            {
                await _whatsAppService.SendCustomMessageAsync(customer.Phone, request.Message);
                sent++;
            }
            catch
            {
                failed++;
            }
        }

        return new BroadcastMessageResult
        {
            TotalCustomers = customers.Count,
            Sent = sent,
            Failed = failed,
        };
    }
}
