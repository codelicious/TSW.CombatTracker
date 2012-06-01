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

		public double PercentOfTotalDamage { get { return (double)Hits.TotalDamage / owner.TotalDamage * 100.0; } }

		private AttackCollection owner;

		public void AddAttack(Attack e)
		{
			Hits.Add(e);
		}

		public void Refresh()
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(null));
		}

		public event PropertyChangedEventHandler PropertyChanged;
	}
}
