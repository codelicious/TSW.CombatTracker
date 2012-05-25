using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TSW.CombatParser
{
	public class Heal
	{
		public DateTime Timestamp { get; private set; }
		public string Healer { get; private set; }
		public string Target { get; private set; }
		public string HealType { get; private set; }
		public uint Amount { get; private set; }
		public bool Critical { get; private set; }

		public Heal(HealEventArgs e)
		{
			Timestamp = e.Timestamp;
			Healer = e.Healer;
			Target = e.Target;
			HealType = e.HealType;
			Amount = e.Amount;
			Critical = e.Critical;
		}
	}

	public class HealCollection : ICollection<Heal>
	{
		private List<Heal> heals = new List<Heal>();

		public uint TotalHeals { get { return (uint)heals.Count; } }
		public uint TotalHealth { get; private set; }
		public uint TotalCrits { get; private set; }

		#region ICollection<Heal> implementation
		public void Add(Heal item)
		{
			heals.Add(item);

			TotalHealth += item.Amount;
			if (item.Critical)
				++TotalCrits;
		}

		public void Clear()
		{
			heals.Clear();

			TotalHealth = 0;
			TotalCrits = 0;
		}

		public bool Contains(Heal item)
		{
			return heals.Contains(item);
		}

		public void CopyTo(Heal[] array, int arrayIndex)
		{
			heals.CopyTo(array, arrayIndex);
		}

		public int Count
		{
			get { return heals.Count; }
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		public bool Remove(Heal item)
		{
			return heals.Remove(item);
		}

		public IEnumerator<Heal> GetEnumerator()
		{
			return heals.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return heals.GetEnumerator();
		}
		#endregion
	}
}
