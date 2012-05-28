using System;
using System.IO;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace TSW.CombatTracker
{
	public class AsyncFileReader
	{
		StreamReader reader;
		IDisposable asyncReader;
		Subject<string> subject;

		public AsyncFileReader()
		{
			subject = new Subject<string>();
		}

		public Subject<string> Subject { get { return subject; } }

		public void Read(FileStream fileStream)
		{
			if (asyncReader != null)
				throw new Exception("AsyncFileReader is already running");

			reader = new StreamReader(fileStream);

			asyncReader = Observable.Interval(TimeSpan.FromSeconds(0.5)).ObserveOn(Scheduler.CurrentThread).Subscribe(i =>
			{
				string line;

				while ((line = reader.ReadLine()) != null)
					subject.OnNext(line);
			});
		}

		public void Close()
		{
			asyncReader.Dispose();
			reader.Close();
		}
	}
}
