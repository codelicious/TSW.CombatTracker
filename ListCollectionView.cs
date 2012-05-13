using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace TSW.CombatTracker
{

	public class ListCollectionView : System.Windows.Data.ListCollectionView
	{
		private Dictionary<string, bool> sortOrGroupFields = new Dictionary<string, bool>();

		public ListCollectionView(IList collection) : base(collection)
		{
			CollectionChanged += ListCollectionView_CollectionChanged;
			GroupDescriptions.CollectionChanged += SortOrGroupDescriptions_CollectionChanged;
			((INotifyCollectionChanged)SortDescriptions).CollectionChanged += SortOrGroupDescriptions_CollectionChanged;
		}

		protected void ListCollectionView_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Reset:
					// Iterated over all of the existing objects, remove any existing handlers, add new NotifyPropertyChanged handlers
					foreach (object item in this.SourceCollection)
					{
						INotifyPropertyChanged inpc = item as INotifyPropertyChanged;
						if (inpc != null)
						{
							inpc.PropertyChanged -= item_PropertyChanged;
							inpc.PropertyChanged += item_PropertyChanged;
						}
					}
					break;

				case NotifyCollectionChangedAction.Add:
					// Add NotifyPropertyChanged handlers to new items
					foreach (object item in e.NewItems)
					{
						INotifyPropertyChanged inpc = item as INotifyPropertyChanged;
						if (inpc != null)
						{
							inpc.PropertyChanged += item_PropertyChanged;
						}
					}
					break;

				case NotifyCollectionChangedAction.Remove:
					// Remove any existing NotifyPropertyChanged handlers
					foreach (object item in e.OldItems)
					{
						INotifyPropertyChanged inpc = item as INotifyPropertyChanged;
						if (inpc != null)
						{
							inpc.PropertyChanged -= item_PropertyChanged;
						}
					}
					break;
			}
		}

		private void SortOrGroupDescriptions_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			SetupSortAndGroupProperties();
		}

		private void SetupSortAndGroupProperties()
		{
			sortOrGroupFields.Clear();

			foreach (SortDescription sd in SortDescriptions)
				sortOrGroupFields[sd.PropertyName] = true;

			foreach (GroupDescription gd in GroupDescriptions)
			{
				PropertyGroupDescription pgd = gd as PropertyGroupDescription;
				if (pgd != null)
					sortOrGroupFields[pgd.PropertyName] = true;
			}
		}

		private void item_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (sortOrGroupFields.ContainsKey(e.PropertyName))
			{
				EditItem(sender);
				CommitEdit();
			}
		}

	}
}
