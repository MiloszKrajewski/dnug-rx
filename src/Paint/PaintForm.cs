using Newtonsoft.Json;
using ReactiveMQ;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace Paint
{
	public partial class PaintForm : Form
	{
		public PaintForm()
		{
			InitializeComponent();
			InitializePanel(panel);

			#region painting

			var events = MouseEvents();
			var segments = MouseToSegments(events);

			AttachPainter(segments, Color.Blue);

			#endregion

			#region networking

			PublishButton.Click += (s, e) => {
				PublishButton.Enabled = SubscribeButton.Enabled = false;
				AttachPublisher(segments);
			};

			SubscribeButton.Click += (s, e) => {
				PublishButton.Enabled = SubscribeButton.Enabled = false;
				AttachSubscriber(Color.Red);
			};

			#endregion

			#region persistence

			AttachPersister("segments.json", segments);

			Shown += (s, e) => {
				var persisted = RestoreSegments("segments.json").ToObservable();
				AttachPainter(persisted, Color.Green);
			};

			#endregion
		}

		#region painting

		private IObservable<MouseEventArgs> MouseEvents()
		{
			var mouseMove =
				Observable.FromEventPattern<MouseEventArgs>(panel, "MouseMove")
					.Select(ep => ep.EventArgs);

			var mouseUp =
				Observable.FromEventPattern<MouseEventArgs>(panel, "MouseUp")
					.Select(ep => ep.EventArgs);

			var mouseDown =
				Observable.FromEventPattern<MouseEventArgs>(panel, "MouseDown")
					.Select(ep => ep.EventArgs);

			return mouseMove.Merge(mouseDown).Merge(mouseUp);
		}

		private static void PaintSegments(Graphics gc, Pen pen, IEnumerable<Point[]> segments)
		{
			foreach (var segment in segments)
				gc.DrawLine(pen, segment[0], segment[1]);
		}

		private static IObservable<Point[]> MouseToSegments(
			IObservable<MouseEventArgs> mouseEvents)
		{
			var mouseDown = mouseEvents
				.Select(e => e.Button & MouseButtons.Left)
				.Where(b => b != MouseButtons.None)
				.DistinctUntilChanged();

			var mouseMove = mouseEvents
				.Select(e => e.Location)
				.DistinctUntilChanged();

			var mouseUp = mouseEvents
				.SkipUntil(mouseDown)
				.Select(e => e.Button & MouseButtons.Left)
				.Where(b => b == MouseButtons.None)
				.DistinctUntilChanged();

			return mouseMove
				.SkipUntil(mouseDown)
				.Pairwise()
				.TakeUntil(mouseUp)
				.Repeat();
		}

		private void AttachPainter(IObservable<Point[]> segments, Color color)
		{
			var pen = new Pen(color, 6);
			var queue = new ConcurrentQueue<Point[]>();

			panel.Paint += (s, e) => PaintSegments(e.Graphics, pen, queue);

			segments
				.Do(queue.Enqueue)
				.Sample(Framerate)
				.ObserveOn(this)
				.Subscribe(_ => panel.Invalidate());
		}

		#endregion

		#region networking

		private static void AttachPublisher(IObservable<Point[]> segments)
		{
			ObservableMQ.Publish(
				segments
					.ObserveOn(TaskPoolScheduler.Default)
					.Select(JsonConvert.SerializeObject)
					.Select(Encoding.UTF8.GetBytes),
				Port);
		}

		private void AttachSubscriber(Color color)
		{
			var externalEvents =
				ObservableMQ.Subscribe(Address, Port)
					.Select(Encoding.UTF8.GetString)
					.Select(JsonConvert.DeserializeObject<Point[]>);

			AttachPainter(externalEvents, color);
		}

		#endregion

		#region persistence

		private static IEnumerable<Point[]> RestoreSegments(string fileName)
		{
			return !File.Exists(fileName)
				? Enumerable.Empty<Point[]>()
				: File.ReadAllLines(fileName).Select(JsonConvert.DeserializeObject<Point[]>);
		}

		private static void AttachPersister(string fileName, IObservable<Point[]> segments)
		{
			segments
				.Select(JsonConvert.SerializeObject)
				.Buffer(TimeSpan.FromSeconds(1))
				.Subscribe(ll => File.AppendAllLines(fileName, ll));
		}

		#endregion

		#region implementation ugliness

		private static void InitializePanel(Control control)
		{
			typeof(Control).InvokeMember("DoubleBuffered",
				BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.NonPublic,
				null, control, new object[] { true });
		}

		private static TimeSpan Framerate
		{
			get { return TimeSpan.FromSeconds(1.0 / 60); }
		}

		private static IPAddress Address
		{
			get { return IPAddress.Parse(ConfigurationManager.AppSettings["server"] ?? "127.0.0.1"); }
		}

		private static int Port
		{
			get { return int.Parse(ConfigurationManager.AppSettings["port"] ?? "4444"); }
		}


		#endregion
	}
}
