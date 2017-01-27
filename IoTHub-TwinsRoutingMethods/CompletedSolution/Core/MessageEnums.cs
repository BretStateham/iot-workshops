using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core
{

    public enum MessageProperty
    {
        Type,
        Status,
        Severity
    }

    public enum Type
    {
        ConfigChage
    }

    public enum Severity
    {
        Information,
        Critical
    }

    public enum Status
    {
        Pending,
        Rejected,
        Accepted,
        PreconditionFailed
    }

    public enum DeviceMethods
    {
        AcceptDesiredProperties
    }
}
