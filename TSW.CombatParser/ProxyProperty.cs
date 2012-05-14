using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;

namespace TSW.CombatParser
{
	public class ProxyProperty<T>: IComparer<ProxyProperty<T>>
	{
		private INotifyPropertyChanged obj;
		private string propertyName;
		private object sender;
		private string senderProperty;
		private PropertyChangedEventHandler handler;

		private PropertyInfo propertyInfo;

		public ProxyProperty(INotifyPropertyChanged obj, string propertyName, object sender, string senderProperty, PropertyChangedEventHandler handler)
		{
			this.obj = obj;
			this.propertyName = propertyName;
			this.sender = sender;
			this.senderProperty = senderProperty;
			this.handler = handler;

			Type objType = obj.GetType();
			propertyInfo = objType.GetProperty(this.propertyName);
			if (propertyInfo == null)
				throw new ArgumentException(String.Format("Property {0} does not exist on type {1}", this.propertyName, objType.FullName));
			
			this.obj.PropertyChanged += owner_PropertyChanged;
		}

		public override string ToString()
		{
			object result = propertyInfo.GetValue(this, null);
			return result.ToString();
		}

		private void owner_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName.Equals(propertyName))
			{
				if (handler != null)
					handler(this.sender, new PropertyChangedEventArgs(this.senderProperty));
			}
		}

		public static implicit operator T(ProxyProperty<T> proxy)
		{
			T result = default(T);

			result = (T)proxy.propertyInfo.GetValue(proxy.obj, null);

			return result;
		}

		public int Compare(ProxyProperty<T> x, ProxyProperty<T> y)
		{
			return Comparer<T>.Default.Compare(x, y);
		}
	}
}
