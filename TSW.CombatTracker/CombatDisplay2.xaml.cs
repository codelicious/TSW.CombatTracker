﻿using System;
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

#if HOOK_SCROLL
		private MouseWheel mouseWheelHook = null;
#endif

		private void CharacterSelector_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
		{
#if HOOK_SCROLL
			Window parent = FindVisualParent<Window>(this);
			if (parent != null && !parent.IsActive)
			{
				if (mouseWheelHook == null)
				{
					mouseWheelHook = new MouseWheel();
					mouseWheelHook.MouseWheelEvent += mouseWheelHook_MouseWheelEvent;
					mouseWheelHook.Capture();
				}
			}
#endif
		}

		private T FindVisualParent<T>(DependencyObject reference) where T : DependencyObject
		{
			DependencyObject parent = reference;

			do
			{
				parent = VisualTreeHelper.GetParent(parent);
			} while (parent != null && !(parent.GetType().IsSubclassOf(typeof(T))));

			return parent as T;
		}

		private void CharacterSelector_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
		{
#if HOOK_SCROLL
			if (mouseWheelHook != null)
			{
				mouseWheelHook.Dispose();
				mouseWheelHook = null;
			}
#endif
		}

		void mouseWheelHook_MouseWheelEvent(int code, UIntPtr wParam, UIntPtr lParam)
		{
			throw new NotImplementedException();
		}

		private void CharacterSelector_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
		{
		}

	}
}
