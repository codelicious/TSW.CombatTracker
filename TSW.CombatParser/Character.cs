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

namespace TSW.CombatParser
{
	[System.Diagnostics.DebuggerDisplay("{Name}")]
	public class Character : INotifyPropertyChanged
	{
		private string name = String.Empty;

		public string Name
		{
			get { return name; }
			set
			{
				name = value;
				if (name.Contains(' ') || name.Contains('\''))
					IsMob = true;
			}
		}

		public AttackCollection OffensiveHits { get; private set; }
		public HealCollection OffensiveHeals { get; private set; }
		public RefreshList<AttackTypeSummary> OffensiveAttackSummaries { get; private set; }
		public RefreshList<HealTypeSummary> OffensiveHealSummaries { get; private set; }


		public AttackCollection DefensiveHits { get; private set; }
		public HealCollection DefensiveHeals { get; private set; }
		public RefreshList<AttackTypeSummary> DefensiveAttackSummaries { get; private set; }
		public RefreshList<HealTypeSummary> DefensiveHealSummaries { get; private set; }

		public uint TotalXP { get; private set; }

		// These are bogus proxy properties because SortDescription is too stupid to use property paths
		public uint Offensive_TotalDamage { get; set; }
		public double Offensive_DPS { get; private set; }
		public double Offensive_DPM { get; private set; }

		public bool IsYou { get; set; }
		public bool IsMob { get; private set; }

		public Character()
		{
			OffensiveHits = new AttackCollection();
			OffensiveHeals = new HealCollection();
			OffensiveAttackSummaries = new RefreshList<AttackTypeSummary>();
			OffensiveHealSummaries = new RefreshList<HealTypeSummary>();

			DefensiveHits = new AttackCollection();
			DefensiveHeals = new HealCollection();
			DefensiveAttackSummaries = new RefreshList<AttackTypeSummary>();
			DefensiveHealSummaries = new RefreshList<HealTypeSummary>();
		}

		public void AddOffensiveHit(Attack attack)
		{
			OffensiveHits.Add(attack);

			AttackTypeSummary attackSummary = OffensiveAttackSummaries.FindAttackSummary(attack, OffensiveHits);
			attackSummary.AddAttack(attack);

			// One of the extra ways we can detect mobs
			if (!IsMob && attackSummary.Name.Equals("attack"))
				IsMob = true;
		}

		public void AddOffensiveEvade(Attack evade)
		{
			OffensiveHits.Add(evade);

			AttackTypeSummary attackSummary = OffensiveAttackSummaries.FindAttackSummary(evade, OffensiveHits);
			attackSummary.AddAttack(evade);
		}

		public void AddOffensiveHeal(Heal heal)
		{
			OffensiveHeals.Add(heal);

			HealTypeSummary healSummary = OffensiveHealSummaries.FindHealSummary(heal, OffensiveHeals);
			healSummary.AddHeal(heal);
		}

		public void AddDefensiveHit(Attack attack)
		{
			DefensiveHits.Add(attack);

			AttackTypeSummary attackSummary = DefensiveAttackSummaries.FindAttackSummary(attack, DefensiveHits);
			attackSummary.AddAttack(attack);
		}

		public void AddDefensiveEvade(Attack evade)
		{
			DefensiveHits.Add(evade);

			AttackTypeSummary attackSummary = DefensiveAttackSummaries.FindAttackSummary(evade, DefensiveHits);
			attackSummary.AddAttack(evade);
		}

		public void AddDefensiveHeal(Heal heal)
		{
			DefensiveHeals.Add(heal);

			HealTypeSummary healSummary = DefensiveHealSummaries.FindHealSummary(heal, DefensiveHeals);
			healSummary.AddHeal(heal);
		}

		public void AddXp(XpEventArgs e)
		{
			TotalXP += e.XP;
		}

		public void Reset()
		{
			OffensiveHits.Clear();
			OffensiveAttackSummaries.Clear();
			OffensiveHeals.Clear();
			OffensiveHealSummaries.Clear();

			DefensiveHits.Clear();
			DefensiveAttackSummaries.Clear();
			DefensiveHeals.Clear();
			DefensiveHealSummaries.Clear();
		}

		public void Refresh()
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(null));

			OffensiveHits.Refresh();
			OffensiveHeals.Refresh();
			OffensiveAttackSummaries.Refresh();
			foreach (AttackTypeSummary summary in OffensiveAttackSummaries) summary.Refresh();
			OffensiveHealSummaries.Refresh();
			foreach (HealTypeSummary summary in OffensiveHealSummaries) summary.Refresh();
			Offensive_TotalDamage = OffensiveHits.TotalDamage;
			Offensive_DPS = OffensiveHits.DPS;
			Offensive_DPM = OffensiveHits.DPM;
			
			DefensiveHits.Refresh();
			DefensiveHeals.Refresh();
			DefensiveAttackSummaries.Refresh();
			foreach (AttackTypeSummary summary in DefensiveAttackSummaries) summary.Refresh();
			DefensiveHealSummaries.Refresh();
			foreach (HealTypeSummary summary in DefensiveHealSummaries) summary.Refresh();
		}

		public event PropertyChangedEventHandler PropertyChanged;
	}

	internal static class AttackSummaryExtensions
	{
		public static AttackTypeSummary FindAttackSummary(this RefreshList<AttackTypeSummary> summaries, Attack hit, AttackCollection owner)
		{
			AttackTypeSummary summary = summaries.FirstOrDefault(s => s.Name.Equals(hit.AttackType));
			if (summary == null)
			{
				summary = new AttackTypeSummary(owner, hit.AttackType, hit.DamageType);
				summaries.Add(summary);
			}
			else if (summary.DamageType == null) // This can happen if the first instance of this attack was evaded and no DamageType was available
				summary.DamageType = hit.DamageType;

			return summary;
		}

		public static HealTypeSummary FindHealSummary(this RefreshList<HealTypeSummary> summaries, Heal heal, HealCollection owner)
		{
			HealTypeSummary summary = summaries.FirstOrDefault(s => s.Name.Equals(heal.HealType));
			if (summary == null)
			{
				summary = new HealTypeSummary(owner, heal.HealType);
				summaries.Add(summary);
			}

			return summary;
		}
	}
}
