using MediatR;

namespace BerberApp.Application.Tenant.Commands;

public class CreateBranchCommand : IRequest<CreateBranchResult>
{
    public Guid ParentTenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
}

public class CreateBranchResult
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
}
