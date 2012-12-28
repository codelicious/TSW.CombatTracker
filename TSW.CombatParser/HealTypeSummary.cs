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
	public class HealTypeSummary
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
		public double HealthPerSecond { get { return Heals.HealthPerSecond; } }
		public double PercentOfTotalHealing { get { return (double)TotalHealth / owner.TotalHealth * 100.0; } }

		private HealCollection owner;

		public void AddHeal(Heal heal)
		{
			Heals.Add(heal);

			TotalHealth += heal.Amount;
			++TotalHeals;
		}

		public void Refresh()
		{
			Heals.Refresh();
		}
	}
}
