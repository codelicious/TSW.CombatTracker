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

	public class EncounterCollection : ICollection<Encounter>
	{
		private List<Encounter> encounters = new List<Encounter>();

		public void Add(Encounter item)
		{
			encounters.Add(item);
		}

		public void Clear()
		{
			throw new NotImplementedException();
		}

		public bool Contains(Encounter item)
		{
			return encounters.Contains(item);
		}

		public void CopyTo(Encounter[] array, int arrayIndex)
		{
			encounters.CopyTo(array, arrayIndex);
		}

		public int Count
		{
			get { return encounters.Count; }
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		public bool Remove(Encounter item)
		{
			return encounters.Remove(item);
		}

		public IEnumerator<Encounter> GetEnumerator()
		{
			return encounters.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return ((System.Collections.IEnumerable)encounters).GetEnumerator();
		}
	}
}
