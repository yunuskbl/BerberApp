using BerberApp.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace BerberApp.Infrastructure.Services;

public class AppSettingsService : IAppSettings
{
    public string FrontendBaseUrl { get; }

    public AppSettingsService(IConfiguration config)
    {
        FrontendBaseUrl = config["AppSettings:FrontendBaseUrl"] ?? "https://ayarliyo.com";
    }
}
