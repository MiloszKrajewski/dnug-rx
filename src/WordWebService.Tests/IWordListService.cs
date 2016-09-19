using System;

namespace WordWebService.Tests
{
	public interface IWordListService
	{
		IObservable<string[]> Fetch(string word);
	}
}