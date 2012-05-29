﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace TSW.CombatParser
{
	public class Attack
	{
		public DateTime Timestamp { get; set; }
		public string Attacker { get; set; }
		public string Target { get; set; }
		public string AttackType { get; set; }
		public uint Damage { get; set; }
		public string DamageType { get; set; }
		public bool Critical { get; set; }
		public bool Glancing { get; set; }
		public bool Blocked { get; set; }
		public bool Penetrated { get; set; }
		public bool Evaded { get; set; }

		public Attack(HitEventArgs e)
		{
			Timestamp = e.Timestamp;
			Attacker = e.Attacker;
			Target = e.Target;
			AttackType = e.AttackType;
			Damage = e.DamageType;
			DamageType = e.Type;
			Critical = e.Critical;
			Glancing = e.Glancing;
			Blocked = e.Blocked;
			Penetrated = e.Penetrated;
			Evaded = false;
		}

		public Attack(EvadeEventArgs e)
		{
			Timestamp = e.Timestamp;
			Attacker = e.Attacker;
			Target = e.Evader;
			AttackType = e.AttackType;
			Damage = 0;
			DamageType = String.Empty;
			Critical = false;
			Glancing = false;
			Blocked = false;
			Penetrated = false;
			Evaded = true;
		}
	}

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

		public double DPS {
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
				return damage / (interval.TotalMinutes / 5);
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

			if (damageInterval.TotalSeconds >= seconds)
				actualInterval = damageInterval;
			else
				actualInterval = TimeSpan.FromSeconds(seconds);

			return recentDamage;
		}

		#region ICollection<Hit> implementation
		public void Add(Attack item)
		{
			attacks.Add(item);

			// Update running totals
			++TotalAttacks;
			TotalDamage += item.Damage;

			OnPropertyChanged("TotalAttacks");
			OnPropertyChanged("TotalDamage");

			if (item.Critical)
			{
				++TotalCrits;
				TotalCritDamage += item.Damage;
				OnPropertyChanged("TotalCrits");
				OnPropertyChanged("TotalCritDamage");
			}
			if (item.Glancing)
			{
				++TotalGlanced;
				TotalGlancedDamage += item.Damage;
				OnPropertyChanged("TotalGlanced");
				OnPropertyChanged("TotalGlancedDamage");
			}
			if (item.Blocked)
			{
				++TotalBlocked;
				TotalBlockedDamage += item.Damage;
				OnPropertyChanged("TotalBlocked");
				OnPropertyChanged("TotalBlockedDamage");
			}
			if (item.Penetrated)
			{
				++TotalPenetrated;
				TotalPenetratedDamage += item.Damage;
				OnPropertyChanged("TotalPenetrated");
				OnPropertyChanged("TotalPenetratedDamage");
			}
			if (item.Evaded)
			{
				++TotalEvaded;
				OnPropertyChanged("TotalEvaded");
			}

			OnPropertyChanged("DamagePerHit");
			OnPropertyChanged("CritPercent");
			OnPropertyChanged("GlancedPercent");
			OnPropertyChanged("BlockedPercent");
			OnPropertyChanged("PenetratedPercent");
			OnPropertyChanged("EvadedPercent");
			OnPropertyChanged("DPS");
			OnPropertyChanged("DPM");
			OnPropertyChanged("DPH");
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

			OnPropertyChanged("TotalAttacks");
			OnPropertyChanged("TotalDamage");
			OnPropertyChanged("TotalCrits");
			OnPropertyChanged("TotalGlanced");
			OnPropertyChanged("TotalBlocked");
			OnPropertyChanged("TotalPenetrated");
			OnPropertyChanged("TotalEvaded");

			OnPropertyChanged("DamagePerHit");
			OnPropertyChanged("CritPercent");
			OnPropertyChanged("GlancedPercent");
			OnPropertyChanged("BlockedPercent");
			OnPropertyChanged("PenetratedPercent");
			OnPropertyChanged("EvadedPercent");
			OnPropertyChanged("DPS");
			OnPropertyChanged("DPM");
			OnPropertyChanged("DPH");
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
			return attacks.GetEnumerator();
		}
		#endregion

		#region INotifyPropertyChanged implementation
		public event PropertyChangedEventHandler PropertyChanged;

		private void OnPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
		#endregion
	}
}
