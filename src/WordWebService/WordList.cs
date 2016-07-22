using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WordWebService
{
	public static class WordList
	{
		private static readonly TimeSpan ONE_SEC = TimeSpan.FromSeconds(1);
		private static readonly TimeSpan FIVE_SECS = TimeSpan.FromSeconds(5);
		private static readonly string[] EMPTY_LIST = new string[0];

		private static readonly string[] _words;

		static WordList()
		{
			_words = 
				File.ReadAllLines("./wordlist.txt")
				.Where(w => !string.IsNullOrWhiteSpace(w))
				.Select(w => w.Trim().ToLower())
				.OrderBy(w => w)
				.ToArray();
		}

		public static void Initialize() { /* just force static constructor */ }

		public static string[] Fetch(string prefix)
		{
			if (string.IsNullOrWhiteSpace(prefix))
			{
				Console.WriteLine("{0:u} empty", DateTime.Now);
				return EMPTY_LIST;
			}

			Console.WriteLine("{0:u} [{1}] filter", DateTime.Now, prefix);
			var result = _words.Where(w => w.StartsWith(prefix)).ToArray();
			Console.WriteLine("{0:u} [{1}] sleep", DateTime.Now, prefix);

			for (int i = 1; i <= 5; i++)
			{
				Thread.Sleep(ONE_SEC);
				Console.WriteLine("{0:u} [{1}] {2}s", DateTime.Now, prefix, i);
			}

			Console.WriteLine("{0:u} [{1}] done", DateTime.Now, prefix);
			return result;
		}

		public static async Task<string[]> FetchAsync(string prefix)
		{
			if (string.IsNullOrWhiteSpace(prefix))
			{
				Console.WriteLine("{0:u} empty", DateTime.Now);
				return EMPTY_LIST;
			}

			Console.WriteLine("{0:u} [{1}] filter", DateTime.Now, prefix);
			var result = _words.Where(w => w.StartsWith(prefix)).ToArray();
			Console.WriteLine("{0:u} [{1}] sleep", DateTime.Now, prefix);

			for (var i = 1; i <= 5; i++)
			{
				await Task.Delay(ONE_SEC);
				Console.WriteLine("{0:u} [{1}] {2}s", DateTime.Now, prefix, i);
			}

			Console.WriteLine("{0:u} [{1}] done", DateTime.Now, prefix);
			return result;
		}
	}
}
