using NUnit.Framework;
using System;
using System.Linq;
using System.Reactive.Concurrency;

namespace WordWebService.Tests
{
	[TestFixture]
	public class TextEntryFormTests
	{
		private HistoricalScheduler _scheduler;
		private FakeWordListService _service;
		private TextEntryFormViewModel _viewModel;

		[SetUp]
		public void Setup()
		{
			_scheduler = new HistoricalScheduler();
			_service = new FakeWordListService(
				TimeSpan.FromSeconds(5), _scheduler,
				new[] {
					"new york",
					"chicago",
					"los angeles",
					"seattle",
					"miami",
					"denver",
					"detroit",
					"huston"
				});
			_viewModel = new TextEntryFormViewModel(
				_service,
				TimeSpan.FromMilliseconds(500),
				_scheduler);
		}

		[Test]
		public void AsLongAsUserIsTypingNoCallIsMade()
		{
			_viewModel.Text.OnNext("d");
			_scheduler.AdvanceBy(TimeSpan.FromSeconds(0.4));
			_viewModel.Text.OnNext("de");
			_scheduler.AdvanceBy(TimeSpan.FromSeconds(0.4));
			_viewModel.Text.OnNext("den");
			_scheduler.AdvanceBy(TimeSpan.FromSeconds(0.4));
			_viewModel.Text.OnNext("denv");
			_scheduler.AdvanceBy(TimeSpan.FromSeconds(0.4));

			Assert.AreEqual(0, _service.History.Count);
		}

		[Test]
		public void WhenUserStopsTypingCallIsMade()
		{
			_viewModel.Text.OnNext("d");
			_scheduler.AdvanceBy(TimeSpan.FromSeconds(0.4));
			_viewModel.Text.OnNext("de");
			_scheduler.AdvanceBy(TimeSpan.FromSeconds(0.4));
			_viewModel.Text.OnNext("den");
			_scheduler.AdvanceBy(TimeSpan.FromSeconds(0.4));
			_viewModel.Text.OnNext("denv");
			_scheduler.AdvanceBy(TimeSpan.FromSeconds(0.4));

			Assert.AreEqual(0, _service.History.Count);

			_scheduler.AdvanceBy(TimeSpan.FromSeconds(0.2));

			Assert.AreEqual(1, _service.History.Count);
			Assert.AreEqual("denv", _service.History.First().Value);
		}

		[Test]
		public void WhenServiceReturnsDataViewModelPublishesIt()
		{
			string[] response = null;
			_viewModel.Words.Subscribe(r => response = r);

			_viewModel.Text.OnNext("de");
			_scheduler.AdvanceBy(TimeSpan.FromSeconds(0.4));

			Assert.AreEqual(0, _service.History.Count);
			Assert.AreEqual(null, response);

			_scheduler.AdvanceBy(TimeSpan.FromSeconds(0.2));

			Assert.AreEqual(1, _service.History.Count);
			Assert.AreEqual("de", _service.History.First().Value);
			Assert.AreEqual(null, response);

			_scheduler.AdvanceBy(TimeSpan.FromSeconds(5));

			CollectionAssert.AreEquivalent(
				new[] { "denver", "detroit" },
				response);
		}

	}
}
