using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace TSW.CombatParser
{
	public class AttackType: INotifyPropertyChanged
	{
		public string Name { get; private set; }
		public string DamageType { get; set; }
		public HitCollection Hits { get; private set; }

		public AttackType(string name, string type)
		{
			Name = name;
			DamageType = type;
			Hits = new HitCollection();
		}

		public uint TotalAttacks { get { return (uint)Hits.Count; } }

		public double TotalDamage { get; private set; }

		public uint TotalEvades { get; private set; }

		public double DamagePerHit { get { return Hits.DamagePerHit; } }

		public double DPS { get { return Hits.DPS; } }

		public double DPM { get { return Hits.DPM; } }

		public double DPH { get { return Hits.DPH; } }

		public void AddAttack(Hit e)
		{
			TotalDamage += e.Damage;
			Hits.Add(e);

			OnPropertyChanged("TotalAttacks");
			OnPropertyChanged("TotalDamage");
			OnPropertyChanged("DamagePerHit");
			OnPropertyChanged("DPS");
			OnPropertyChanged("DPM");
			OnPropertyChanged("DPH");
		}

		public void AddEvade(Evade e)
		{
			++TotalEvades;

			OnPropertyChanged("TotalEvades");
		}

		public event PropertyChangedEventHandler PropertyChanged;

		private void OnPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
