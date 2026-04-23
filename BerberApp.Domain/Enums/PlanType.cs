using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BerberApp.Domain.Enums;

/// <summary>
/// Subscription plan types
/// </summary>
public enum PlanType
{
    /// <summary>
    /// Temel plan - Tüm temel özellikler
    /// </summary>
    Basic = 1,

    /// <summary>
    /// Standart plan - İleri özellikler
    /// </summary>
    Standard = 2,

    /// <summary>
    /// Premium plan - Tüm özellikler
    /// </summary>
    Full = 3
}