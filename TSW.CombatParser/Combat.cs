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

		public Character You { get; private set; }
		
		public FileInfo CombatLog { get; private set; }

		private uint totalAttacks = 0;
		private uint totalEvades = 0;
		private uint totalHeals = 0;
		private uint totalLines = 0;

		private CombatParser combatParser;

		public Combat()
		{
			Characters = new ObservableCollection<Character>();

			combatParser = new CombatParser();
			combatParser.Hit += combatParser_Hit;
			combatParser.Evade += combatParser_Evade;
			combatParser.Heal += combatParser_Heal;
			combatParser.XP += combatParser_XP;
		}

		public uint TotalAttacks
		{
			get { return totalAttacks; }
			private set
			{
				if (value != totalAttacks)
				{
					totalAttacks = value;
					OnPropertyChanged("TotalAttacks");
				}
			}
		}

		public uint TotalEvades
		{
			get { return totalEvades; }
			private set
			{
				if (value != totalEvades)
				{
					totalEvades = value;
					OnPropertyChanged("TotalEvades");
				}
			}
		}

		public uint TotalHeals
		{
			get { return totalHeals; }
			private set
			{
				if (value != totalHeals)
				{
					totalHeals = value;
					OnPropertyChanged("TotalHeals");
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
			OnPropertyChanged("You");

			TotalAttacks = 0;
			TotalEvades = 0;
			TotalHeals = 0;
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
			attacker.AddAttack(e);

			Character target = FindCharacter(e.Target);
			target.AddHit(e);

			++TotalAttacks;
		}

		private void combatParser_Evade(object sender, EvadeEventArgs e)
		{
			Character attacker = FindCharacter(e.Attacker);
			attacker.AddEvade(e);

			++TotalEvades;
		}

		private void combatParser_Heal(object sender, HealEventArgs e)
		{
			Character healer = FindCharacter(e.Healer);
			healer.AddHeal(e);

			Character healed = FindCharacter(e.Target);
			healed.AddHealTaken(e);

			++TotalHeals;
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
					{
						You = character;
						OnPropertyChanged("You");
					}
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
