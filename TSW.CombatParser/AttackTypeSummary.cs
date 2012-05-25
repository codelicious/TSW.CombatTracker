using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace TSW.CombatParser
{
	public class AttackTypeSummary : INotifyPropertyChanged
	{
		public AttackTypeSummary(AttackCollection owner, string name, string type)
		{
			this.owner = owner;
			Name = name;
			DamageType = type;
			Hits = new AttackCollection();
		}

		public string Name { get; private set; }
		public string DamageType { get; set; }
		public AttackCollection Hits { get; private set; }

		public uint TotalAttacks { get { return (uint)Hits.Count; } }

		public double TotalDamage { get; private set; }

		public uint TotalEvades { get; private set; }

		public double DamagePerHit { get { return Hits.DamagePerHit; } }

		public double DPS { get { return Hits.DPS; } }

		public double DPM { get { return Hits.DPM; } }

		public double DPH { get { return Hits.DPH; } }

		public double PercentOfTotalDamage { get { return (double)TotalDamage / owner.TotalDamage * 100.0; } }

		private AttackCollection owner;

		public void AddAttack(Attack e)
		{
			TotalDamage += e.Damage;
			Hits.Add(e);

			OnPropertyChanged("TotalAttacks");
			OnPropertyChanged("TotalDamage");
			OnPropertyChanged("TotalEvades");
			OnPropertyChanged("DamagePerHit");
			OnPropertyChanged("DPS");
			OnPropertyChanged("DPM");
			OnPropertyChanged("DPH");
			OnPropertyChanged("PercentOfTotalDamage");
		}

		public void AddEvade(Evade e)
		{
			++TotalEvades;

			OnPropertyChanged("TotalEvades");
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
