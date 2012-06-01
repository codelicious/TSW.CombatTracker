using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace TSW.CombatParser
{
	public class RefreshList<T> : ICollection<T>, IEnumerable<T>, INotifyCollectionChanged
	{
		private List<T> list;
		private bool isModified = false;

		public RefreshList()
		{
			list = new List<T>();
		}

		public void Refresh()
		{
			if (isModified)
			{
				if (CollectionChanged != null)
					CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

				isModified = false;
			}
		}

		#region ICollection<T> implementation
		public void Add(T item)
		{
			list.Add(item);
			isModified = true;
		}

		public void Clear()
		{
			list.Clear();
			isModified = true;
		}

		public bool Contains(T item)
		{
			return list.Contains(item);
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			list.CopyTo(array, arrayIndex);
		}

		public int Count
		{
			get { return list.Count; }
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		public bool Remove(T item)
		{
			bool result = list.Remove(item);
			if (result)
				isModified = true;

			return result;
		}
		#endregion

		#region IEnumerable<T> implementation
		public IEnumerator<T> GetEnumerator()
		{
			return list.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return ((System.Collections.IEnumerable)list).GetEnumerator();
		}

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return ((IEnumerable<T>)list).GetEnumerator();
		}
		#endregion

		public event NotifyCollectionChangedEventHandler CollectionChanged;
	}
}
