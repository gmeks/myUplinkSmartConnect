# MyUplink Schedule changer

This program connect to the myuplink.com and adjust the schedule of [Høiax CONNECTED](https://www.hoiax.no/om-hoiax/articles/hoiax-connected-smart-varmtvannsbereder-med-skylosning) hot water heaters, and adjust the schedule so that it uses electricity when the price is lowest. This is done by fetching price information on a pr hour basic, for today and tomorrow and building the schedule
It also allows you to periodicity publish current values from the heater via mqtt to your home automation of choice.
When changing the schedule it gets the price information for your region from [transparency.entsoe.eu](https://transparency.entsoe.eu) or other 3d parti API.

![Screenshot](schedule_screenshot.png)

### Schedule building rules

#### Old method
Water heater will heat water when its bellow a certain temprature, without considering price. Considering alot of showing and cooking happens around the same time, this means heating water at peak electricity price.

#### Price based rules (Default)
This schedule builder simply uses cost, and will schedule heating the water (Consider charging a batteri) when the prices are low.  The main issue with this method is that water heaters loose a fair bit of heat over time, and having realy hot water at 02:00 may not be productive. The heat thats lost in the water, basicly gets added to the house as heat ( Simular cost effect as regualar electric heater)

#### Energi based rules
This schedule builder is provided configured peak times, and will attemt to use cheapest posible electricity to heat the tank to reach a target energi level in the tank. This will use less electricity then Price based rules, but may use it at bit higher price.

## Legal

This is a 3d party program made to work against [myuplink](https://myuplink.com), and not afflilated with them in any way.

## How to use

1) Download the program.
2) Configure the application.json
3) Install with *myuplink install* (Or you can just run it)
4) Start the service or reboot ( MyUplink-smartconnect.exe start )

Deploy via docker:
<https://hub.docker.com/r/erlingsaeterdal/myuplinksmartconnect>

## Future plan

- Full integration with home assistant as a integration? ( This depends a bit on having someone know the homeassistant part, to help build a homeassistant integration that can connect to the docker container over MTQQ)

*How far this is taken depends alot of to what degree others use it*

## Configuration explained:

- UserName - Your username to myuplink.com ( Sadly the public facing APi, does not allow for reschedules..)
- Password - Your password for myuplink.com
- ConsoleLogLevel - Sets the logs shown in the console window (Default Information), posible values Verbose,Debug,Information,Warning,Error,Fatal

## Heating schedule based settings

- ChangeSchedule - Change schedule automaticly based on defined rules (Default on.)
- EnergiBasedCostSaving - Energi based rules instead of pure price based ( More below ).
- WaterHeaterMaxPowerInHours - This is the number of hours pr day the water heater is running full power.
- WaterHeaterMediumPowerInHours - This the number of hours pr day the water heater is running at half power, but with a lower "target temperature"
- MediumPowerTargetTemperature - Target temperature in medium mode, default 50c
- HighPowerTargetTemperature - Target temperature in high mode, default 70c
- EnergiBasedPeakTimes - When using Energi based rules, this sets the target where the water should be at HighPowerTargetTemperature. Valid values are: name of day ( example: monday)

## Optional mqtt Configuration, used to connect to smarthouse solution like homeassistant

- MQTTServer - IP address or FQDN of mqtt, this is optional.
- MQTTServerPort - The port of the mqtt server, this is optional.
- MQTTUserName - If the mqtt requires username and passord.
- MQTTPassword - If the mqtt requires username and passord.
- MQTTLogLevel - Sets the logs shown in the console window (Default Warning), posible values Verbose,Debug,Information,Warning,Error,Fatal

## Recommended configuration.

This works for my famility, with 1 adult male and a wife. Both taking showers that last 10+ min pr day.

### Price based rules

- WaterHeaterMaxPowerInHours = 5
- WaterHeaterMediumPowerInHours = 10

### Energi based rules

- EnergiBasedCostSaving=true
- EnergiBasedPeakTimes=weekday6,weekday21,weekday23,weekend11,weekend23
- WaterHeaterMaxPowerInHours=3
- WaterHeaterMediumPowerInHours=11
- HighPowerTargetTemprature=60

