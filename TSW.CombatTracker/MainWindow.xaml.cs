using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Input;
using TSW.CombatParser;

namespace TSW.CombatTracker
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window, INotifyPropertyChanged
	{
		public MainWindow()
		{
			InitializeComponent();

			VerifyLogFolder();

			MouseDown += delegate(object sender, MouseButtonEventArgs e)
			{
				if (e.LeftButton == MouseButtonState.Pressed)
				{
					DragMove();
					Properties.Settings.Default.Save();
				}
			};

			IsPinned = false;
			IsMinimized = false;

			CombatDisplay.Combat = Combat;

			DataContext = this;
		}

		public Combat Combat { get { return combatParser; } }

		public bool IsPinned { get; private set; }

		public bool IsMinimized { get; private set; }

		public FileInfo CombatLog { get; private set; }



		private Combat combatParser = new Combat();

		private AsyncFileReader logReader = null;

		private IDisposable logSubject = null;

		private FileSystemWatcher fileWatcher = null;

		private IDisposable logWatcher = null;

		private IDisposable combatDisplayUpdater = null;

		protected override void OnMouseEnter(MouseEventArgs e)
		{
			if (MinButton.IsChecked.HasValue && MinButton.IsChecked.Value)
			{
				ContentPanel.Visibility = Visibility.Visible;
				Height = normalHeight;
			}
		}

		protected override void OnMouseLeave(MouseEventArgs e)
		{
			if (MinButton.IsChecked.HasValue && MinButton.IsChecked.Value)
			{
				normalHeight = Height;
				ContentPanel.Visibility = Visibility.Collapsed;
				Height = Double.NaN;
			}
		}

		private void RunButton_Checked(object sender, RoutedEventArgs e)
		{
			if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
			{
				SelectFileDialog dlg = new SelectFileDialog();
				if (dlg.ShowDialog())
				{
					ProcessLog(new FileInfo(dlg.Filename));
				}
				else
					RunButton.IsChecked = false;
			}
			else
			{
				// Start processing the logs
				Run();
			}
		}

		private void RunButton_Unchecked(object sender, RoutedEventArgs e)
		{
			Reset();
		}

		private void ClearButton_Click(object sender, RoutedEventArgs e)
		{
			bool wipeCharacters = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
			combatParser.Reset(wipeCharacters);
			CombatDisplay.Refresh();
		}

		double normalHeight = Double.NaN;

		private void MinButton_Checked(object sender, RoutedEventArgs e)
		{
			normalHeight = Height;
			ContentPanel.Visibility = Visibility.Collapsed;
			Height = Double.NaN;
		}

		private void MinButton_Unchecked(object sender, RoutedEventArgs e)
		{
			ContentPanel.Visibility = Visibility.Visible;
			Height = normalHeight;
		}

		private void CloseCommandHandler(object sender, ExecutedRoutedEventArgs e)
		{
			Close();
		}

		public void Run()
		{
			DirectoryInfo tswFolder = new DirectoryInfo(Properties.Settings.Default["TSWFolder"] as String);

			var combatLogQuery = from combatLog in tswFolder.GetFiles("Combat*")
								 orderby combatLog.Name
								 select combatLog;
			
			if (combatLogQuery.Count() > 0)
				ProcessLog(combatLogQuery.Last());

			fileWatcher = new FileSystemWatcher(tswFolder.FullName, "Combat*.txt");

			logWatcher = Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
				h => fileWatcher.Created += h,
				h => fileWatcher.Deleted -= h).ObserveOnDispatcher().Subscribe(e =>
					{
						Reset();
						ProcessLog(new FileInfo(e.EventArgs.FullPath));
					});

			fileWatcher.EnableRaisingEvents = true;
		}

		public void ProcessLog(FileInfo combatLog)
		{
			CombatLog = combatLog;
			OnPropertyChanged("CombatLog");

			logReader = new AsyncFileReader();
			logSubject = logReader.Subject.ObserveOnDispatcher().Subscribe((line) =>
			{
				combatParser.ProcessLine(line);
			});

			combatDisplayUpdater = logReader.Update.ObserveOnDispatcher().Subscribe((b) =>
			{
				CombatDisplay.Refresh();
			});

			FileStream combatStream = File.Open(combatLog.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			logReader.Read(combatStream);
		}

		public void Reset()
		{
			if (combatDisplayUpdater != null)
			{
				combatDisplayUpdater.Dispose();
				combatDisplayUpdater = null;
			}

			if (logWatcher != null)
			{
				logWatcher.Dispose();
				logWatcher = null;
			}

			if (logSubject != null)
			{
				logSubject.Dispose();
				logSubject = null;
			}

			if (logReader != null)
			{
				logReader.Close();
				logReader = null;
			}

			combatParser.Reset(true);

			CombatLog = null;
			OnPropertyChanged("CombatLog");

			CombatDisplay.Reset();
		}

		private void VerifyLogFolder()
		{
			string TSWFolder = Properties.Settings.Default["TSWFolder"] as string;
			if (String.IsNullOrEmpty(TSWFolder))
			{
				SelectFolderDialog dlg = new SelectFolderDialog();
				dlg.SetTitle("Select The Secret World Installation Folder");
				dlg.SetFileName("The Secret World");
				if (dlg.ShowDialog())
				{
					Properties.Settings.Default["TSWFolder"] = dlg.FolderName;
					Properties.Settings.Default.Save();
				}
				else
					Application.Current.Shutdown();
			}
		}

		#region INotifyPropertyChanged region
		public event PropertyChangedEventHandler PropertyChanged;

		private void OnPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
		#endregion
	}
}
