using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using DiffPlex;
using DiffPlex.Model;

namespace LiveDiff
{
	class Program
	{
		private static IDiffer engine = new Differ();
		static void Main(string[] args)
		{
			var fileName = "textfile.txt";

			var empty = Observable.Return(string.Empty);

			var content =
				Observable.Interval(TimeSpan.FromSeconds(1))
				.Select(_ => File.Exists(fileName)).Where(e => e)
				.Select(_ => File.ReadAllText(fileName));

			var diffs = empty.Concat(content).Pairwise().SelectMany(Diff);

			diffs.Subscribe(Console.WriteLine);

			Console.ReadLine();
		}

		private static IEnumerable<string> Diff(string[] pair)
		{
			var source = pair[0];
			var target = pair[1];
			var diffs = engine.CreateLineDiffs(source, target, false);
			return diffs.DiffBlocks.SelectMany(d => ExpandDiffBlock(d, source, target));
		}

		private static IEnumerable<string> ExpandDiffBlock(
			DiffBlock block, string source, string target)
		{
			if (block.DeleteCountA != 0)
			{
				foreach (var line in ExtractLines(source, block.DeleteStartA, block.DeleteCountA))
					yield return string.Format("---: {0}", line);
			}

			if (block.InsertCountB != 0)
			{
				foreach (var line in ExtractLines(target, block.InsertStartB, block.InsertCountB))
					yield return string.Format("+++: {0}", line);
			}
		}

		private static IEnumerable<string> ExtractLines(string text, int start, int length)
		{
			return text
				.Split('\n')
				.Skip(start)
				.Take(length)
				.Select(l => l.Trim())
				.Where(l => !string.IsNullOrWhiteSpace(l));
		}
	}
}
