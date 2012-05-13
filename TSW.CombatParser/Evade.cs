using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TSW.CombatParser
{
	public class Evade
	{
		public DateTime Timestamp { get; private set; }
		public string Attacker { get; private set; }
		public string Evader { get; private set; }
		public string AttackType { get; private set; }

		public Evade(EvadeEventArgs e)
		{
			Timestamp = e.Timestamp;
			Attacker = e.Attacker;
			Evader = e.Evader;
			AttackType = e.AttackType;
		}
	}

	public class EvadeCollection : ICollection<Evade>
	{
		private List<Evade> evades = new List<Evade>();

		public uint TotalEvades { get; private set; }

		#region ICollection<Evade> implementation
		public void Add(Evade item)
		{
			evades.Add(item);

			++TotalEvades;
		}

		public void Clear()
		{
			evades.Clear();
		}

		public bool Contains(Evade item)
		{
			return evades.Contains(item);
		}

		public void CopyTo(Evade[] array, int arrayIndex)
		{
			evades.CopyTo(array, arrayIndex);
		}

		public int Count
		{
			get { return evades.Count; }
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		public bool Remove(Evade item)
		{
			return evades.Remove(item);
		}

		public IEnumerator<Evade> GetEnumerator()
		{
			return evades.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return evades.GetEnumerator();
		}
		#endregion
	}
}
