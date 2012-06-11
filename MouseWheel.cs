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
		MouseWheelEventHandler handler = null;

		public void Capture()
		{
			handler = new MouseWheelEventHandler(MouseWheel_MouseWheelEvent);

			Register(handler, 0);
		}

		private void MouseWheel_MouseWheelEvent(int code, UIntPtr wParam, UIntPtr lParam)
		{
		}

		public void Release()
		{
			Unregister();
		}

		public void Dispose()
		{
			Release();
		}

		public event MouseWheelEventHandler MouseWheelEvent;

		[DllImport("MouseWheelHook.dll", EntryPoint = "Register", SetLastError = false,
			CallingConvention = CallingConvention.StdCall)]
		private static extern bool Register(MouseWheelEventHandler handler, int threadId);

		[DllImport("MouseWheelHook.dll", EntryPoint = "Unregister", SetLastError = false,
			CallingConvention = CallingConvention.StdCall)]
		private static extern bool Unregister();
	}

	public delegate void MouseWheelEventHandler(int code, UIntPtr wParam, UIntPtr lParam);

}
