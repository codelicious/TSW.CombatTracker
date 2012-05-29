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
			CollectionViewSource cvs = FindResource("CharactersSource") as CollectionViewSource;
			cvs.View.Refresh();

			Combat.Refresh();
		}

		private void CombatDisplay_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
		{

		}
	}
}
