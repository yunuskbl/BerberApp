using BerberApp.Application.Appointment.DTOs;
using BerberApp.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BerberApp.Infrastructure.Services
{
    public class LinkNotificationService : INotificationService
    {
        private readonly string _baseUrl;

        public LinkNotificationService(IConfiguration config)
        {
            _baseUrl = config["AppSettings:BaseUrl"] ?? "https://berberapp.com.tr";
        }

        public Task SendAppointmentReceivedAsync(string recipient, AppointmentStatusDto dto)
        {
            // Şimdilik sadece log — ileride WhatsApp mesajı buraya gelir
            var link = $"{_baseUrl}/randevu/{dto.Id}";
            Console.WriteLine($"[NOTIFICATION] Randevu alındı linki: {link} → {recipient}");
            return Task.CompletedTask;
        }

        public Task SendAppointmentConfirmedAsync(string recipient, AppointmentStatusDto dto)
        {
            var link = $"{_baseUrl}/randevu/{dto.Id}";
            Console.WriteLine($"[NOTIFICATION] Randevu onaylandı linki: {link} → {recipient}");
            return Task.CompletedTask;
        }

        public Task SendAppointmentCancelledAsync(string recipient, AppointmentStatusDto dto)
        {
            var link = $"{_baseUrl}/randevu/{dto.Id}";
            Console.WriteLine($"[NOTIFICATION] Randevu iptal linki: {link} → {recipient}");
            return Task.CompletedTask;
        }
    }
}
