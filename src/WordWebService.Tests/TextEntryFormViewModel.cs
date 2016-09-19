using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace WordWebService.Tests
{
	public class TextEntryFormViewModel: ITextEntryFormViewModel
	{
		private readonly Subject<string> _text;
		private readonly Subject<string[]> _words;

		public TextEntryFormViewModel(
			IWordListService service, 
			TimeSpan? throttle = null,
			IScheduler scheduler = null)
		{
			scheduler = scheduler ?? Scheduler.Default;
			throttle = throttle ?? TimeSpan.FromSeconds(0.5);

			_text = new Subject<string>();
			_words = new Subject<string[]>();

			_text
				.Throttle(throttle.Value, scheduler)
				.DistinctUntilChanged()
				.Select(service.Fetch)
				.Switch()
				.Subscribe(_words);
		}

		public IObserver<string> Text { get { return _text; } }
		public IObservable<string[]> Words { get { return _words; } }
	}
}
