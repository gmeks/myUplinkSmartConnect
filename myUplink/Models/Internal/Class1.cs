using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace myUplink.ModelsPublic.Internal
{
    public enum WaterheaterSettingsMode
    {
        TargetTempratureSetpoint= 1,
        TargetHeaterWatt=2,
    }

    public enum DesiredPower
    {
        None=0,
        Watt700=1,
        Watt1300 = 2,
        Watt2000 = 3,
    }

    public class WaterHeaterModeSetting
    {
        public WaterheaterSettingsMode settingId { get; set; }
        public int value { get; set; }

        /*
         0 = 0Watt
         1 = 700 Watt
         2 = 1300 Watt
         3 = 2000 Watt
         */
    }

    public class WaterHeaterMode
    {
        public int modeId { get; set; }
        public string name { get; set; }
        public List<WaterHeaterModeSetting> settings { get; set; }
    }

    public class EnumValue
    {
        public string text { get; set; }
        public string name { get; set; }
        public string iconId { get; set; }
        public int value { get; set; }
    }

    public class ModeSetting
    {
        public int settingId { get; set; }
        public string type { get; set; }
        public string name { get; set; }
        public int minValue { get; set; }
        public int maxValue { get; set; }
        public int defaultValue { get; set; }
        public int stepValue { get; set; }
        public double scaleValue { get; set; }
        public int decimalsNumber { get; set; }
        public string unit { get; set; }
        public List<EnumValue> enumValues { get; set; }
    }

    public class ScheduleConfig
    {
        public bool allowRename { get; set; }
        public bool allowUnscheduled { get; set; }
        public bool overrideAvailable { get; set; }
        public bool vacationAvailable { get; set; }
        public bool weeklyAvailable { get; set; }
        public bool weekFormatAvailable { get; set; }
        public bool stopTimeAvailable { get; set; }
        public bool eventDisableAvailable { get; set; }
        public int weeklySchedulesNumber { get; set; }
        public List<ModeSetting> modeSettings { get; set; }
        public int minModesNumber { get; set; }
        public int maxModesNumber { get; set; }
        public int maxEventsNumber { get; set; }
        public int maxVacationsNumber { get; set; }
        public bool forceAllModeSettings { get; set; }
    }


    public class Country
    {
        public string name { get; set; }
        public string countryCode { get; set; }
        public string countryCode2Alpha { get; set; }
        public string countryCode3Alpha { get; set; }
    }

    public class Address
    {
        public string id { get; set; }
        public string lineOne { get; set; }
        public string lineTwo { get; set; }
        public string postalCode { get; set; }
        public string city { get; set; }
        public string region { get; set; }
        public Country country { get; set; }
        public bool isEmpty { get; set; }
    }

    public class Device
    {
        public string id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string type { get; set; }
        public string role { get; set; }
        public object tags { get; set; }
        public object serialNumber { get; set; }
        public string deviceAccess { get; set; }
    }

    public class Group
    {
        public string userRole { get; set; }
        public string id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string addressId { get; set; }
        public Address address { get; set; }
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

    public class MyGroupRoot
    {
        public List<Group> groups { get; set; }
    }
    public class HeaterWeeklyEvent
    {
        public bool enabled { get; set; }
        public int modeId { get; set; }
        public string startDay { get; set; }
        public string startTime { get; set; }
        public object stopDay { get; set; }
        public object stopTime { get; set; }
    }

    public class HeaterWeeklyRoot
    {
        public int weeklyScheduleId { get; set; }
        public string weekFormat { get; set; }
        public List<HeaterWeeklyEvent> events { get; set; }
    }
}
