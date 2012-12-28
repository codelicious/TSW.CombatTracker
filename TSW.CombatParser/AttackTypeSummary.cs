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
	[System.Diagnostics.DebuggerDisplay("{Name} [{DamageType}]")]
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
