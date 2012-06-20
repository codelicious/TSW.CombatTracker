using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;

namespace TSW.CombatTracker
{
	public class MouseWheel : IDisposable
	{
		public void Capture()
		{
			System.Diagnostics.Debug.WriteLine("Capture mouse events");
			Register((code, wParam, lParam) =>
			{
				if (MouseWheelEvent != null)
					MouseWheelEvent(code, wParam, lParam);
			});
		}

		private void MouseWheel_MouseWheelEvent(int code, UIntPtr wParam, UIntPtr lParam)
		{
		}

		public void Release()
		{
			System.Diagnostics.Debug.WriteLine("Release mouse events");
			Unregister();
		}

		public void Dispose()
		{
			Release();
		}

		public event MouseWheelEventHandler MouseWheelEvent;

		[DllImport("MouseWheelHook.dll", CallingConvention = CallingConvention.StdCall, SetLastError = false)]
		private static extern void Load();

		[DllImport("MouseWheelHook.dll", SetLastError = false, CallingConvention = CallingConvention.StdCall)]
		private static extern bool Register(MouseWheelEventHandler handler);

		[DllImport("MouseWheelHook.dll", SetLastError = false, CallingConvention = CallingConvention.StdCall)]
		private static extern bool Unregister();
	}

	public delegate void MouseWheelEventHandler(int code, UIntPtr wParam, UIntPtr lParam);
}
