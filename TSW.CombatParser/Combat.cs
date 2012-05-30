﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Data;
using System.Collections.ObjectModel;

namespace TSW.CombatParser
{
	public class Combat : INotifyPropertyChanged
	{
		public FileInfo CombatLog { get; private set; }

		public EncounterCollection Encounters { get; private set; }

		public List<Character> Characters { get; private set; }

		public Character You { get; private set; }

		public uint TotalLines { get; private set; }

		private CombatParser combatParser;

		private Encounter currentEncounter = null;

		public Combat()
		{
			Encounters = new EncounterCollection();
			Characters = new List<Character>();

			combatParser = new CombatParser();
			combatParser.Hit += combatParser_Hit;
			combatParser.Evade += combatParser_Evade;
			combatParser.Heal += combatParser_Heal;
			combatParser.XP += combatParser_XP;
		}

		public void Reset()
		{
			Characters.Clear();
			CombatLog = null;

			You = null;
			TotalLines = 0;
		}

		public void ProcessLine(string line)
		{
			combatParser.Parse(line);
			++TotalLines;
		}

		private void combatParser_Hit(object sender, HitEventArgs e)
		{
			UpdateEncounter(e.Timestamp);

			Attack attack = new Attack(e);
			currentEncounter.AddAttack(attack);

			Character attacker = FindCharacter(e.Attacker);
			attacker.AddOffensiveHit(attack);

			Character target = FindCharacter(e.Target);
			target.AddDefensiveHit(attack);
		}

		private void combatParser_Evade(object sender, EvadeEventArgs e)
		{
			UpdateEncounter(e.Timestamp);

			Attack evade = new Attack(e);
			Character attacker = FindCharacter(e.Attacker);
			attacker.AddOffensiveEvade(evade);

			Character target = FindCharacter(e.Evader);
			target.AddDefensiveEvade(evade);
		}

		private void combatParser_Heal(object sender, HealEventArgs e)
		{
			UpdateEncounter(e.Timestamp);

			Heal heal = new Heal(e);
			Character healer = FindCharacter(e.Healer);
			healer.AddOffensiveHeal(heal);

			Character healed = FindCharacter(e.Target);
			healed.AddDefensiveHeal(heal);
		}

		private void combatParser_XP(object sender, XpEventArgs e)
		{
			if (You != null)
				You.AddXp(e);
		}

		private void UpdateEncounter(DateTime timestamp)
		{
			if (currentEncounter == null || currentEncounter.IsEncounterEnded(timestamp))
			{
				currentEncounter = new Encounter(timestamp);
				Encounters.Add(currentEncounter);
			}
		}

		public Character FindCharacter(string characterName)
		{
			Character character = Characters.FirstOrDefault(c => c.Name.Equals(characterName, StringComparison.InvariantCultureIgnoreCase));
			if (character == null)
			{
				character = new Character();
				character.Name = characterName;

				if (characterName.Equals("You", StringComparison.InvariantCultureIgnoreCase))
				{
					character.IsYou = true;
					if (You == null)
						You = character;
				}

				Characters.Add(character);
			}

			return character;
		}

		public void Test()
		{
			Characters.ToList().ForEach(c => c.AddOffensiveHit(new Attack(new HitEventArgs() { Attacker = "Big Mob", AttackType = "attack", Target = "You", Damage = 13, Type = "physical", Timestamp = DateTime.Now })));
		}

		public void Refresh()
		{
			Characters.ToList().ForEach(c => c.Refresh());

			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(null));
		}

		public event PropertyChangedEventHandler PropertyChanged;
	}
}
