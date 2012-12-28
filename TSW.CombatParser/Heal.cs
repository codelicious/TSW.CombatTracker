/*
    Copyright 2012 Douglas Harber

    This file is part of TSW.CombatTracker.

    TSW.CombatTracker is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    TSW.CombatTracker is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with TSW.CombatTracker.  If not, see <http://www.gnu.org/licenses/>.
*/
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

	public class HealCollection : ICollection<Heal>, INotifyPropertyChanged
	{
		private List<Heal> heals = new List<Heal>();

		public uint TotalHeals { get { return (uint)heals.Count; } }
		public uint TotalHealth { get; private set; }
		public uint TotalCrits { get; private set; }
		public uint TotalCritHealth { get; private set; }

		public double HealthPerHeal { get { return (double)TotalHealth / TotalHeals; } }
		public double HealthPerSecond
		{
			get
			{
				TimeSpan interval;
				double health = GetRecentHealing(300.0, out interval);
				return health / interval.TotalSeconds;
			}
		}

		public double CritPercent { get { return (double)TotalCrits / TotalHeals * 100.0; } }
		public double CritHealPercent { get { return (double)TotalCritHealth / TotalHealth * 100.0; } }

		private double GetRecentHealing(double seconds, out TimeSpan actualInterval)
		{
			double recentHealing = 0.0;
			DateTime latestTime = DateTime.MinValue;
			TimeSpan healingInterval = TimeSpan.MinValue;

			int n = heals.Count;

			while (--n >= 0)
			{
				Heal heal = heals[n];
				if (latestTime == DateTime.MinValue)
					latestTime = heal.Timestamp;

				TimeSpan interval = latestTime - heal.Timestamp;
				if (interval.TotalSeconds >= seconds)
					break;

				recentHealing += heal.Amount;
				healingInterval = interval;
			}

			if (healingInterval.TotalSeconds >= 1.0)
				actualInterval = healingInterval;
			else
				actualInterval = TimeSpan.FromSeconds(1.0);

			return recentHealing;
		}

		#region ICollection<Heal> implementation
		public void Add(Heal heal)
		{
			heals.Add(heal);

			TotalHealth += heal.Amount;
			if (heal.Critical)
			{
				++TotalCrits;
				TotalCritHealth += heal.Amount;
			}
		}

		public void Clear()
		{
			heals.Clear();

			TotalHealth = 0;
			TotalCrits = 0;
			TotalCritHealth = 0;
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

		public void Refresh()
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(null));
		}

		public event PropertyChangedEventHandler PropertyChanged;
	}
}
