using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BerberApp.Application.Common.CQRS;

namespace BerberApp.Application.Common.CQRS.Queries;

public class GetAllQuery<TDto> : IQuery<List<TDto>>
{
    public Guid TenantId { get; set; }
}
