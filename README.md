**MyUplink Schedule changer.**

This program connect to the myuplink.com and adjust the schedule of [Høiax CONNECTED](https://www.hoiax.no/om-hoiax/articles/hoiax-connected-smart-varmtvannsbereder-med-skylosning) hot water heaters, and adjust the schedule so that it uses electricity when the price is low.

**How does program work:**
It contacts a EU API [transparency.entsoe.eu](https://transparency.entsoe.eu) and gets the electricity price pr hour for your region. It wil then set the schedule for when to increase the water temprature.


**How to use:**
1) Download the program.
2) Configure the application.json
3) Run the program once pr Day, after 14:00  (Before this time, "tomorrows" electricity price is nto ready.)


**Configuration explained:**
 UserName - Your username to myuplink.com ( Sadly the public facing APi, does not allow for reschedules..)
 Password - Your password for myuplink.com 
 WaterHeaterMaxPowerInHours - This is the number of hours pr day the water heater is running full power.
 WaterHeaterMediumPowerInHours - This the number of hours pr day the water heater is running at half power, but with a lower "target temprature"



**Recommended configuration.**

This works for my famility, with 1 adult male and a wife. Both taking showers that last 10+ min pr day.

 1) WaterHeaterMaxPowerInHours = 5
 2) WaterHeaterMediumPowerInHours = 6

*These 2 numbers should be combined, when considering when the water heater is on.*