using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;

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
				if (name.Contains(' '))
					IsMob = true;
			}
		}

		public AttackCollection OffensiveHits { get; private set; }
		public HealCollection OffensiveHeals { get; private set; }
		public ObservableCollection<AttackTypeSummary> OffensiveAttackSummaries { get; private set; }


		public AttackCollection DefensiveHits { get; private set; }
		public HealCollection DefensiveHeals { get; private set; }
		public ObservableCollection<AttackTypeSummary> DefensiveAttackSummaries { get; private set; }

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
			OffensiveAttackSummaries = new ObservableCollection<AttackTypeSummary>();

			DefensiveHits = new AttackCollection();
			DefensiveHeals = new HealCollection();
			DefensiveAttackSummaries = new ObservableCollection<AttackTypeSummary>();

			OffensiveHits.PropertyChanged += OffensiveHits_PropertyChanged;
		}

		public void AddOffensiveHit(HitEventArgs e)
		{
			Attack hit = new Attack(e);
			OffensiveHits.Add(hit);

			AttackTypeSummary attackSummary = OffensiveAttackSummaries.FindAttackSummary(hit);
			attackSummary.AddAttack(hit);

			// One of the extra ways we can detect mobs
			if (!IsMob && attackSummary.Name.Equals("attack"))
			{
				IsMob = true;
				OnPropertyChanged("IsMob");
			}
		}

		public void AddOffensiveEvade(EvadeEventArgs e)
		{
			Attack evade = new Attack(e);
			OffensiveHits.Add(evade);

			AttackTypeSummary attackSummary = OffensiveAttackSummaries.FindAttackSummary(evade);
			attackSummary.AddAttack(evade);
		}

		public void AddOffensiveHeal(HealEventArgs e)
		{
		}

		public void AddDefensiveHit(HitEventArgs e)
		{
			Attack hit = new Attack(e);
			DefensiveHits.Add(hit);

			AttackTypeSummary attackSummary = DefensiveAttackSummaries.FindAttackSummary(hit);
		}

		public void AddDefensiveEvade(EvadeEventArgs e)
		{
			Attack evade = new Attack(e);
			DefensiveHits.Add(evade);

			AttackTypeSummary attackSummary = DefensiveAttackSummaries.FindAttackSummary(evade);
			attackSummary.AddAttack(evade);
		}

		public void AddDefensiveHeal(HealEventArgs e)
		{
		}

		public void AddXp(XpEventArgs e)
		{
			TotalXP += e.XP;
			OnPropertyChanged("XP");
		}

		private void OffensiveHits_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName.Equals("TotalDamage"))
			{
				Offensive_TotalDamage = OffensiveHits.TotalDamage;
				OnPropertyChanged("Offensive_TotalDamage");
			}
			else if (e.PropertyName.Equals("DPM"))
			{
				Offensive_DPM = OffensiveHits.DPM;
				OnPropertyChanged("Offensive_DPM");
			}
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

	internal static class AttackSummryExtensions
	{
		public static AttackTypeSummary FindAttackSummary(this ObservableCollection<AttackTypeSummary> summaries, Attack hit)
		{
			AttackTypeSummary summary = summaries.FirstOrDefault(s => s.Name.Equals(hit.AttackType));
			if (summary == null)
			{
				summary = new AttackTypeSummary(hit.AttackType, hit.DamageType);
				summaries.Add(summary);
			}
			else if (summary.DamageType == null) // This can happen if the first instance of this attack was evaded and no DamageType was available
				summary.DamageType = hit.DamageType;

			return summary;
		}
	}
}
