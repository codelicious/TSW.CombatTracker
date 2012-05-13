using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TSW.CombatParser
{
	public class Hit
	{
		public DateTime Timestamp { get; set; }
		public string Attacker { get; set; }
		public string Target { get; set; }
		public string AttackType { get; set; }
		public uint Damage { get; set; }
		public string Type { get; set; }
		public bool Critical { get; set; }
		public bool Glancing { get; set; }
		public bool Blocked { get; set; }
		public bool Penetrated { get; set; }

		public Hit(HitEventArgs e)
		{
			Timestamp = e.Timestamp;
			Attacker = e.Attacker;
			Target = e.Target;
			AttackType = e.AttackType;
			Damage = e.DamageType;
			Type = e.Type;
			Critical = e.Critical;
			Glancing = e.Glancing;
			Blocked = e.Blocked;
			Penetrated = e.Penetrated;
		}
	}

	public class HitCollection : ICollection<Hit>
	{
		List<Hit> hits = new List<Hit>();

		// Running totals
		public uint TotalDamage { get; private set; }
		public uint TotalHits { get { return (uint)hits.Count; } }
		public uint TotalCrits { get; private set; }
		public uint TotalGlancing { get; private set; }
		public uint TotalBlocked { get; private set; }
		public uint TotalPenetrated { get; private set; }

		public double DamagePerHit { get { return (double)TotalDamage / hits.Count; } }

		public double DPS { get { return GetRecentDamage(60.0) / 60.0; } }

		public double DPM { get { return GetRecentDamage(300.0) / 5.0; } }

		public double DPH { get { return GetRecentDamage(3600.0); } }

		private double GetRecentDamage(double seconds)
		{
			double recentDamage = 0.0;
			DateTime latestTime = DateTime.MinValue;
			TimeSpan damageInterval = TimeSpan.MinValue;

			int n = hits.Count;

			while (--n >= 0)
			{
				Hit hit = hits[n];
				if (latestTime == DateTime.MinValue)
					latestTime = hit.Timestamp;

				TimeSpan interval = latestTime - hit.Timestamp;
				if (interval.TotalSeconds >= seconds)
					break;

				recentDamage += hit.Damage;
				damageInterval = interval;
			}

			return recentDamage;
		}

		#region ICollection<Hit> implementation
		public void Add(Hit item)
		{
			hits.Add(item);

			// Update running totals
			TotalDamage += item.Damage;
			if (item.Critical) ++TotalCrits;
			if (item.Glancing) ++TotalGlancing;
			if (item.Blocked) ++TotalBlocked;
			if (item.Penetrated) ++TotalPenetrated;
		}

		public void Clear()
		{
			hits.Clear();

			TotalDamage = 0;
			TotalCrits = 0;
			TotalGlancing = 0;
			TotalBlocked = 0;
			TotalPenetrated = 0;
		}

		public bool Contains(Hit item)
		{
			return hits.Contains(item);
		}

		public void CopyTo(Hit[] array, int arrayIndex)
		{
			hits.CopyTo(array, arrayIndex);
		}

		public int Count
		{
			get { return hits.Count; }
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		public bool Remove(Hit item)
		{
			return hits.Remove(item);
		}

		public IEnumerator<Hit> GetEnumerator()
		{
			return hits.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return hits.GetEnumerator();
		}
		#endregion
	}
}
