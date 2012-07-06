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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using TSW.CombatParser;

namespace ShowLogSkips
{
	// ShowLogSkips is a diagnostic tool for finding combat log entries that aren't handled by any
	// expression but should be.
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
