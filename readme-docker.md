**MyUplink Schedule changer.**

This program connect to the myuplink.com and adjust the schedule of [Høiax CONNECTED](https://www.hoiax.no/om-hoiax/articles/hoiax-connected-smart-varmtvannsbereder-med-skylosning) hot water heaters, and adjust the schedule so that it uses electricity when the price is low.
It also allows you to periodicity publish current values from the heater via mqtt to your home automation of choice.

![Screenshot](schedule_screenshot.png)

**How does program work:**
---
It contacts a EU API [transparency.entsoe.eu](https://transparency.entsoe.eu) and gets the electricity price pr hour for your region. It will then set the schedule for when to increase the water temprature.

**Legal**
This is a 3d party program made to work against [myuplink](https://myuplink.com), and not afflilated with them in any way.

Github link: https://github.com/gmeks/myUplinkSmartConnect

**Docker-compose:**
---
     version: "2"
     services:
       server:
         image: erlingsaeterdal/myuplinksmartconnect:1.0.0
         restart: always
         pull_policy: always
         environment:
           - TZ=Europe/Oslo     
           - UserName=eks
           - Password=UziqmhE95sW2hnt
           - IsInsideDocker=1
           - CheckRemoteStatsIntervalInMinutes=1      
           - WaterHeaterMaxPowerInHours=5
           - WaterHeaterMediumPowerInHours=8
           - PowerZone=NO2
           - MQTTServer=192.168.50.10
           - MQTTServerPort=1883
           - MQTTUserName=
           - MQTTPassword=

* How far this is taken depends alot of to what degree others use it *

**Configuration explained:**
---
- UserName - Your username to myuplink.com ( Sadly the public facing APi, does not allow for reschedules..) 
- Password - Your password for myuplink.com 
- WaterHeaterMaxPowerInHours - This is the number of hours pr day the water heater is running full power.
- WaterHeaterMediumPowerInHours - This the number of hours pr day the water heater is running at half power, but with a lower "target temprature"
- MediumPowerTargetTemprature - Target temprature in medium mode, default 50c
- HighPowerTargetTemprature - Target temprature in high mode, default 70c

**Optional mqtt Configuration, used to connect to smarthouse solution like homeassistant**
- MQTTServer - IP address or FQDN of mqtt, this is optional.
- MQTTServerPort - The port of the mqtt server, this is optional.
- MQTTUserName - If the mqtt requires username and passord.
- MQTTPassword - If the mqtt requires username and passord.

** Setup Homeassistant sensor, in configuration.yml**
*18760NE2240322014631 needs to be replaced with the ID of your hotwater heater, simplest way to find out is to just read the console output of this application*

     sensor:
       - platform: mqtt
         state_topic: "heater/18760NE2240322014631/TargetTemprature"
         unique_id: "WaterHeaterTargetTemprature"
         name: "Target temprature"
         unit_of_measurement: "°C"
         
       - platform: mqtt
         state_topic: "heater/18760NE2240322014631/CurrentTemprature"
         unique_id: "WaterHeaterCurrentTemprature"
         name: "Current temprature"
         unit_of_measurement: "°C"
         
       - platform: mqtt
         state_topic: "heater/18760NE2240322014631/EstimatedPower"
         unique_id: "WaterHeaterEstimatedPower"    
         name: "Current Watt"
         device_class: power    
         state_class: measurement    
         unit_of_measurement : W   
        
      - platform: mqtt
        state_topic: "heater/18760NE2240322014631/EnergyTotal"
        unique_id: "WaterHeaterEnergyTotal"
        name: "Energy Total"    
        device_class: power
        state_class: total_increasing
        unit_of_measurement : kWh    
        
      - platform: mqtt
        state_topic: "heater/18760NE2240322014631/EnergiStored"
        unique_id: "WaterHeaterEnergiStored"
        name: "Energi in tank"
        device_class: power
        unit_of_measurement : kWh        
        
      - platform: mqtt
        state_topic: "heater/18760NE2240322014631/FillLevel"
        name: "Tank fill level"
        unique_id: "WaterHeaterFillLevel"    
        unit_of_measurement: '%'

      - platform: mqtt
        state_topic: "heater/18760NE2240322014631/LastScheduleChangeInHours"
        name: "Last schedule change"
        unique_id: "LastScheduleChange"    


**Recommended configuration.**
---
This works for my famility, with 1 adult male and a wife. Both taking showers that last 10+ min pr day.

 - WaterHeaterMaxPowerInHours = 5
 - WaterHeaterMediumPowerInHours = 6

*If your wondering how many hours pr day the water heater is potensialy on, please combine the 2 above numbers.*