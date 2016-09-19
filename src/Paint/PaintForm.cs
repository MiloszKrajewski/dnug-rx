using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using Newtonsoft.Json;
using ReactiveMQ;

namespace Paint
{
	public partial class PaintForm : Form
	{
		public PaintForm()
		{
			InitializeComponent();
			InitializePanel(panel);

			var pen = new Pen(Color.Red, 6);
			var segments = new LinkedList<Point[]>();

			panel.Paint += (s, e) => RedrawSegments(e.Graphics, pen, segments);

			var mouseMove =
				Observable.FromEventPattern<MouseEventArgs>(panel, "MouseMove")
				.Sample(TimeSpan.FromSeconds(1.0 / 60))
				.ObserveOn(this)
				.Select(ep => ep.EventArgs);

			var mouseUp =
				Observable.FromEventPattern<MouseEventArgs>(panel, "MouseUp")
				.Select(ep => ep.EventArgs);

			var mouseDown =
				Observable.FromEventPattern<MouseEventArgs>(panel, "MouseDown")
				.Select(ep => ep.EventArgs);

			PublishButton.Click += (s, e) => {
				PublishButton.Enabled = SubscribeButton.Enabled = false;
				var mouseEvents = mouseMove.Merge(mouseDown).Merge(mouseUp);
				ObservableMQ.Publish(
					mouseEvents
					.Select(JsonConvert.SerializeObject)
					.Select(Encoding.UTF8.GetBytes),
					4444);
				WireMouse(mouseEvents, segments);
			};

			SubscribeButton.Click += (s, e) => {
				PublishButton.Enabled = SubscribeButton.Enabled = false;
				var mouseEvents =
					ObservableMQ.Subscribe(IPAddress.Loopback, 4444)
					.Select(Encoding.UTF8.GetString)
					.Select(JsonConvert.DeserializeObject<MouseEventArgs>);
				WireMouse(mouseEvents, segments);
			};
		}

		private void RedrawSegments(Graphics gc, Pen pen, IEnumerable<Point[]> segments)
		{
			foreach (var segment in segments)
				gc.DrawLine(pen, segment[0], segment[1]);
		}

		private IDisposable WireMouse(
			IObservable<MouseEventArgs> mouseEvents,
			ICollection<Point[]> segments)
		{
			var mouseMove = mouseEvents
				.Select(e => e.Location).DistinctUntilChanged();

			var mouseDown = mouseEvents
				.Select(e => e.Button & MouseButtons.Left)
				.DistinctUntilChanged()
				.Where(b => b != MouseButtons.None);

			var mouseUp = mouseEvents
				.Select(e => e.Button & MouseButtons.Left)
				.DistinctUntilChanged()
				.Where(b => b == MouseButtons.None);

			return mouseMove
				.SkipUntil(mouseDown)
				.Pairwise()
				.TakeUntil(mouseUp)
				.Repeat()
				.ObserveOn(this)
				.Do(segments.Add)
				.Subscribe(_ => panel.Invalidate());
		}

		#region implementation ugliness

		private void InitializePanel(Control control)
		{
			typeof(Control).InvokeMember("DoubleBuffered",
				BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.NonPublic,
				null, control, new object[] { true });
		}

		#endregion
	}
}
