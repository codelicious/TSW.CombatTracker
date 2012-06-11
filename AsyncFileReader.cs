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
		Subject<long> update;

		public AsyncFileReader()
		{
			subject = new Subject<string>();
			update = new Subject<long>();
		}

		public Subject<string> Subject { get { return subject; } }

		public Subject<long> Update { get { return update; } }

		public void Read(FileStream fileStream)
		{
			if (asyncReader != null)
				throw new Exception("AsyncFileReader is already running");

			reader = new StreamReader(fileStream);

			asyncReader = Observable.Interval(TimeSpan.FromSeconds(1.0)).ObserveOn(Scheduler.CurrentThread).Subscribe(i =>
			{
				string line;
				bool readLines = false;

				while ((line = reader.ReadLine()) != null)
				{
					subject.OnNext(line);
					// It would be really nice if there was a way to know if the line actually caused something to update.
					// I'll just have to settle for assuming that if any lines were read, there was probably at least one
					// damage or heal update.
					readLines = true;
				}

				if (readLines)
					update.OnNext(i);
			});
		}

		public void Close()
		{
			asyncReader.Dispose();
			reader.Close();
		}
	}
}
