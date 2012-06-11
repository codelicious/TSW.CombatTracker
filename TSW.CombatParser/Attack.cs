using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace TSW.CombatParser
{
	public class Attack
	{
		public DateTime Timestamp { get; set; }
		public string Attacker { get; set; }
		public string Target { get; set; }
		public string AttackType { get; set; }
		public uint Damage { get; set; }
		public string DamageType { get; set; }
		public bool Critical { get; set; }
		public bool Glancing { get; set; }
		public bool Blocked { get; set; }
		public bool Penetrated { get; set; }
		public bool Evaded { get; set; }
		public bool Absorbed { get; set; }

		public Attack(HitEventArgs e)
		{
			Timestamp = e.Timestamp;
			Attacker = e.Attacker;
			Target = e.Target;
			AttackType = e.AttackType;
			Damage = e.Damage;
			DamageType = e.Type;
			Critical = e.Critical;
			Glancing = e.Glancing;
			Blocked = e.Blocked;
			Penetrated = e.Penetrated;
			Absorbed = false;
			Evaded = false;
		}

		public Attack(EvadeEventArgs e)
		{
			Timestamp = e.Timestamp;
			Attacker = e.Attacker;
			Target = e.Evader;
			AttackType = e.AttackType;
			Damage = 0;
			DamageType = String.Empty;
			Critical = false;
			Glancing = false;
			Blocked = false;
			Penetrated = false;
			Absorbed = false;
			Evaded = true;
		}

		public Attack(AbsorbEventArgs e)
		{
			Timestamp = e.Timestamp;
			Attacker = e.Attacker;
			Target = e.Target;
			AttackType = e.BarrierType;
			Damage = e.Damage;
			DamageType = String.Empty;
			Critical = false;
			Penetrated = false;
			Glancing = false;
			Blocked = false;
			Absorbed = true;
			Evaded = false;
		}
	}
}
