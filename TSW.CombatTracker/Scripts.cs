﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TSW.CombatTracker
{
	using TSW.CombatParser;

	public class Scripts
	{
		public static void GenerateScripts(Combat combat, string tswFolderName)
		{
			DirectoryInfo tswFolder = new DirectoryInfo(tswFolderName);
			DirectoryInfo scriptsFolder = tswFolder.GetDirectories().FirstOrDefault(d => d.Name.Equals("scripts", StringComparison.InvariantCultureIgnoreCase));
			if (scriptsFolder == null)
				scriptsFolder = Directory.CreateDirectory(tswFolder.FullName + Path.DirectorySeparatorChar + "scripts");

			FileStream s = File.Create(scriptsFolder.FullName + Path.DirectorySeparatorChar + "combat");
			StreamWriter w = new StreamWriter(s);

			var combatantsQuery = from combatant in combat.Characters
								  where !combatant.IsMob
								  orderby combatant.OffensiveHits.TotalDamage descending
								  select combatant;

			double totalDamage = combatantsQuery.Sum(c => c.OffensiveHits.TotalDamage);

			w.WriteLine("Name                Damage     DPS  Crit(%)        Pen(%)     % Total");

			foreach (Character combatant in combatantsQuery)
			{
				string crits = String.Format("{0,5}({1:N1}%)", combatant.OffensiveHits.TotalCrits, combatant.OffensiveHits.CritPercent);
				string penetrated = String.Format("{0,5}({1:N1}%)", combatant.OffensiveHits.TotalPenetrated, combatant.OffensiveHits.PenetratedPercent);

				string line = String.Format("{0,-15} {1,10:N0} {2,7:N1} {3,-13} {4,-13} {5,4:N0}%",
					combatant.Name,
					combatant.OffensiveHits.TotalDamage,
					combatant.OffensiveHits.DPS,
					crits,
					penetrated,
					combatant.OffensiveHits.TotalDamage * 100.0 / totalDamage);
				w.WriteLine(line);
			}

			w.Close();
		}
	}
}
