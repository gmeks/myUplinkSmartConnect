using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyUplinkSmartConnect.Models
{

    public enum connectionState
    {
        Offline=0,
        Online=1,
    }
    public class SystemDevice
    {
        public string? id { get; set; }

        public connectionState connectionState { get; set; }

        public string? currentFwVersion { get; set; }
    }

    public class myUplinkSystem
    {
        public string? systemId { get; set; }
        public string? name { get; set; }
        public string? securityLevel { get; set; }
        public bool hasAlarm { get; set; }
        public string? country { get; set; }
        public List<SystemDevice>? devices { get; set; }
    }

    public class RootDevices
    {
        public int page { get; set; }
        public int itemsPerPage { get; set; }
        public int numItems { get; set; }
        public List<myUplinkSystem>? systems { get; set; }
    }
}
