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
		private static readonly TimeSpan FIVE_SECS = TimeSpan.FromSeconds(5);

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

		public static IEnumerable<string> Fetch(string prefix)
		{
			Console.WriteLine("{0:u} filter: {1}", DateTime.Now, prefix);
			var result = _words.Where(w => w.StartsWith(prefix)).ToArray();
			Console.WriteLine("{0:u} sleep: {1}", DateTime.Now, prefix);
			Thread.Sleep(FIVE_SECS);
			Console.WriteLine("{0:u} return: {1}", DateTime.Now, prefix);
			return result;
		}

		public static async Task<IEnumerable<string>> FetchAsync(string prefix)
		{
			Console.WriteLine("{0:u} filter: {1}", DateTime.Now, prefix);
			var result = _words.Where(w => w.StartsWith(prefix)).ToArray();
			Console.WriteLine("{0:u} sleep: {1}", DateTime.Now, prefix);
			await Task.Delay(FIVE_SECS);
			Console.WriteLine("{0:u} return: {1}", DateTime.Now, prefix);
			return result;
		}
	}
}
