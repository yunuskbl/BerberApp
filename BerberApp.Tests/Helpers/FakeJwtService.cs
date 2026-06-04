using BerberApp.Application.Common.Interfaces;
using BerberApp.Domain.Entities;
using BerberApp.Domain.Enums;

namespace BerberApp.Tests.Helpers;

/// <summary>Gerçek JWT üretmeyen, test için sabit token döndüren servis.</summary>
public class FakeJwtService : IJwtTokenService
{
    public string GenerateAccessToken(User user, PlanType planType) => "fake-access-token";
    public string GenerateRefreshToken() => "fake-refresh-token";
}
