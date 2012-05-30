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
	public partial class CombatDisplay2 : UserControl
	{
		public Combat Combat { get; set; }

		public CombatDisplay2()
		{
			InitializeComponent();
		}

		public void Refresh()
		{
			CollectionViewSource source = FindResource("CharactersSource") as CollectionViewSource;
			IEditableCollectionView view = source.View as IEditableCollectionView;
			foreach (Character character in Combat.Characters)
			{
				view.EditItem(character);
				view.CommitEdit();
			}

			Combat.Refresh();
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
