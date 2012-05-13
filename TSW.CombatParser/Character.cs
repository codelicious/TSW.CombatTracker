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

		public HitCollection Hits { get; private set; }
		public EvadeCollection Evades { get; private set; }

		public ObservableCollection<AttackType> AttackTypes { get; private set; }

		public uint TotalAttacks { get; private set; }
		public uint TotalDamage { get; private set; }
		public uint TotalCrits { get; private set; }
		public uint TotalPenetrated { get; private set; }
		public uint TotalGlanced { get; private set; }
		public uint TotalBlocked { get; private set; }
		public uint TotalEvaded { get; private set; }

		public double CritPercent { get { return (double)TotalCrits / TotalAttacks * 100.0; } }
		public double PenetratedPercent { get { return (double)TotalPenetrated / TotalAttacks * 100.0; } }
		public double GlancedPercent { get { return (double)TotalGlanced / TotalAttacks * 100.0; } }
		public double BlockedPercent { get { return (double)TotalBlocked / TotalAttacks * 100.0; } }
		public double EvadedPercent { get { return (double)TotalEvaded / TotalAttacks * 100.0; } }

		public uint TotalAttacksTaken { get; private set; }
		public uint TotalDamageTaken { get; private set; }
		public uint TotalCritsTaken { get; private set; }
		public uint TotalPenetratedTaken { get; private set; }
		public uint TotalGlancedTaken { get; private set; }
		public uint TotalBlockedTaken { get; private set; }
		public uint TotalEvadedTaken { get; private set; }

		public double DamagePerHitTaken { get { return (double)TotalDamageTaken / TotalAttacksTaken; } }
		public double CritsTakenPercent { get { return (double)TotalCritsTaken / TotalAttacksTaken * 100.0; } }
		public double PenetratedTakenPercent { get { return (double)TotalPenetratedTaken / TotalAttacksTaken * 100.0; } }
		public double GlancedTakenPercent { get { return (double)TotalGlancedTaken / TotalAttacksTaken * 100.0; } }
		public double BlockedTakenPercent { get { return (double)TotalBlockedTaken / TotalAttacksTaken * 100.0; } }
		public double EvadedTakenPercent { get { return (double)TotalEvadedTaken / TotalAttacksTaken * 100.0; } }

		public uint TotalHealsTaken { get; private set; }
		public uint TotalHealthTaken { get; private set; }
		public uint TotalCritHealsTaken { get; private set; }

		public double HealthPerHealTaken { get { return (double)TotalHealthTaken / TotalHealsTaken; } }
		public double CritHealsTakenPercent { get { return (double)TotalHealthTaken / TotalHealthTaken * 100.0; } }

		public uint TotalXP { get; private set; }

		public bool IsYou { get; set; }
		public bool IsMob { get; private set; }

		public Character()
		{
			Hits = new HitCollection();
			Evades = new EvadeCollection();
			AttackTypes = new ObservableCollection<AttackType>();
		}

		public double DamagePerHit { get { return Hits.DamagePerHit; } }

		public double DPS { get { return Hits.DPS; } }

		public double DPM { get { return Hits.DPM; } }

		public double DPH { get { return Hits.DPH; } }

		public void AddAttack(HitEventArgs e)
		{
			Hit hit = new Hit(e);
			Hits.Add(hit);

			AttackType attack = FindAttack(hit);
			attack.AddAttack(hit);

			++TotalAttacks;
			TotalDamage += hit.Damage;

			OnPropertyChanged("TotalAttacks");
			OnPropertyChanged("TotalDamage");
			OnPropertyChanged("DamagePerHit");
			OnPropertyChanged("DPS");
			OnPropertyChanged("DPM");
			OnPropertyChanged("DPH");

			if (hit.Critical)
			{
				++TotalCrits;
				OnPropertyChanged("TotalCrits");
			}
			if (hit.Penetrated)
			{
				++TotalPenetrated;
				OnPropertyChanged("TotalPenetrated");
			}
			if (hit.Glancing)
			{
				++TotalGlanced;
				OnPropertyChanged("TotalGlanced");
			}
			if (hit.Blocked)
			{
				++TotalBlocked;
				OnPropertyChanged("TotalBlocked");
			}

			if (!IsMob && attack.Name.Equals("attack"))
			{
				IsMob = true;
				OnPropertyChanged("IsMob");
			}

			OnPropertyChanged("CritPercent");
			OnPropertyChanged("PenetratedPercent");
			OnPropertyChanged("GlancedPercent");
			OnPropertyChanged("BlockedPercent");
			OnPropertyChanged("EvadedPercent");
		}

		public void AddEvade(EvadeEventArgs e)
		{
			Evade evade = new Evade(e);
			Evades.Add(evade);

			AttackType attack = FindAttack(evade);
			attack.AddEvade(evade);

			++TotalAttacks;
			++TotalEvaded;

			OnPropertyChanged("TotalAttacks");
			OnPropertyChanged("TotalEvaded");
			OnPropertyChanged("EvadedPercent");
		}

		public void AddHeal(HealEventArgs e)
		{
		}

		public void AddHit(HitEventArgs e)
		{
			Hit hit = new Hit(e);

			++TotalAttacksTaken;
			TotalDamageTaken += e.DamageType;

			OnPropertyChanged("TotalAttacksTaken");
			OnPropertyChanged("TotalDamageTaken");

			if (hit.Critical)
			{
				++TotalCritsTaken;
				OnPropertyChanged("TotalCritsTaken");
			}
			if (hit.Penetrated)
			{
				++TotalPenetratedTaken;
				OnPropertyChanged("TotalPenetratedTaken");
			}
			if (hit.Glancing)
			{
				++TotalGlancedTaken;
				OnPropertyChanged("TotalGlancedTaken");
			}
			if (hit.Blocked)
			{
				++TotalBlockedTaken;
				OnPropertyChanged("TotalBlockedTaken");
			}
			
			OnPropertyChanged("CritsTakenPercent");
			OnPropertyChanged("PenetratedTakenPercent");
			OnPropertyChanged("GlancedTakenPercent");
			OnPropertyChanged("BlockedTakenPercent");
			OnPropertyChanged("EvadedTakenPercent");
		}

		public void AddHealTaken(HealEventArgs e)
		{
			++TotalHealsTaken;
			TotalHealthTaken += e.Amount;

			OnPropertyChanged("TotalHealsTaken");
			OnPropertyChanged("TotalHealthTaken");

			if (e.Critical)
			{
				++TotalCritHealsTaken;
				OnPropertyChanged("TotalCritHealsTaken");
			}

			OnPropertyChanged("HealthPerHealTaken");
			OnPropertyChanged("CritHealsTakenPercent");
		}

		public void AddXp(XpEventArgs e)
		{
			TotalXP += e.XP;
			OnPropertyChanged("XP");
		}

		public AttackType FindAttack(Hit hit)
		{
			AttackType attack = AttackTypes.FirstOrDefault(a => a.Name.Equals(hit.AttackType));
			if (attack == null)
			{
				attack = new AttackType(hit.AttackType, hit.Type);
				AttackTypes.Add(attack);
			}
			else if (attack.DamageType == null) // This can happen if the first instance of this attack was evaded
				attack.DamageType = hit.Type;
			
			return attack;
		}

		public AttackType FindAttack(Evade evade)
		{
			AttackType attack = AttackTypes.FirstOrDefault(a => a.Name.Equals(evade.AttackType));
			if (attack == null)
			{
				attack = new AttackType(evade.AttackType, null);
				AttackTypes.Add(attack);
			}

			return attack;
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
