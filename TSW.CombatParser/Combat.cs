using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Data;

namespace TSW.CombatParser
{
	public class Combat : INotifyPropertyChanged
	{
		public ObservableCollection<Character> Characters { get; private set; }

		public FileInfo CombatLog { get; private set; }

		private CombatParser combatParser;

		private Character you = null;

		private uint totalLines = 0;

		public Combat()
		{
			Characters = new ObservableCollection<Character>();

			combatParser = new CombatParser();
			combatParser.Hit += combatParser_Hit;
			combatParser.Evade += combatParser_Evade;
			combatParser.Heal += combatParser_Heal;
			combatParser.XP += combatParser_XP;
		}

		public Character You
		{
			get { return you; }
			private set
			{
				if (value != you)
				{
					you = value;
					OnPropertyChanged("You");
				}
			}
		}
		
		public uint TotalLines
		{
			get { return totalLines; }
			private set
			{
				if (value != totalLines)
				{
					totalLines = value;
					OnPropertyChanged("TotalLines");
				}
			}
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
			Character attacker = FindCharacter(e.Attacker);
			attacker.AddOffensiveHit(e);

			Character target = FindCharacter(e.Target);
			target.AddDefensiveHit(e);
		}

		private void combatParser_Evade(object sender, EvadeEventArgs e)
		{
			Character attacker = FindCharacter(e.Attacker);
			attacker.AddOffensiveEvade(e);

			Character target = FindCharacter(e.Evader);
			target.AddDefensiveEvade(e);
		}

		private void combatParser_Heal(object sender, HealEventArgs e)
		{
			Character healer = FindCharacter(e.Healer);
			healer.AddOffensiveHeal(e);

			Character healed = FindCharacter(e.Target);
			healed.AddDefensiveHeal(e);
		}

		private void combatParser_XP(object sender, XpEventArgs e)
		{
			if (You != null)
				You.AddXp(e);
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

		#region INotifyPropertyChanged implementation
		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
		#endregion
	}
}