EnergiBasedPeakTimes takes a commma seperated list of weekdays with clock at the end. Example pr monday21 or weekday21.

 *Heater should be in schedule mode.*
*If your wondering how many hours pr day the water heater is potensialy on, please combine the 2 above numbers.*

## FAQ

Q) What are the different scheduling rules

> Simple price based ( Default) - This rule is very simple we use the WaterHeaterMaxPowerInHours/WaterHeaterMediumPowerInHours and run the heater in the cheapest hours.
> Energi based - In this mode, we attemt to use WaterHeaterMaxPowerInHours/WaterHeaterMediumPowerInHours to make sure we reach a target energi at targeted times.

Q) What are modes and how are they used?

> The heater has 5 different modes ( Mode is a combination of target temperature for water and how much power to use to heat it.), this program uses 3 of them ( And will automaticly configure this)
> M6: Target temp 70c ( 2000w)
> M5: Target temp 50c ( 700w)
> M4: Target temp 50c ( 0w)
> It will then use these modes based on electricity price.

Q) When does the application change the schedule?

> The application will change the schedule at a set thats different for each user. You will see you time in the logs ( [16:47:05 INF] Target Schedule change time is 15:17  ) We spread out the load of when to change the schedule, simply because having all heaters >attemting to do it at the same is bad practice and might overload the remote apis.

Q) When the heater is set to start heating at XX:YY it starts an hour earlier/later then desired.
> There is a hidden Timezone setting on myuplink.com -> System Menu -> Settings -> Timezone offset

Q) Can i force it to update the schedule?
> Yes, if you restart the service/docker after the target schedule change time ( But before midnight), it will force a schedule update.

## Setup Homeassistant sensor, in configuration.yml

Please note from my personal experimentation most of the values that the heater is reporting is best case just guessing. You can trust how much energi its using or has used.


18760NE2240322014631 needs to be replaced with the ID of your hotwater heater, simplest way to find out is to just read the console output of this application*

>      mqtt:
>       sensor:
>         - name: "Waterheater current temprature"
>           state_topic: "heater/18760NE2240322014631/CurrentTemprature"
>           unique_id: "Waterheater.CurrentTemprature"
>           state_class: measurement    
>           unit_of_measurement: "°C"
>     
>         - name: "Waterheater current Watt"
>           state_topic: "heater/18760NE2240322014631/EstimatedPower"
>           unique_id: "Waterheater.EstimatedPower"
>           device_class: power    
>           state_class: measurement    
>           unit_of_measurement : W     
>       
>         - name: "Target temprature"
>           state_topic: "heater/18760NE2240322014631/TargetTemprature"
>           unique_id: "Waterheater.TargetTemprature"
>           unit_of_measurement: "°C"
>     
>         - name: "Energy Total(Waterheater)"
>           state_topic: "heater/18760NE2240322014631/EnergyTotal"
>           unique_id: "Waterheater.EnergyTotal"   
>           device_class: energy
>           state_class: total_increasing
>           unit_of_measurement : kWh    
>     
>         - name: "Energi in tank"
>           state_topic: "heater/18760NE2240322014631/EnergiStored"
>           unique_id: "Waterheater.EnergiStored"
>           device_class: power
>           unit_of_measurement : kWh    
>     
>         - name: "Tank fill level"
>           state_topic: "heater/18760NE2240322014631/FillLevel"
>           unique_id: "Waterheater.FillLevel"    
>           unit_of_measurement: '%'
>     
>         - name: "Last schedule change"
>           state_topic: "heater/18760NE2240322014631/LastScheduleChangeInHours"
>           unique_id: "Waterheater.LastScheduleChange" 
>           unit_of_measurement: "Hours ago"
>
>         - name: "MyUplinkSmartConnectLogs"
>           state_topic: "heater/LogEntry"
>           unique_id: "MyUplinkSmartConnectLogs" 
>
>         - name: "MyUplinkSmartConnect status"
>           state_topic: "heater/ServiceStatus"
>           unique_id: "MyUplinkSmartConnectStatus" 