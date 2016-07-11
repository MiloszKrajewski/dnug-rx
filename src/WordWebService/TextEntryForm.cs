using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Forms;

namespace WordWebService
{
	public partial class TextEntryForm: Form
	{
		public TextEntryForm()
		{
			InitializeComponent();

			var textChanges = Observable
				.FromEventPattern(
					h => edit.TextChanged += h,
					h => edit.TextChanged -= h)
				.Select(_ => edit.Text);

			textChanges
				.Throttle(TimeSpan.FromMilliseconds(500))
				.DistinctUntilChanged()
				.Where(text => !string.IsNullOrWhiteSpace(text))
				.Select(text => WordList.FetchAsync(text))
				.Switch()
				.ObserveOn(this)
				.Subscribe(items => SetList(items));
		}

		private void SetList(IEnumerable<string> items)
		{
			list.Items.Clear();
			list.Items.AddRange(items.ToArray());
		}
	}
}
