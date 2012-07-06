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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using TSW.CombatParser;

namespace TSW.CombatTracker
{
	/// <summary>
	/// Interaction logic for CombatDisplay.xaml
	/// </summary>
	public partial class CombatDisplay : UserControl
	{
		public Combat Combat { get; set; }

		public CombatDisplay()
		{
			InitializeComponent();
		}

		public void Refresh()
		{
			Combat.Refresh();

			CollectionViewSource source = FindResource("CharactersSource") as CollectionViewSource;
			IEditableCollectionView view = source.View as IEditableCollectionView;
			foreach (Character character in Combat.Characters)
			{
				view.EditItem(character);
				view.CommitEdit();
			}
		}

		public void Reset()
		{
			CollectionViewSource source = FindResource("CharactersSource") as CollectionViewSource;
			source.View.Refresh();
		}

		private void CharactersSource_Filter(object sender, FilterEventArgs e)
		{
			Character character = e.Item as Character;
			if (character != null)
				e.Accepted = !character.IsMob;
			else
				e.Accepted = true;
		}
	}
}
