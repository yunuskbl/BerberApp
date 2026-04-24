using BerberApp.Application.Common.Interfaces;
using BerberApp.Application.Tenant.Commands;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BerberApp.Application.Tenant.Handlers
{
    public class CreateTenantCommandHandler : IRequestHandler<CreateTenantCommand, Guid>
    {
        private readonly IAppDbContext _context;

        public CreateTenantCommandHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<Guid> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
        {
            var tenant = new Domain.Entities.Tenant
            {
                Name = request.Name,
                Subdomain = request.Subdomain,
                Phone = request.Phone,
                Address = request.Address,
                IsActive = true
            };

            _context.Tenants.Add(tenant);
            await _context.SaveChangesAsync(cancellationToken);

            return tenant.Id;
        }
    }
}
