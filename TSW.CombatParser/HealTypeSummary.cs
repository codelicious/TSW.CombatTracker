using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace TSW.CombatParser
{
	public class HealTypeSummary : INotifyPropertyChanged
	{
		public HealTypeSummary(HealCollection owner, string name)
		{
			this.owner = owner;
			Name = name;
			Heals = new HealCollection();
		}

		public string Name { get; private set; }
		public HealCollection Heals { get; private set; }

		public uint TotalHealth { get; private set; }
		public uint TotalHeals { get; private set; }
		public double HealthPerHeal { get { return (double)TotalHealth / TotalHeals; } }
		public double PercentOfTotalHealing { get { return (double)TotalHealth / owner.TotalHealth * 100.0; } }

		private HealCollection owner;

		public double HealthPerMinute
		{
			get { return 0.0; }
		}

		public void AddHeal(Heal heal)
		{
			Heals.Add(heal);

			TotalHealth += heal.Amount;
			++TotalHeals;

			OnPropertyChanged("TotalHealth");
			OnPropertyChanged("TotalHeals");
			OnPropertyChanged("HealthPerHeal");
			OnPropertyChanged("HealthPerMinute");
			OnPropertyChanged("PercentOfTotalHealing");
		}

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
