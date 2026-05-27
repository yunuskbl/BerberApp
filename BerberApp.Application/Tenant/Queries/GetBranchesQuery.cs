using MediatR;

namespace BerberApp.Application.Tenant.Queries;

public class GetBranchesQuery : IRequest<List<BranchDto>>
{
    public Guid ParentTenantId { get; set; }
}

public class BranchDto
{
    public Guid   Id        { get; set; }
    public string Name      { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public string? Phone    { get; set; }
    public string? Address  { get; set; }
    public string? City     { get; set; }
    public bool   IsActive  { get; set; }
    public DateTime CreatedAt { get; set; }
}
