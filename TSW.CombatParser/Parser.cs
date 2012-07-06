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
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TSW.CombatParser
{
	public class CombatParser
	{
		#region Event-matching regular expressions
		static Regex timestampEx = new Regex(@"\[(\d\d:\d\d:\d\d)\] (.*)", RegexOptions.Compiled);

		static Regex yourHitEx = new Regex(@"(?:\((Normal|Critical|Blocked|Penetrated)\) ){0,1}Your (.+) hits \((Normal|Glancing)\) (.+) for (\d+) (physical|magical) damage\. \((Normal|Critical|Blocked|Penetrated)\)", RegexOptions.Compiled);
		static Regex otherHitYouEx = new Regex(@"(?:\((Normal|Critical|Blocked|Penetrated)\) ){0,1}([^\']+)'s (.+) hits \((Normal|Glancing)\) you for (\d+) (physical|magical) damage\. \((Normal|Critical|Blocked|Penetrated)\)", RegexOptions.Compiled);
		static Regex otherHitOtherEx = new Regex(@"(?:\((Normal|Critical|Blocked|Penetrated)\) ){0,1}([^\']+)'s (.+) hits \((Normal|Glancing)\) (.+) for (\d+) (physical|magical) damage\. \((Normal|Critical|Blocked|Penetrated)\)", RegexOptions.Compiled);
		static Regex interrupedEx = new Regex(@"Interrupted!", RegexOptions.Compiled);

		static Regex youHealedEx = new Regex(@"(?:\((Critical)\) ){0,1}Your (.+) heals (.+) for (\d+)\.", RegexOptions.Compiled);
		static Regex otherHealedEx = new Regex(@"(?:\((Critical)\) ){0,1}([^\']+)\'s (.+) heals (.+) for (\d+)\.", RegexOptions.Compiled);

		static Regex someoneBeganAttack = new Regex(@"(.+) start[s]{0,1} using (.+)\.", RegexOptions.Compiled);
		static Regex someoneFinishedAttack = new Regex(@"(.+) successfully used (?:the ){0,1}(.+)\.", RegexOptions.Compiled);

		static Regex otherEvadedYouEx = new Regex(@"(.+) evaded your (.+)\.", RegexOptions.Compiled);
		static Regex youEvadeOtherEx = new Regex(@"(You) evade ([^\']+)\'s (.+)\.", RegexOptions.Compiled);
		static Regex otherEvadedOtherEx = new Regex(@"(.+) evaded (.+)\'s (.+)\.", RegexOptions.Compiled);

		static Regex youAbsorbed = new Regex(@"Your (.+) absorbs (\d+) damage from (.+)'s (.)+\.", RegexOptions.Compiled);
		static Regex otherAbsorbed = new Regex(@"(.+) absorbs (\d+) damage of your (.)+\.", RegexOptions.Compiled);

		// TODO: Add reflect expressions

		static Regex youGainedXpEx = new Regex(@"You gained (\d+) XP\.", RegexOptions.Compiled);
		#endregion

		public event EventHandler<AttackEventArgs> Attack;
		public event EventHandler<HitEventArgs> Hit;
		public event EventHandler<EvadeEventArgs> Evade;
		public event EventHandler<HealEventArgs> Heal;
		public event EventHandler<AbsorbEventArgs> Absorb;
		// TODO: Add reflect event
		public event EventHandler<XpEventArgs> XP;
		public event EventHandler<SkippedEventArgs> Skipped;

		public void Parse(string line)
		{
			Match m = timestampEx.Match(line);
			if (m.Success)
			{
				DateTime timestamp;
				if (DateTime.TryParse(m.Groups[1].Value, out timestamp))
				{
					line = m.Groups[2].Value;

					m = yourHitEx.Match(line);
					if (m.Success)
					{
						OnYouHitOther(timestamp, m);
						return;
					}

					m = otherHitYouEx.Match(line);
					if (m.Success)
					{
						OnOtherHitYou(timestamp, m);
						return;
					}

					m = otherHitOtherEx.Match(line);
					if (m.Success)
					{
						OnOtherHitOther(timestamp, m);
						return;
					}

					m = youHealedEx.Match(line);
					if (m.Success)
					{
						OnYouHealed(timestamp, m);
						return;
					}

					m = otherHealedEx.Match(line);
					if (m.Success)
					{
						OnOtherHealed(timestamp, m);
						return;
					}

					// TODO: Handle start and end of attack
					// N.B. It turns out there may be no good reason to do this. There appears to be no
					// synchronization between attack "boundaries", i.e., starting and completing an attack,
					// and the damage that actually occurs, especially for secondary procs.
#if FALSE
					m = someoneBeganAttack.Match(line);
					if (m.Success)
					{
						return;
					}

					m = someoneFinishedAttack.Match(line);
					if (m.Success)
					{
						return;
					}
#endif
					m = otherEvadedYouEx.Match(line);
					if (m.Success)
					{
						OnOtherEvadedYou(timestamp, m);
						return;
					}

					m = youEvadeOtherEx.Match(line);
					if (m.Success)
					{
						OnYouEvadedOther(timestamp, m);
						return;
					}

					m = otherEvadedOtherEx.Match(line);
					if (m.Success)
					{
						OnOtherEvadedOther(timestamp, m);
						return;
					}

					m = youAbsorbed.Match(line);
					if (m.Success)
					{
						OnYouAbsorbed(timestamp, m);
						return;
					}

					m = otherAbsorbed.Match(line);
					if (m.Success)
					{
						OnOtherAbsorbed(timestamp, m);
						return;
					}

					// TODO: Add reflect parsing

					m = youGainedXpEx.Match(line);
					if (m.Success)
					{
						OnXpGain(timestamp, m);
						return;
					}

					m = interrupedEx.Match(line);
					if (m.Success)
					{
						//OnInterrupted(timestamp, m);
						return;
					}
				}
			}

			if (Skipped != null)
				Skipped(null, new SkippedEventArgs() { Line = line });
		}

		private void OnYouHitOther(DateTime timestamp, Match m)
		{
			if (Hit != null)
			{
				HitEventArgs hit = new HitEventArgs();
				hit.Timestamp = timestamp;
				hit.Attacker = "You";
				hit.Target = m.Groups[4].Value;
				hit.AttackType = m.Groups[2].Value;
				hit.Type = m.Groups[6].Value;
				
				uint damage;
				if (uint.TryParse(m.Groups[5].Value, out damage))
					hit.Damage = damage;
				else
					hit.Damage = 0;
				
				string critical = m.Groups[1].Value;
				if (!String.IsNullOrEmpty(critical) && critical.Equals("Critical"))
					hit.Critical = true;
				else
					hit.Critical = false;

				string glancing = m.Groups[3].Value;
				if (!String.IsNullOrEmpty(glancing) && glancing.Equals("Glancing"))
					hit.Glancing = true;
				else
					hit.Glancing = false;

				string blocked = m.Groups[7].Value;
				if (!String.IsNullOrEmpty(blocked))
				{
					hit.Blocked = blocked.Equals("Blocked");
					hit.Penetrated = blocked.Equals("Penetrated");
				}

				Hit(null, hit);
			}
		}

		private void OnOtherHitYou(DateTime timestamp, Match m)
		{
			if (Hit != null)
			{
				HitEventArgs hit = new HitEventArgs();
				hit.Timestamp = timestamp;
				hit.Attacker = m.Groups[2].Value;
				hit.Target = "You";
				hit.AttackType = m.Groups[3].Value;
				hit.Type = m.Groups[6].Value;

				uint damage;
				if (uint.TryParse(m.Groups[5].Value, out damage))
					hit.Damage = damage;
				else
					hit.Damage = 0;

				string critical = m.Groups[1].Value;
				if (!String.IsNullOrEmpty(critical) && critical.Equals("Critical"))
					hit.Critical = true;
				else
					hit.Critical = false;

				string glancing = m.Groups[4].Value;
				if (!String.IsNullOrEmpty(glancing) && glancing.Equals("Glancing"))
					hit.Glancing = true;
				else
					hit.Glancing = false;

				string blocked = m.Groups[7].Value;
				if (!String.IsNullOrEmpty(blocked))
				{
					hit.Blocked = blocked.Equals("Blocked");
					hit.Penetrated = blocked.Equals("Penetrated");
				}

				Hit(null, hit);
			}
		}

		private void OnOtherHitOther(DateTime timestamp, Match m)
		{
			if (Hit != null)
			{
				HitEventArgs hit = new HitEventArgs();
				hit.Timestamp = timestamp;
				hit.Attacker = m.Groups[2].Value;
				hit.Target = m.Groups[5].Value;
				hit.AttackType = m.Groups[3].Value;
				hit.Type = m.Groups[7].Value;

				uint damage;
				if (uint.TryParse(m.Groups[6].Value, out damage))
					hit.Damage = damage;
				else
					hit.Damage = 0;

				string critical = m.Groups[1].Value;
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

		void OnOtherEvadedYou(DateTime timestamp, Match m)
		{
			if (Evade != null)
			{
				EvadeEventArgs evade = new EvadeEventArgs();
				evade.Timestamp = timestamp;
				evade.Attacker = "You";
				evade.Evader = m.Groups[1].Value;
				evade.AttackType = m.Groups[2].Value;

				Evade(null, evade);
			}
		}

		void OnYouHealed(DateTime timestamp, Match m)
		{
			if (Heal != null)
			{
				HealEventArgs heal = new HealEventArgs();
				heal.Timestamp = timestamp;
				heal.Healer = "You";
				heal.Target = m.Groups[3].Value;
				heal.HealType = m.Groups[2].Value;

				uint amount;
				if (uint.TryParse(m.Groups[4].Value, out amount))
					heal.Amount = amount;

				string critical = m.Groups[1].Value;
				if (!String.IsNullOrEmpty(critical) && critical.Equals("Critical"))
					heal.Critical = true;
				else
					heal.Critical = false;

				Heal(null, heal);
			}
		}

		void OnOtherHealed(DateTime timestamp, Match m)
		{
			if (Heal != null)
			{
				HealEventArgs heal = new HealEventArgs();
				heal.Timestamp = timestamp;
				heal.Healer = m.Groups[2].Value;
				heal.Target = m.Groups[4].Value;
				heal.HealType = m.Groups[3].Value;

				uint amount;
				if (uint.TryParse(m.Groups[5].Value, out amount))
					heal.Amount = amount;

				string critical = m.Groups[1].Value;
				if (!String.IsNullOrEmpty(critical) && critical.Equals("Critical"))
					heal.Critical = true;
				else
					heal.Critical = false;

				Heal(null, heal);
			}
		}

		void OnYouEvadedOther(DateTime timestamp, Match m)
		{
			if (Evade != null)
			{
				EvadeEventArgs evade = new EvadeEventArgs();
				evade.Timestamp = timestamp;
				evade.Attacker = m.Groups[2].Value;
				evade.Evader = "You";
				evade.AttackType = m.Groups[3].Value;

				Evade(null, evade);
			}
		}

		void OnOtherEvadedOther(DateTime timestamp, Match m)
		{
			if (Evade != null)
			{
				EvadeEventArgs evade = new EvadeEventArgs();
				evade.Timestamp = timestamp;
				evade.Attacker = m.Groups[2].Value;
				evade.Evader = m.Groups[1].Value;
				evade.AttackType = m.Groups[3].Value;

				Evade(null, evade);
			}
		}

		void OnYouAbsorbed(DateTime timestamp, Match m)
		{
			if (Absorb != null)
			{
				AbsorbEventArgs absorb = new AbsorbEventArgs();
				absorb.Timestamp = timestamp;
				absorb.Attacker = m.Groups[3].Value;
				absorb.Target = "You";
				absorb.BarrierType = m.Groups[1].Value;

				uint damage;
				if (uint.TryParse(m.Groups[2].Value, out damage))
					absorb.Damage = damage;
				else
					absorb.Damage = 0;

				Absorb(null, absorb);
			}
		}

		void OnOtherAbsorbed(DateTime timestamp, Match m)
		{
			// "(.+) absorbs (\d+) damage of your (.)+\."

			if (Absorb != null)
			{
				AbsorbEventArgs absorb = new AbsorbEventArgs();
				absorb.Timestamp = timestamp;
				absorb.Attacker = "You";
				absorb.Target = m.Groups[1].Value;
				absorb.BarrierType = m.Groups[3].Value;

				uint damage;
				if (uint.TryParse(m.Groups[2].Value, out damage))
					absorb.Damage = damage;
				else
					absorb.Damage = 0;

				Absorb(null, absorb);
			}
		}

		void OnXpGain(DateTime timestamp, Match m)
		{
			if (XP != null)
			{
				XpEventArgs xp = new XpEventArgs();
				xp.Timestamp = timestamp;

				uint xpAmount;
				if (uint.TryParse(m.Groups[1].Value, out xpAmount))
					xp.XP = xpAmount;

				XP(null, xp);
			}
		}
	}

	public class AttackEventArgs : EventArgs
	{
		public DateTime Timestamp { get; set; }
		public string Attacker { get; set; }
		public string Attack { get; set; }
		public AttackState State { get; set; }
	}

	public enum AttackState
	{
		Started,
		Completed,
		Interrupted
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

	public class AbsorbEventArgs : EventArgs
	{
		public DateTime Timestamp { get; set; }
		public string Attacker { get; set; }
		public string Target { get; set; }
		public string BarrierType { get; set; }
		public uint Damage { get; set; }
	}

	public class XpEventArgs : EventArgs
	{
		public DateTime Timestamp { get; set; }
		public uint XP { get; set; }
	}

	public class SkippedEventArgs : EventArgs
	{
		public string Line { get; set; }
	}
}
