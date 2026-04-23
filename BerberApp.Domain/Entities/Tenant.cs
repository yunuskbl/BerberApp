using BerberApp.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace BerberApp.Domain.Entities
{
    public class Tenant : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Subdomain { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public string? Phone { get; set; }
        public string? NotificationPhone { get; set; }
        public string? Address { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation
        public ICollection<User> Users { get; set; } = new List<User>();
        public ICollection<Staff> Staff { get; set; } = new List<Staff>();
        public ICollection<Service> Services { get; set; } = new List<Service>();
        public ICollection<Customer> Customers { get; set; } = new List<Customer>();
        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}