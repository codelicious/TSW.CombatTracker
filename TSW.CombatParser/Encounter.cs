using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace TSW.CombatParser
{
	public class Encounter
	{
		public DateTime Start { get; private set; }
		public DateTime End { get; private set; }
		public TimeSpan Duration
		{
			get
			{
				if (End >= Start)
					return End - Start;
				else
					return TimeSpan.Zero;
			}
		}

		public AttackCollection Attacks { get; private set; }
		public List<Character> Characters { get; private set; }

		public Encounter(DateTime start)
		{
			Attacks = new AttackCollection();
			Characters = new List<Character>();

			Start = start;
			End = start + TimeSpan.FromSeconds(1.0);

			IdleTimeToEncounterEnd = TimeSpan.FromSeconds(60.0);
		}

		public void AddAttack(Attack attack)
		{
			Attacks.Add(attack);
			if (attack.Timestamp > End)
				End = attack.Timestamp;
		}

		public TimeSpan IdleTimeToEncounterEnd { get; set; }

		public bool IsEncounterEnded(DateTime timestamp)
		{
			if (timestamp - End > IdleTimeToEncounterEnd)
				return true;
			else
				return false;
		}
	}

	public class EncounterCollection : ObservableCollection<Encounter>
	{
	}
}
