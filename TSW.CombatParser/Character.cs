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
		public List<AttackTypeSummary> OffensiveAttackSummaries { get; private set; }
		public List<HealTypeSummary> OffensiveHealSummaries { get; private set; }


		public AttackCollection DefensiveHits { get; private set; }
		public HealCollection DefensiveHeals { get; private set; }
		public List<AttackTypeSummary> DefensiveAttackSummaries { get; private set; }
		public List<HealTypeSummary> DefensiveHealSummaries { get; private set; }

		public uint TotalXP { get; private set; }

		// These are bogus proxy properties because SortDescription is too stupid to use property paths
		public uint Offensive_TotalDamage { get; private set; }
		public double Offensive_DPM { get; private set; }

		public bool IsYou { get; set; }
		public bool IsMob { get; private set; }

		public Character()
		{
			OffensiveHits = new AttackCollection();
			OffensiveHeals = new HealCollection();
			OffensiveAttackSummaries = new List<AttackTypeSummary>();
			OffensiveHealSummaries = new List<HealTypeSummary>();

			DefensiveHits = new AttackCollection();
			DefensiveHeals = new HealCollection();
			DefensiveAttackSummaries = new List<AttackTypeSummary>();
			DefensiveHealSummaries = new List<HealTypeSummary>();
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

		public void Refresh()
		{
			OffensiveHits.Refresh();
			OffensiveHeals.Refresh();
			OffensiveAttackSummaries.ForEach(s => s.Refresh());
			
			DefensiveHits.Refresh();
			DefensiveHeals.Refresh();
			DefensiveAttackSummaries.ForEach(s => s.Refresh());

			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(null));
		}

		public event PropertyChangedEventHandler PropertyChanged;
	}

	internal static class AttackSummaryExtensions
	{
		public static AttackTypeSummary FindAttackSummary(this List<AttackTypeSummary> summaries, Attack hit, AttackCollection owner)
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

		public static HealTypeSummary FindHealSummary(this List<HealTypeSummary> summaries, Heal heal, HealCollection owner)
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
