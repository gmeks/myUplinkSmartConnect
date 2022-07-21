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
         image: erlingsaeterdal/myuplinksmartconnect:latest
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
        #Remove comment from below line for extended logging
        #' - LogLevel=Debug

* How far this is taken depends alot of to what degree others use it *

**Configuration explained:**
---
Read about the settings here: https://github.com/gmeks/myUplinkSmartConnect