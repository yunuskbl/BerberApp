using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BerberApp.Domain.Entities;
using BerberApp.Domain.Enums;

namespace BerberApp.Application.Common.Interfaces;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user,PlanType planType);
    string GenerateRefreshToken();
}