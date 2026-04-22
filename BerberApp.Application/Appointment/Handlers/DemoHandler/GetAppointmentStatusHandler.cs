using BerberApp.Application.Appointment.DTOs;
using BerberApp.Application.Common.Exceptions;
using BerberApp.Application.Common.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BerberApp.Application.Appointment.Handlers.DemoHandler
{
    // GetAppointmentStatusHandler.cs
    public class GetAppointmentStatusHandler : IRequestHandler<GetAppointmentStatusQuery, AppointmentStatusDto>
    {
        private readonly IGenericRepository<AppointmentEntity> _appointmentRepo;
        private readonly IGenericRepository<CustomerEntity> _customerRepo;
        private readonly IGenericRepository<ServiceEntity> _serviceRepo;
        private readonly IGenericRepository<StaffEntity> _staffRepo;

        public GetAppointmentStatusHandler(
            IGenericRepository<AppointmentEntity> appointmentRepo,
            IGenericRepository<CustomerEntity> customerRepo,
            IGenericRepository<ServiceEntity> serviceRepo,
            IGenericRepository<StaffEntity> staffRepo)
        {
            _appointmentRepo = appointmentRepo;
            _customerRepo = customerRepo;
            _serviceRepo = serviceRepo;
            _staffRepo = staffRepo;
        }

        public async Task<AppointmentStatusDto> Handle(GetAppointmentStatusQuery request, CancellationToken ct)
        {
            var appointment = await _appointmentRepo.GetAsync(
                x => x.Id == request.AppointmentId && x.TenantId == request.TenantId, ct);

            if (appointment is null)
                throw new NotFoundException("Randevu", request.AppointmentId);

            var customer = await _customerRepo.GetByIdAsync(appointment.CustomerId, ct);
            var service = await _serviceRepo.GetByIdAsync(appointment.ServiceId, ct);
            var staff = await _staffRepo.GetByIdAsync(appointment.StaffId, ct);

            return new AppointmentStatusDto
            {
                Id = appointment.Id,
                CustomerName = customer?.FullName ?? "",
                ServiceName = service?.Name ?? "",
                StaffName = staff?.FullName ?? "",
                StartTime = appointment.StartTime,
                EndTime = appointment.EndTime,
                Status = appointment.Status.ToString(),
                Price = service?.Price ?? 0,
                Currency = service?.Currency ?? "TRY"
            };
        }
    }
}
