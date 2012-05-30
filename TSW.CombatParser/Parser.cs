using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TSW.CombatParser
{
	public class CombatParser
	{
		static Regex yourHitEx = new Regex(@"\[(\d\d:\d\d:\d\d)\] (?:\((Normal|Critical|Blocked|Penetrated)\) ){0,1}Your (.+) hits \((Normal|Glancing)\) (.+) for (\d+) (physical|magical) damage. \((Normal|Critical|Blocked|Penetrated)\)", RegexOptions.Compiled);
		static Regex otherHitYouEx = new Regex(@"\[(\d\d:\d\d:\d\d)\] (?:\((Normal|Critical|Blocked|Penetrated)\) ){0,1}([^\']+)'s (.+) hits \((Normal|Glancing)\) you for (\d+) (physical|magical) damage. \((Normal|Critical|Blocked|Penetrated)\)", RegexOptions.Compiled);
		static Regex otherHitOtherEx = new Regex(@"\[(\d\d:\d\d:\d\d)\] (?:\((Normal|Critical|Blocked|Penetrated)\) ){0,1}([^\']+)'s (.+) hits \((Normal|Glancing)\) (.+) for (\d+) (physical|magical) damage. \((Normal|Critical|Blocked|Penetrated)\)", RegexOptions.Compiled);
		static Regex interrupedEx = new Regex(@"\[(\d\d:\d\d:\d\d)\] Interrupted!", RegexOptions.Compiled);

		static Regex otherEvadedYouEx = new Regex(@"\[(\d\d:\d\d:\d\d)\] ([\w\s]+) evaded your ([\w\s]+)\.", RegexOptions.Compiled);
		static Regex youEvadeOtherEx = new Regex(@"\[(\d\d:\d\d:\d\d)\] (You) evade ([\w\s]+)\'s ([\w\s]+)\.", RegexOptions.Compiled);
		static Regex otherEvadedOtherEx = new Regex(@"\[(\d\d:\d\d:\d\d)\] ([\w\s]+) evaded ([\w\s]+)\'s ([\w\s]+)\.", RegexOptions.Compiled);

		static Regex youHealedEx = new Regex(@"\[(\d\d:\d\d:\d\d)\] (?:\((Critical)\) ){0,1}Your ([\w\s]+) heals ([\w\s]+) for (\d+)\.", RegexOptions.Compiled);
		static Regex otherHealedEx = new Regex(@"\[(\d\d:\d\d:\d\d)\] (?:\((Critical)\) ){0,1}([\w\s]+)\'s ([\w\s]+) heals ([\w\s]+) for (\d+)\.", RegexOptions.Compiled);

		static Regex youGainedXpEx = new Regex(@"\[(\d\d:\d\d:\d\d)\] You gained (\d+) XP\.", RegexOptions.Compiled);


		public event EventHandler<HitEventArgs> Hit;
		public event EventHandler<EvadeEventArgs> Evade;
		public event EventHandler<HealEventArgs> Heal;
		public event EventHandler<XpEventArgs> XP;

		public void Parse(string line)
		{
			Match m = yourHitEx.Match(line);
			if (m.Success)
			{
				OnYouHitOther(m);
				return;
			}

			m = otherHitYouEx.Match(line);
			if (m.Success)
			{
				OnOtherHitYou(m);
				return;
			}

			m = otherHitOtherEx.Match(line);
			if (m.Success)
			{
				OnOtherHitOther(m);
				return;
			}

			m = otherEvadedYouEx.Match(line);
			if (m.Success)
			{
				OnOtherEvadedYou(m);
				return;
			}

			m = youEvadeOtherEx.Match(line);
			if (m.Success)
			{
				OnYouEvadedOther(m);
				return;
			}

			m = otherEvadedOtherEx.Match(line);
			if (m.Success)
			{
				OnOtherEvadedOther(m);
				return;
			}

			m = youHealedEx.Match(line);
			if (m.Success)
			{
				OnYouHealed(m);
				return;
			}

			m = otherHealedEx.Match(line);
			if (m.Success)
			{
				OnOtherHealed(m);
				return;
			}

			m = youGainedXpEx.Match(line);
			if (m.Success)
			{
				OnXpGain(m);
				return;
			}

			m = interrupedEx.Match(line);
			if (m.Success)
			{
				//OnInterrupted(m);
				return;
			}
		}

		private void OnYouHitOther(Match m)
		{
			if (Hit != null)
			{
				HitEventArgs hit = new HitEventArgs();
				DateTime timestamp;
				if (DateTime.TryParse(m.Groups[1].Value, out timestamp))
					hit.Timestamp = timestamp;
				else
					hit.Timestamp = DateTime.MinValue;

				hit.Attacker = "You";
				hit.Target = m.Groups[5].Value;
				hit.AttackType = m.Groups[3].Value;
				hit.Type = m.Groups[7].Value;
				
				uint damage;
				if (uint.TryParse(m.Groups[6].Value, out damage))
					hit.Damage = damage;
				else
					hit.Damage = 0;
				
				string critical = m.Groups[2].Value;
				if (!String.IsNullOrEmpty(critical) && critical.Equals("Critical"))
					hit.Critical = true;
				else
					hit.Critical = false;

				string glancing = m.Groups[4].Value;
				if (!String.IsNullOrEmpty(glancing) && glancing.Equals("Glancing"))
					hit.Glancing = true;
				else
					hit.Glancing = false;

				string blocked = m.Groups[8].Value;
				if (!String.IsNullOrEmpty(blocked))
				{
					hit.Blocked = blocked.Equals("Blocked");
					hit.Penetrated = blocked.Equals("Penetrated");
				}

				Hit(null, hit);
			}
		}

		private void OnOtherHitYou(Match m)
		{
			if (Hit != null)
			{
				HitEventArgs hit = new HitEventArgs();
				DateTime timestamp;
				if (DateTime.TryParse(m.Groups[1].Value, out timestamp))
					hit.Timestamp = timestamp;
				else
					hit.Timestamp = DateTime.MinValue;

				hit.Attacker = m.Groups[3].Value;
				hit.Target = "You";
				hit.AttackType = m.Groups[4].Value;
				hit.Type = m.Groups[7].Value;

				uint damage;
				if (uint.TryParse(m.Groups[6].Value, out damage))
					hit.Damage = damage;
				else
					hit.Damage = 0;

				string critical = m.Groups[2].Value;
				if (!String.IsNullOrEmpty(critical) && critical.Equals("Critical"))
					hit.Critical = true;
				else
					hit.Critical = false;

				string glancing = m.Groups[5].Value;
				if (!String.IsNullOrEmpty(glancing) && glancing.Equals("Glancing"))
					hit.Glancing = true;
				else
					hit.Glancing = false;

				string blocked = m.Groups[8].Value;
				if (!String.IsNullOrEmpty(blocked))
				{
					hit.Blocked = blocked.Equals("Blocked");
					hit.Penetrated = blocked.Equals("Penetrated");
				}

				Hit(null, hit);
			}
		}

		private void OnOtherHitOther(Match m)
		{
			if (Hit != null)
			{
				HitEventArgs hit = new HitEventArgs();
				DateTime timestamp;
				if (DateTime.TryParse(m.Groups[1].Value, out timestamp))
					hit.Timestamp = timestamp;
				else
					hit.Timestamp = DateTime.MinValue;

				hit.Attacker = m.Groups[3].Value;
				hit.Target = m.Groups[6].Value;
				hit.AttackType = m.Groups[4].Value;
				hit.Type = m.Groups[8].Value;

				uint damage;
				if (uint.TryParse(m.Groups[7].Value, out damage))
					hit.Damage = damage;
				else
					hit.Damage = 0;

				string critical = m.Groups[2].Value;
				if (!String.IsNullOrEmpty(critical) && critical.Equals("Critical"))
					hit.Critical = true;
				else
					hit.Critical = false;

				string glancing = m.Groups[5].Value;
				if (!String.IsNullOrEmpty(glancing) && glancing.Equals("Glancing"))
					hit.Glancing = true;
				else
					hit.Glancing = false;

				string blocked = m.Groups[9].Value;
				if (!String.IsNullOrEmpty(blocked))
				{
					hit.Blocked = blocked.Equals("Blocked");
					hit.Penetrated = blocked.Equals("Penetrated");
				}

				Hit(null, hit);
			}
		}

		void OnOtherEvadedYou(Match m)
		{
			if (Evade != null)
			{
				EvadeEventArgs evade = new EvadeEventArgs();
				DateTime timestamp;
				if (DateTime.TryParse(m.Groups[1].Value, out timestamp))
					evade.Timestamp = timestamp;
				else
					evade.Timestamp = DateTime.MinValue;
				evade.Attacker = "You";
				evade.Evader = m.Groups[2].Value;
				evade.AttackType = m.Groups[3].Value;

				Evade(null, evade);
			}
		}

		void OnYouEvadedOther(Match m)
		{
			if (Evade != null)
			{
				EvadeEventArgs evade = new EvadeEventArgs();
				DateTime timestamp;
				if (DateTime.TryParse(m.Groups[1].Value, out timestamp))
					evade.Timestamp = timestamp;
				else
					evade.Timestamp = DateTime.MinValue;
				evade.Attacker = m.Groups[3].Value;
				evade.Evader = "You";
				evade.AttackType = m.Groups[4].Value;

				Evade(null, evade);
			}
		}

		void OnOtherEvadedOther(Match m)
		{
			if (Evade != null)
			{
				EvadeEventArgs evade = new EvadeEventArgs();
				DateTime timestamp;
				if (DateTime.TryParse(m.Groups[1].Value, out timestamp))
					evade.Timestamp = timestamp;
				else
					evade.Timestamp = DateTime.MinValue;
				evade.Attacker = m.Groups[3].Value;
				evade.Evader = m.Groups[2].Value;
				evade.AttackType = m.Groups[4].Value;

				Evade(null, evade);
			}
		}

		void OnYouHealed(Match m)
		{
			if (Heal != null)
			{
				HealEventArgs heal = new HealEventArgs();
				
				DateTime timestamp;
				if (DateTime.TryParse(m.Groups[1].Value, out timestamp))
					heal.Timestamp = timestamp;
				else
					heal.Timestamp = DateTime.MinValue;
				
				heal.Healer = "You";
				heal.Target = m.Groups[4].Value;
				heal.HealType = m.Groups[3].Value;

				uint amount;
				if (uint.TryParse(m.Groups[5].Value, out amount))
					heal.Amount = amount;

				string critical = m.Groups[2].Value;
				if (!String.IsNullOrEmpty(critical) && critical.Equals("Critical"))
					heal.Critical = true;
				else
					heal.Critical = false;

				Heal(null, heal);
			}
		}

		void OnOtherHealed(Match m)
		{
			if (Heal != null)
			{
				HealEventArgs heal = new HealEventArgs();
				
				DateTime timestamp;
				if (DateTime.TryParse(m.Groups[1].Value, out timestamp))
					heal.Timestamp = timestamp;
				else
					heal.Timestamp = DateTime.MinValue;

				heal.Healer = m.Groups[3].Value;
				heal.Target = m.Groups[5].Value;
				heal.HealType = m.Groups[4].Value;

				uint amount;
				if (uint.TryParse(m.Groups[6].Value, out amount))
					heal.Amount = amount;

				string critical = m.Groups[2].Value;
				if (!String.IsNullOrEmpty(critical) && critical.Equals("Critical"))
					heal.Critical = true;
				else
					heal.Critical = false;

				Heal(null, heal);
			}
		}

		void OnXpGain(Match m)
		{
			if (XP != null)
			{
				XpEventArgs xp = new XpEventArgs();

				DateTime timestamp;
				if (DateTime.TryParse(m.Groups[1].Value, out timestamp))
					xp.Timestamp = timestamp;
				else
					xp.Timestamp = DateTime.MinValue;
			
				uint xpAmount;
				if (uint.TryParse(m.Groups[2].Value, out xpAmount))
					xp.XP = xpAmount;

				XP(null, xp);
			}
		}
	}

	[System.Diagnostics.DebuggerDisplay("{Attacker,nq} hit {Target,nq} with {Attack,nq} for {Damage,nq} {Type,nq} damage")]
	public class HitEventArgs : EventArgs
	{
		public DateTime Timestamp { get; set; }
		public string Attacker { get; set; }
		public string Target { get; set; }
		public string AttackType { get; set; }
		public uint Damage { get; set; }
		public string Type { get; set; }
		public bool Critical { get; set; }
		public bool Glancing { get; set; }
		public bool Penetrated { get; set; }
		public bool Blocked { get; set; }
	}

	public class EvadeEventArgs : EventArgs
	{
		public DateTime Timestamp { get; set;  }
		public string Attacker { get; set; }
		public string Evader { get; set; }
		public string AttackType { get; set; }
	}

	public class HealEventArgs : EventArgs
	{
		public DateTime Timestamp { get; set; }
		public string Healer { get; set; }
		public string Target { get; set; }
		public string HealType { get; set; }
		public uint Amount { get; set; }
		public bool Critical { get; set; }
	}

	public class XpEventArgs : EventArgs
	{
		public DateTime Timestamp { get; set; }
		public uint XP { get; set; }
	}
}
