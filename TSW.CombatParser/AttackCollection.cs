using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace TSW.CombatParser
{
	public class AttackCollection : ICollection<Attack>, INotifyPropertyChanged
	{
		List<Attack> attacks = new List<Attack>();

		// Running totals
		public uint TotalAttacks { get; private set; }
		public uint TotalDamage { get; private set; }
		public uint TotalCrits { get; private set; }
		public uint TotalCritDamage { get; private set; }
		public uint TotalGlanced { get; private set; }
		public uint TotalGlancedDamage { get; private set; }
		public uint TotalBlocked { get; private set; }
		public uint TotalBlockedDamage { get; private set; }
		public uint TotalPenetrated { get; private set; }
		public uint TotalPenetratedDamage { get; private set; }
		public uint TotalEvaded { get; private set; }

		public double DamagePerHit { get { return (double)TotalDamage / TotalAttacks; } }
		public double CritPercent { get { return (double)TotalCrits / TotalAttacks * 100.0; } }
		public double CritDamagePercent { get { return (double)TotalCritDamage / TotalDamage * 100.0; } }
		public double PenetratedPercent { get { return (double)TotalPenetrated / TotalAttacks * 100.0; } }
		public double PenetratedDamagePercent { get { return (double)TotalPenetratedDamage / TotalDamage * 100.0; } }
		public double GlancedPercent { get { return (double)TotalGlanced / TotalAttacks * 100.0; } }
		public double GlancedDamagePercent { get { return (double)TotalGlancedDamage / TotalDamage * 100.0; } }
		public double BlockedPercent { get { return (double)TotalBlocked / TotalAttacks * 100.0; } }
		public double BlockedDamagePercent { get { return (double)TotalBlockedDamage / TotalDamage * 100.0; } }
		public double EvadedPercent { get { return (double)TotalEvaded / TotalAttacks * 100.0; } }

		public double DPS
		{
			get
			{
				TimeSpan interval;
				double damage = GetRecentDamage(60.0, out interval);
				return damage / interval.TotalSeconds;
			}
		}

		public double DPM
		{
			get
			{
				TimeSpan interval;
				double damage = GetRecentDamage(300.0, out interval);
				return damage / 5.0;
			}
		}

		public double DPH
		{
			get
			{
				TimeSpan interval;
				double damage = GetRecentDamage(3600.0, out interval);
				return damage / interval.TotalHours;
			}
		}


		private double GetRecentDamage(double seconds, out TimeSpan actualInterval)
		{
			double recentDamage = 0.0;
			DateTime latestTime = DateTime.MinValue;
			TimeSpan damageInterval = TimeSpan.MinValue;

			int n = attacks.Count;

			while (--n >= 0)
			{
				Attack hit = attacks[n];
				if (latestTime == DateTime.MinValue)
					latestTime = hit.Timestamp;

				TimeSpan interval = latestTime - hit.Timestamp;
				if (interval.TotalSeconds >= seconds)
					break;

				recentDamage += hit.Damage;
				damageInterval = interval;
			}

			if (damageInterval.TotalSeconds >= 1.0)
				actualInterval = damageInterval;
			else
				actualInterval = TimeSpan.FromSeconds(1.0);

			return recentDamage;
		}

		#region ICollection<Hit> implementation
		public void Add(Attack item)
		{
			attacks.Add(item);

			// Update running totals
			++TotalAttacks;
			TotalDamage += item.Damage;

			if (item.Critical)
			{
				++TotalCrits;
				TotalCritDamage += item.Damage;
			}
			if (item.Glancing)
			{
				++TotalGlanced;
				TotalGlancedDamage += item.Damage;
			}
			if (item.Blocked)
			{
				++TotalBlocked;
				TotalBlockedDamage += item.Damage;
			}
			if (item.Penetrated)
			{
				++TotalPenetrated;
				TotalPenetratedDamage += item.Damage;
			}
			if (item.Evaded)
			{
				++TotalEvaded;
			}
		}

		public void Clear()
		{
			attacks.Clear();

			TotalAttacks = 0;
			TotalDamage = 0;
			TotalCrits = 0;
			TotalGlanced = 0;
			TotalBlocked = 0;
			TotalPenetrated = 0;
			TotalGlanced = 0;
		}

		public bool Contains(Attack item)
		{
			return attacks.Contains(item);
		}

		public void CopyTo(Attack[] array, int arrayIndex)
		{
			attacks.CopyTo(array, arrayIndex);
		}

		public int Count
		{
			get { return attacks.Count; }
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		public bool Remove(Attack item)
		{
			return attacks.Remove(item);
		}

		public IEnumerator<Attack> GetEnumerator()
		{
			return attacks.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return ((System.Collections.IEnumerable)attacks).GetEnumerator();
		}
		#endregion

		public void Refresh()
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(null));
		}

		public event PropertyChangedEventHandler PropertyChanged;
	}
}
