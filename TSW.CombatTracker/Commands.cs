using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace TSW.CombatTracker
{
	public class CommandActions
	{
		public static RoutedCommand GenerateScripts { get; private set; }

		static CommandActions()
		{
			GenerateScripts = new RoutedCommand();
		}
	}
}
