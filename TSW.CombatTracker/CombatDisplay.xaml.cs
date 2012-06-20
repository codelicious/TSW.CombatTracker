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

		private IEditableCollectionView charactersView = null;
		private IEditableCollectionView damageDealersView = null;


		public CombatDisplay()
		{
			InitializeComponent();
		}

		private void CombatDisplay_Loaded(object sender, RoutedEventArgs e)
		{
		}

		private void DamageDealersSource_Filter(object sender, FilterEventArgs e)
		{
			Character ch = e.Item as Character;
			if (ch != null)
				e.Accepted = !ch.IsMob;
			else
				e.Accepted = false;
		}

		private IEditableCollectionView CharactersView
		{
			get
			{
				if (charactersView == null)
				{
					CollectionViewSource charactersSource = FindResource("CharactersSource") as CollectionViewSource;
					if (charactersSource != null)
						charactersView = charactersSource.View as IEditableCollectionView;
				}

				return charactersView;
			}
		}

		private IEditableCollectionView DamageDealersView
		{
			get
			{
				if (damageDealersView == null)
				{
					CollectionViewSource damageDealersSource = FindResource("DamageDealersSource") as CollectionViewSource;
					if (damageDealersSource != null)
						damageDealersView = damageDealersSource.View as IEditableCollectionView;
				}

				return damageDealersView;
			}
		}
	}
}
