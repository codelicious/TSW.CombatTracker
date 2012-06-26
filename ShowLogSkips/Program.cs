using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using TSW.CombatParser;

namespace ShowLogSkips
{
	class Program
	{
		static CombatParser parser;

		static void Main(string[] args)
		{
			parser = new CombatParser();
			parser.Skipped += parser_SkippedLine;

			foreach (string arg in args)
			{
				ProcessEntry(arg);
			}
		}

		static void ProcessEntry(string entry)
		{
			DirectoryInfo dir = new DirectoryInfo(Environment.CurrentDirectory);
			FileInfo[] files = dir.GetFiles(entry);
			foreach (FileInfo file in files)
				ProcessFile(file);
		}

		static void ProcessFile(FileInfo file)
		{
			StreamReader r = new StreamReader(file.FullName);
			string line;

			while ((line = r.ReadLine()) != null)
				parser.Parse(line);

			r.Close();
		}

		static void parser_SkippedLine(object sender, SkippedEventArgs e)
		{
			Console.WriteLine(e.Line);
		}
	}
}
