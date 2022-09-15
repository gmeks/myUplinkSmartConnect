﻿using MyUplinkSmartConnect.Models;

namespace MyUplinkSmartConnect.ExternalPrice
{
    public class ElectricityPriceInformation : IEquatable<ElectricityPriceInformation>
    {
        public ElectricityPriceInformation()
        {
            Id = Guid.NewGuid();
            Price = 0;
            Start = DateTime.MinValue;
            End = DateTime.MinValue;
            HeatingMode =  HeatingMode.HeathingDisabled;
        }

        public Guid Id { get; set; }

        public double Price { get; set; }

        public DateTime Start { get; set; }

        public DateTime End { get; set; }

        public bool Equals(ElectricityPriceInformation? other)
        {
            return Id.Equals(other?.Id);
        }

        public HeatingMode HeatingMode { get; set; }

        public double GetMaximumCost()
        {
            var currentKwh = CurrentState.ModeLookup.GetHeatingPowerInKwh(HeatingMode);

            return currentKwh > 0 ? (Price * currentKwh) : 0;
        }
    }
}
