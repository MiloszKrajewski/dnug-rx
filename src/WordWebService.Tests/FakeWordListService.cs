using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace WordWebService.Tests
{
	public class FakeWordListService : IWordListService
	{
		private readonly TimeSpan _delay;
		private readonly IScheduler _scheduler;
		private readonly List<Timestamped<string>> _history;
		private readonly string[] _words;

		public FakeWordListService(
			TimeSpan delay,
			IScheduler scheduler,
			string[] words = null)
		{
			_delay = delay;
			_scheduler = scheduler;
			_words = words ?? new string[0];
			_history = new List<Timestamped<string>>();
		}

		public IObservable<string[]> Fetch(string word)
		{
			_history.Add(Timestamped.Create(word, _scheduler.Now));
			var result = _words.Where(w => w.StartsWith(word)).ToArray();
			return Observable.Return(result).Delay(_delay, _scheduler);
		}

		public IList<Timestamped<string>> History => _history;
	}
}