# Em-AltJobs

A set of alternative job/s, written in C#, for FiveM.

## Jobs

- <b>Organ Delivery</b>
Can be found behind the morgue and requires the player to be in the drivers seat of a vehicle.

## Dependencies

- <a href="https://github.com/Davenport-Physics/Em-PlayExternalSounds-FiveM">PlayExternalSounds</a>

## Events Triggered

AltJobs triggers the following events, so be sure to add the corresponding event handlers.

`ShowInformationLeft(int ms_to_show_msg, string message)`

Shows a message, for the specific milliseconds, as a pop up.

`addItem(string item_to_add, int amount_to_add, bool force_into_inventory)`

Adds the specified amount of items to the players inventory.

`removeItem(string item_to_remove, int amount_to_remove)`

Removes the specified item from the players inventory.

`addMoney(int money_to_add)`

Adds a specific amount of money to the players wallet.
