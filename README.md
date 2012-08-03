TSW Damage Parser
-----------------

[TSW.CombatTracker 1.0.0 - Installer][dl] (750 KB) - 27 Jul 2012

[dl]: https://github.com/downloads/codelicious/TSW.CombatTracker/TSW.CombatTracker.msi

Requirements and Installation
-----------------------------

TSW.CombatTracker can be built in Visual Studio 2010 or downloaded as a Windows Installer package.

TSW.CombatTracker requires .NET 4.0. (I'm not sure what it does if the runtime isn't installed on your computer. I believe it will try to install it automatically but I don't know that for sure and I don't have a computer without .NET 4.0 to test it on.)


Usage
-----

When TSW.CombatTracker runs, it attempts to stay on top of other applications. This allows it to remain visible even while playing the game. However, in order for it to be visible, The Secret World must be running in one of the two windowed modes. If TSW is running in fullscreen mode, it will still be possible to ALT+TAB out of the game to view combat numbers but it will be far less convenient.

Basic operation of TSW.CombatTracker is very simple:

1. Start The Secret World
2. Start TSW.CombatTracker (note that these two can be started in any order)
3. Click the "Process combat log" button (the button descriptions can be seen by hovering the mouse over them)
4. Start combat logging in The Secret World with the "/logcombat on" method
5. Select a character whose damage has been logged with the character combobox

The first time TSW.CombatTracker is executed, it will prompt for the location of the The Secret World game folder. This is the location where TSW writes its combat log files and where TSW.CombatTracker will look for log files when started. If the location of the TSW game folder changes or becomes invalid, TSW.CombatTracker will prompt for a new location for the game folder.

TSW.CombatTracker has a very minimal interface. There are five buttons on the right hand side of the title bar that are used to control it. They are, from left to right:

- Process combat logs
- Clear statistics
- Transparent
- Minimize
- Close

### Process combat logs

The "Process combat logs" button toggles the actual running of the combat tracker. Clicking on the button causes several things to happen:

- First, TSW.CombatTracker checks the TSW game folder for the newest combat log, opens it, and processes all of the current entries in that log.
- It then continues to monitor the log for new entries and updates the statistics accordingly.
- TSW.CombatTracker will also monitor the TSW game folder for newer combat logs and will automatically switch to those logs as they are created. Thus, it becomes very simple to capture fresh statistics by simply performing a "/logcombat off /logcombat on" sequence in The Secret World.

Clicking the "Process combat logs" button a second time terminates monitoring of combat logs.

Clicking the "Process combat logs" button while holding the Control key, will bring up a file selection dialog and allow you to load a specific combat log. The entire log will be read in and the statistics displayed for that file. In this case, TSW.CombatTracker will not monitor the log for further changes since there won't be any changes in older files. It will also not monitor for new combat logs until you click "Process combat logs" again to start processing the default combat log.

### Clear statistics

Pressing the "Clear statistics" button will zero out any damage or healing given or received for all players and NPC's. This provides a convenient way to start logging fresh damage numbers for a new encounter.

### Transparent

The "Transparent" button will make the entire TSW.CombatTracker screen transparent so that the game screen can still be seen while the parser is active.

### Minimize

The "Minimize" button will cause the TSW.CombatTracker window to shrink down to its title bar so it consumes the minimum space on the screen while playing. Hovering the mouse over the title bar will temporarily expand the window again without deactivating the game. This provides a convenient way to quickly inspect updated damage numbers without interfering with game play.

The window can be permanently brought back to full size by pressing the "Minimze" button again.

### Close

The "Close" button exits the application.


Known Issues
------------

Damage numbers aren't encounter-based so there is some inconsistency in DPS between individual attack types. This is because DPS is calculated over the seconds from the first time of a particular attack to the last time of that attack.

Absorb and reflect damage are not being calculated or displayed.

Heal % of total isn't actually calculated, even though there is a field for it.