using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WorkmonitorLib
{
    public interface IWorkmonitorUtils
    {
        System.Collections.Generic.List<string> GetOnlineUsers();

        double GetWorkingHours(string userName, DateTime periodBegin, DateTime periodEnd);
    }
}
