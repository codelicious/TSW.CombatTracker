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
using System.IO;
using System.Threading;
using System.Windows;

namespace TSW.CombatTracker
{
	public class AsyncFileReader : IDisposable
	{
		StreamReader reader = null;
		Timer asyncReadTimer = null;

		public AsyncFileReader()
		{
		}

		public event EventHandler<LineEventArgs> Line;

		public event EventHandler<EventArgs> Update;

		public void Read(FileStream fileStream)
		{
			if (reader != null)
				throw new Exception("AsyncFileReader is already running");

			reader = new StreamReader(fileStream);

			asyncReadTimer = new Timer(o =>
				{
					string line;
					bool readLines = false;

					while ((line = reader.ReadLine()) != null)
					{
						OnLine(line);
						// It would be really nice if there was a way to know if the line actually caused something to update.
						// I'll just have to settle for assuming that if any lines were read, there was probably at least one
						// damage or heal update.
						readLines = true;
					}

					if (readLines)
						OnUpdate();

					asyncReadTimer.Change(1000, Timeout.Infinite);
				});

			// Start the first read
			asyncReadTimer.Change(0, Timeout.Infinite);
		}

		public void Close()
		{
			if (asyncReadTimer != null)
			{
				asyncReadTimer.Change(Timeout.Infinite, Timeout.Infinite);
				asyncReadTimer.Dispose();
				asyncReadTimer = null;
			};

			reader.Close();
		}

		public void Dispose()
		{
			Close();
		}

		private void OnLine(string line)
		{
			if (Line != null)
			{
				Application.Current.Dispatcher.BeginInvoke(
					new Action(() => Line(this, new LineEventArgs() { Line = line })),
					null);
			}
		}

		private void OnUpdate()
		{
			if (Update != null)
			{
				Application.Current.Dispatcher.BeginInvoke(
					new Action(() => Update(this, EventArgs.Empty)),
					null);
			}
		}
	}

	public class LineEventArgs : EventArgs
	{
		public string Line { get; set; }
	}
}
