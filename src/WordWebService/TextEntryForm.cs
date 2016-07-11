using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
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
				.Select(WordList.FetchAsync)
				.Switch()
				.ObserveOn(this)
				.Subscribe(LoadWords);
		}

		private void LoadWords(string[] items)
		{
			list.Items.Clear();
			list.Items.AddRange(items);
		}
	}
}
