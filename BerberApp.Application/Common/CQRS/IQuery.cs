using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MediatR;

namespace BerberApp.Application.Common.CQRS;

public interface IQuery<TResponse> : IRequest<TResponse> { }