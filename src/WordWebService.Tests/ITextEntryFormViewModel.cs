using System;

namespace WordWebService.Tests
{
	public interface ITextEntryFormViewModel
	{
		IObserver<string> Text { get; }
		IObservable<string[]> Words { get; }
	}
}