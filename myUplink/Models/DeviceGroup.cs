namespace MyUplinkSmartConnect.Models
{
    public class DeviceGroup
    {
        public string userRole { get; set; }
        public string id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string addressId { get; set; }
        //public Address address { get; set; }
        public string latitude { get; set; }
        public string longitude { get; set; }
        public bool alarm { get; set; }
        public List<object> alarms { get; set; }
        public bool online { get; set; }
        public string role { get; set; }
        public object invitationUserName { get; set; }
        public object invitationUserId { get; set; }
        public object tags { get; set; }
        public string brandId { get; set; }
        public List<Device> devices { get; set; }
        public bool notifyByEmail { get; set; }
        public bool notifyByPush { get; set; }
        public string serialNumber { get; set; }
        public bool isFound { get; set; }
        public bool hasAddress { get; set; }
    }
}
