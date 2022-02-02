# example_mastercaster
A selection of the C# code used in Master Caster.

<b>spellDraw</b> tracks the player's touch position and adds line renderers to show them what they have drawn. It automatically handles classifying the gesture shape once the player stops drawing, creates the corresponding ingredient object when a UI button is pressed, and deletes lines too small to be properly recognised.

<b>gameHandler</b> is the main gameplay script that determines whether the ingredient the player created is correct for the current potion, if there is one. It enables or disables the ability to draw based on whether they have lives remaining, the game is paused, or the UI to check the spell list is blocking the drawing area.

<b>potionGeneration</b> is the code that handles which potions a customer can request based the game's difficulty setting (how long the player's run had been going.) It is also the system that passes the current potion's spell combination to the gameHandler and keeps track of customer patience.
