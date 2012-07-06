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

namespace TSW.CombatParser
{
	public class Evade
	{
		public DateTime Timestamp { get; private set; }
		public string Attacker { get; private set; }
		public string Evader { get; private set; }
		public string AttackType { get; private set; }

		public Evade(EvadeEventArgs e)
		{
			Timestamp = e.Timestamp;
			Attacker = e.Attacker;
			Evader = e.Evader;
			AttackType = e.AttackType;
		}
	}

	public class EvadeCollection : ICollection<Evade>
	{
		private List<Evade> evades = new List<Evade>();

		public uint TotalEvades { get; private set; }

		#region ICollection<Evade> implementation
		public void Add(Evade item)
		{
			evades.Add(item);

			++TotalEvades;
		}

		public void Clear()
		{
			evades.Clear();
		}

		public bool Contains(Evade item)
		{
			return evades.Contains(item);
		}

		public void CopyTo(Evade[] array, int arrayIndex)
		{
			evades.CopyTo(array, arrayIndex);
		}

		public int Count
		{
			get { return evades.Count; }
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		public bool Remove(Evade item)
		{
			return evades.Remove(item);
		}

		public IEnumerator<Evade> GetEnumerator()
		{
			return evades.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return evades.GetEnumerator();
		}
		#endregion
	}
}
