using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BerberApp.Domain.Enums
{
    /// <summary>
    /// Subscription status
    /// </summary>
    public enum SubscriptionStatus
    {
        Active = 1,
        Inactive = 2,
        Expired = 3,
        Cancelled = 4,
        Pending = 5
    }
}