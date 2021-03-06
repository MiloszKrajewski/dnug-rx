- title : Reactive Extensions, the answer to the ultimate question of life, the universe, and everything
- description : a gentle introduction to Reactive Extensions
- author : Milosz Krajewski
- theme : beige
- transition : default

***

## Reactive Extensions

![Eveything is a stream](images/dog-stream.jpg)

### the answer to the ultimate question of life, the universe, and everything

***

### About me

- Milosz Krajewski
- Full-stack @ Reckon
- first line of code written in ~1984
- C#, SQL, ES6
- Python, F#, Kotlin, Scala
- C/C++

***

### http://reactivex.io

**Bart de Smet** @ DevCamp 2010<br>
"Rx: Curing your asynchronous programming blues"<br>
http://bit.ly/1qsXfsx

**Mike Taulty** @ DevDays 2011<br>
"Reactive Extensions for .NET for the Rest of Us"<br>
http://bit.ly/1PSoukV

***

### Free lunch is over

![cpu](images/cpu-speed.png)

---

### C10k

* lighttpd, nginx, twisted, node (*)
* actors
* async / await
* reactive extensions
* erlang / OTP
* WhatsApp (C2M @ 2010)

---

### Node ?

![bodil](images/bodil-node.png)

---

### What it does?

```csharp
var counter = 0;
for (var i = 0; i < 10000; i++) {
    new Thread(() => Interlocked.Increment(ref counter)).Start();
}
```

---

### ...and this?

```csharp
for (var i = 0; i < 10000; i++) {
    Task.Run(() => Interlocked.Increment(ref counter));
}
```

***

### Platforms

* Rx.NET, ReactiveUI (WinForms, WPF, Xamarin)
* RxJava, RxScala, RxKotlin, RxGroovy, RxClojure, RxJavaFX, RxSwing, RxNetty, RxAndroid
* RxJS, RxLua, RxPython, RxRuby
* RxCpp, RxSwift, RxPHP, RxRust, RxGo

---

![companies](images/reactivex-companies.png)

***

### Familiar != Easy != Simple

* Polish is neither **easy** nor **simple**, but for some people is **familiar**
* successful stock trading is **simple** (buy low and sell high), it's not **easy** though

---

Reactive programming is **simple** but not **easy**.<br>
It's worth to make it **familiar**.

(Reactive Extensions are great it's just me...)

***

### Observer Pattern

![Observer Pattern](images/observer-wikipedia.png)

the GoF version in Java...

---

...and converted to C#

```csharp
public interface IObserver<T> {
    void OnNotification(T item); // Notify
}

public interface ISubject<T> {
    void Subscribe(IObserver<T> observer); // registerObserver
    void Unsubscribe(IObserver<T> observer); // unregisterObserver
    void Notify(T item); // notifyObservers
}
```

***

### .NET events are equivalent of GoF observers

```csharp
// Subject
public event Action<T> OnNotification; // = _ => {};

// Observer.OnNotification(item)
public void OnNotificationHandler(T item) { ... }

// Subject.Subscribe(Observer)
this.OnNotification += OnNotificationHandler;

// Subject.Unsubscribe(Observer)
this.OnNotification -= OnNotificationHandler;

// Subject.Notify(item)
this.OnNotification(item);
```

***

### What's wrong with .NET events?

* Not first-class citizens (cannot pass around)
* Clunky unsubscribe (anonymous handlers)
* Null event handlers (managing race conditions)
* Lapsed listener problem (event handler leak)

(Java does not really help you with events, either)

---

### Not first-class citizens

```csharp
public class Producer {
	public event EventHandler<int> OnProduced;
}

void AttachPrinter(EventHandler<int> handler) {
	handler += (s, e) => Console.WriteLine(e);
}

void Main() {
	var producer = new Producer();
	AttachPrinter(producer.OnProduced);
}
```

Does it compile?

---

<pre>
The event 'Producer.OnProduced' can only appear on the left hand
side of += or -= (except when used from within the type 'Producer')
</pre>

---

### Clunky unsubscribe

```csharp
public class Producer {
	public event EventHandler<int> OnProduced;
}

void Main() {
	var producer = new Producer();
	producer.OnProduced += (s, e) => Console.WriteLine(e);
	producer.OnProduced -= (s, e) => Console.WriteLine(e);
}
```

---

### Null event handlers

Official documentation does it wrong (https://goo.gl/CEK0xm):

```csharp
class Emitter {
    public event EventHandler<int> Changed;
    public void OnChanged(int newValue) {
        if (Changed != null)
            Changed(this, newValue); // BANG!!!
    }
}
```

---

Recommended solution:

```csharp
class Emitter {
    public event EventHandler<int> Changed;
    public void OnChanged(int newValue) {
        var handler = Changed;
        if (handler != null)
            handler(this, newValue);
    }
}
```

---

Recommended solution with extension method:

```csharp
public static class EventExtensions {
    public static void Raise<T>(
        this EventHandler<T> handler, object sender, T args)
    {
        if (handler == null) return;
        handler(this, args);
    }
}

class Emitter {
    public event EventHandler<int> Changed;
    public void OnChanged(int newValue) {
        Changed.Raise(this, newValue);
    }
}
```

---

My personal winner is 'untouchable' lambda:

```csharp
class Emitter {
    public event EventHandler<int> Changed = (s, e) => { };
    public void OnChanged(int newValue) {
        Changed(this, newValue);
    }
}
```

---

### Lapsed listener problem

```csharp
public class LeakingForm: Form {
	public LeakingForm(EventBus bus) {
		bus.OnEvent += (s, e) => editBox.Text = e.Text;
	}
}
```

if lifetime of `form` and `bus` is different the form will leak...

---

...to avoid that, we need to unsubscribe on close,
but to unsubscribe we cannot use anonymous handler...

```csharp
public class LessLeakingForm: Form {
    public LessLeakingForm(EventBus bus) {
        bus.OnEvent += HandleTextEdit;
        FormClosed += (s, e) => bus.OnEvent -= HandleTextEdit;
    }
    public void HandleTextEdit(object sender, TextEventArgs args) {
        editBox.Text = args.Text;
    }
}
```

...now, for a change, the `bus` may leak even if no longer used

***

### So what are Reactive Extensions?

> An API for asynchronous programming with observable streams. -- reactivex.io

---

when reading between the lines we get:

* **observable**: Events
* **streams**: Enumerables
* **asynchronous**: Tasks

![Everything](images/everything.png)

---

***

### Rx observables are like events<br>(and GoF subjects)

```csharp
public interface IObserver<T> { // GoF
    void OnNotification(T item);
}
```

```csharp
public interface IObserver<T> { // Rx
    void OnNext(T item);
}
```

---

### GoF vs Rx subjects

```csharp
public interface ISubject<T> { // GoF
    void Subscribe(IObserver<T> observer);
    void Unsubscribe(IObserver<T> observer);
    void Notify(T item);
}
```

GoF `ISubject` is actually two interfaces,<br>
(they missed abstraction opportunity)

```csharp
public interface IObservable<T> {
    void Subscribe(IObserver<T> observer);
    void Unsubscribe(IObserver<T> observer);
}

public interface ISubject<T>: IObservable<T> {
    void Notify(T item);
}
```

---

```csharp
public interface IObservable<T> {
    void Subscribe(IObserver<T> observer);
    void Unsubscribe(IObserver<T> observer);
}
```

`Unsubscribe` is actually redundant...


```csharp
public interface IObservable<T> {
    IDisposable Subscribe(IObserver<T> observer);
}
```

...if replaced by `IDisposable`

---

while `ISubject` is meant to be called...

```csharp
public interface ISubject<T>: IObservable<T> {
    void Notify(T item);
}
```

...the `IObserver` is mean to be implemented...

```csharp
public interface IObserver<T> {
    void OnNext(T item);
}
```

...but they are the same, therefore:

```csharp
public interface ISubject<T>: IObservable<T>, IObserver<T> { }
```

---

### Rx interfaces for events

```csharp
// Action<T>
public interface IObserver<T> {
    void OnNext(T item);
}

// Func<Action<T>, Action>
public interface IObservable<T> {
    IDisposable Subscribe(IObserver<T> observer);
}

public interface ISubject<T>: IObservable<T>, IObserver<T> { }
```

(it covers GoF Subject with 3 methods, and Observer with 1 method)

---

### 4 kinds of subjects

* `PublishSubject<T>` / `Subject<T>`
* `BehaviourSubject<T>`
* `ReplaySubject<T>`
* `AsyncSubject<T>`

---

### So how can I use Rx for events?

The way to implement events is to use `Subject<T>`<br>
(also known as `PublishSubject<T>`):

```csharp
var subject = new Subject<string>();
//...
subject.OnNext("is anyone listening?");
```

---

`Subscribe(...)` has overloads allowing to use lambdas:

```csharp
keyPressed.Subscribe(k => Console.WriteLine("Pressed: {0}", k));
```

(lightweight `IObserver<T>` is created behind the scene)

---

`Subscribe(...)` returns `IDisposable`:

```csharp
var subscription = keyPressed.Subscribe(...);
//...
subscription.Dispose();
```

...to help control subscription lifespan.

---

It's worth to invest into some kind of `DisposableBag`:

```csharp
public class DisposableBag: IDisposable {
    private List<IDisposable> _bag = new List<IDisposable>();
    public void Add(IDisposable other) { _bag.Add(other); }
    public void Dispose() { _bag.ForEach(d => d.Dispose()); }
}
```

(proper implementation should handle add-after-dispose, exception-on-dispose, and be thread-safe)

---

so multiple subscriptions can be managed:

```csharp
bag.Add(eventA.Subscribe(...));
bag.Add(eventB.Subscribe(...));
bag.Add(eventC.Subscribe(...));
//...
bag.Dispose();
```

---

What we have already:

* `IObservable<T>` can be passed around
* Lambda-friendly `Unsubscribe(...)`
* Null-aware `OnNext(...)`

---

So Rx event source is:

```csharp
public class Dice {
    private Random _generator = new Random();

    private ISubject<int> _rolled = new Subject<int>();
    public IObservable<int> OnRolled { get { return _rolled; } }

    public void Roll() {
        _rolled.OnNext(_generator.Next(6) + 1);
    }
}
```

...implemented with `Subject<T>`, exposed as `IObservable<T>`, triggered with `OnNext(T)`...

---

...while Rx event handler is:

```csharp
public class Player {
    public Player(Dice dice) {
        dice.OnRolled.Subscribe(
            d => Console.WriteLine("I see dice: {0}", d));
    }
}
```

...implemented with `Subscribe(...)` using (implicit) `IObserver<T>`.

---

| `EventHandler<Args>` | `IObservable<T>`              |
|:--------------------:|:-----------------------------:|
| event += handler     | observable.Subscribe(handler) |
| event -= handler     | IDisposable                   |
| event()              | subject.OnNext()              |
| handler              | observable.Subscribe(handler) |

***

### Rx observables are 'opposite' (dual) to enumerables

![Duals](images/duals.png)

---

```csharp
SheetOfPaper Print(StreamOfBytes);
```

![Scanner/Printer](images/scanner-printer.jpg)

```csharp
StreamOfBytes Scan(SheetOfPaper);
```

---

### Swap inputs and outputs<br>(and find opposite name)

```csharp
int Parse(string);
string Render(int);
```

```csharp
byte[] Serialize<T>(T);
T Deserialize<T>(byte[]);
```

```csharp
void Consume<T>(T);
T Produce<T>(void);
```
---

Let's do this for `IEnumerable<T>` and `IEnumerator<T>`<br>
(step by step)

---

```csharp
interface IEnumerable<T> {
    IEnumerator<T> GetEnumerator();
}
```

`IEnumerator` is also `IDisposable`

```csharp
interface IEnumerable<T> {
    (IDisposable & IEnumerator<T>) GetEnumerator(void);
}
```

---

```csharp
interface IEnumerator<T> {
    bool MoveNext();
    T Current { get; }
}
```

```csharp
interface IEnumerator<T> {
    (bool | Exception) MoveNext(void);
    T GetCurrent(void);
}
```

```csharp
interface IEnumerator<T> {
    (T | void | Exception) GetNext(void);
    // Either[Maybe[T], Exception]
}
```

---

### Magic!

![magic hat](images/magic-hat.jpg)

### Swap inputs and outputs<br>(and find opposite name)

---

`IEnumerator` becomes `IObserver`

```csharp
interface IEnumerator<T> {
    (T | void | Exception) GetNext(void);
}

interface IObserver<T> {
    void PutNext(T | void | Exception);
}
```

---

`IEnumerable` becomes `IObservable`

```csharp
interface IEnumerable<T> {
    (IDisposable & IEnumerator<T>) GetEnumerator(void);
}

interface IObservable<T> {
    IDisposable SetObserver(IObserver<T>);
}
```

(`IDisposable` is technical detail)

---

setting and observer is subscribing:

```csharp
interface IObservable<T> {
    IDisposable SetObserver(IObserver<T>);
}
```

```csharp
interface IObservable<T> {
    IDisposable Subscribe(IObserver<T>);
}
```

---

```csharp
interface IObserver<T> {
    void PutNext(T | void | Exception);
}
```

`PutNext` is `OnNext`:

```csharp
interface IObserver<T> {
    void OnNext(T | void | Exception);
}
```

...in absence of discriminated unions,<br>
`OnNext` is implemented as three methods:

```csharp
interface IObserver<T> {
    void OnNext(T);
    void OnComplete(void);
    void OnError(Exception);
}
```

---

```csharp
// Action<T|void|Exception>
interface IObserver<T> {
    void OnNext(T);
    void OnComplete();
    void OnError(Exception);
}

// Func<Action<T|void|Exception>, Action>
interface IObservable<T> {
    IDisposable Subscribe(IObserver<T>);
}

public interface ISubject<T>: IObservable<T>, IObserver<T> { }
```

***

### So how can I use Rx for streams?

Almost every single operator defined for `IEnumerable<T>`<br>
can be expected for `IObservable<T>`:

```csharp
keyboard.OnKeyPressed
    .Select(k => k.ToUpper())
    .Where(k => k != 'A')
    .Subscribe(k => Console.WriteLine("Press 'A'. Try again."));
```

...the extra operators are usually related to managing<br>
**time** and **absence** of events.

---

### The operators are not magic

Here's `Where(...)`:

```csharp
public static IObservable<T> Where<T>(
    this IObservable<T> input, Func<T, bool> predicate)
{
    var output = new Subject<T>();
    input.Subscribe(v => { if (predicate(v)) output.OnNext(v); });
    return output;
}
```

---

...and `Select(...)`:

```csharp
public static IObservable<R> Select<T, R>(
    this IObservable<T> input, Func<T, R> selector)
{
    var output = new Subject<R>();
    input.Subscribe(v => output.OnNext(selector(v)));
    return output;
}
```

(this is **not** a good implementation,<br>
it just gives an idea how it works,<br>
use `Observable.Create(...)` instead)

---

```csharp
return Observable.Create(output => {
    /* constructor */
    return () => { /* destructor */ }
});
```

---

```csharp
public static IObservable<R> Select<T, R>(
    this IObservable<T> input, Func<T, R> selector)
{
    return Observable.Create(output => {
        var subscription = input.Subscribe(v => output.OnNext(selector(v)));
        return () => subscription.Dispose();
    });
}
```

```csharp
return Observable.Create(output => {
    return input.Subscribe(v => output.OnNext(selector(v)));
});
```

---

| `IEnumerable<T>` | `IObservable<T>`    |
|:----------------:|:-------------------:|
| yield return i   | output.OnNext(i)    |
| throw e          | output.OnError(e)   |
| yield break      | output.OnComplete() |

***

### Rx observables are kind of like Promises

> In computer science, future (or promise) describes an object that acts as a
proxy for a result that is initially unknown, usually because the computation
of its value is yet incomplete. -- *Wikipedia* (kind of)

---

(...three hour talk about M-things...)

---

`Promise[T]` (`Task<T>` in .NET)<br>
encapsulates the possibility of having `T` in the future.

```csharp
task.ContinueWith(
    t => Console.WriteLine("Result finally received: {0}", t.Result));
```

When `task` returns/produces a value continuation is called with the value passed in `Result` property.

---

...and that's exactly what `IObservable<T>` does as well:<br>
it calls `OnNext(T)` for all subscribers when result is produced.

---

`Promise[T]` is a little bit more though,<br>
it is actually `Promise[Either[T, Exception]]`,<br>
as it may deliver `Exception` instead of `T`.

```csharp
task.ContinueWith(
    t => Console.WriteLine("Task crashed with: {0}", t.Exception),
    TaskContinuationOptions.OnlyOnFaulted);
```

When `task` throws an exception continuation is called with exception passed in `Exception` property.

---

...and that's exactly what `IObservable<T>` does as well:<br>
it calls `OnError(Exception)` for all subscribers when exception is thrown.

---

`Task` (no `<T>`) does not really deliver value,<br>
it just finishes at some point in time.<br>
Let's say it is `Promise[Maybe[Exception]]`.

```csharp
task.ContinueWith(
    t => Console.WriteLine("Yup, done."),
    TaskContinuationOptions.OnlyOnRanToCompletion);
```

When `task` finishes it does not deliver value,<br>
it calls continuation with information saying "yup, it's finished".

---

...and that's exactly what `IObservable<T>` does as well:<br>
it calls `OnComplete()` for all subscribers when sequence is finished.

***

`Observables` share characteristics of both `Promises` and `Sequences`.

---

| `Task<T>`         | `IObservable<T>`    |
|:-----------------:|:-------------------:|
| t.Result          | output.OnNext(i)    |
| t.Exception       | output.OnError(e)   |
| t.AsyncWaitHandle | output.OnComplete() |

---

|                    | One       | Many             |
|-------------------:|:---------:|:----------------:|
| **Synchronously**  | `T`       | `IEnumerable<T>` |
| **Asynchronously** | `Task<T>` | `IObservable<T>` |

---

Reactive Extensions do not provide new concepts,<br>
but rather new view on old ones.

***

### So how to use it?

***

Let's assume there is a form:

![WordList](images/wordlist.png)

---

```csharp
// This interface is demonstration purposes only
// Names used shadow existing Form properties
public interface IWordListView {
    event EventHandler TextChanged;
    string Text { get; }
    void Load(IEnumerable<string> words);
}
```

---

There's also a service allowing to fetch suggestions:

```csharp
public interface IWordListService {
    string[] Fetch(string prefix); // string[]
    Task<string[]> FetchAsync(string prefix); // Task<string[]>
    Task<string[]> FetchAsyncWithCancel(
        string prefix, CancellationToken token); // Task<string[]>
}
```

---

Let's wrap event as `Observable<string>`:

```csharp
var textChanges = Observable
    .FromEventPattern(
        h => edit.TextChanged += h,
        h => edit.TextChanged -= h) // IObservable<EventArgs>
    .Select(_ => edit.Text); // IObservable<string>
```

---

Now we can handle `textChanges` observable,<br>
map it with `Select(WordList.Fetch)`<br>
and then populate listbox with `Subscribe(LoadWords)`:

```csharp
textChanges // IObservable<string>
    .Select(WordList.Fetch) // IObservable<string[]>
    .Subscribe(LoadWords);
```

---

We don't like the fact that it freezes the UI,<br>
so we spawn a task with `Task.Run(...)`<br>
and extract `T` from `Task<T>` with `SelectMany(...)`

```csharp
textChanges // IObservable<string>
    .SelectMany(text => Task.Run(() => WordList.Fetch(text)))
    // IObservable<Task<string[]>> -> IObservable<string[]>
    .Subscribe(LoadWords);
```

---

`SelectMany` (aka `bind` or `flatMap`)

```csharp
IEnumerable<T> SelectMany(IEnumerable<IEnumerable<T>> nested);
// IEnumerable<T> ~ IObservable<T>
IObservable<T> SelectMany(IObservable<IObservable<T>> nested);
// IObservable<T> ~ Task<T>
IObservable<T> SelectMany(IObservable<Task<T>> nested);
```

---

There is one problem though,<br>
it actually crashes because of cross-thread UI access.<br>
We need to use `ObserveOn(...)` to sync to UI thread.

```csharp
textChanges // IObservable<string>
    .SelectMany(WordList.FetchAsync)
    .ObserveOn(this) // IObservable<string[]>
    .Subscribe(LoadWords);
```

---

But we don't need to fetch all the time,<br>
let's wait for user to stop typing with `Throttle(...)`<br>
and avoid duplicates with `DistinctUntilChanged()`:

```csharp
textChanges // IObservable<string>
    .Throttle(TimeSpan.FromMilliseconds(500))
    .DistinctUntilChanged()
    .SelectMany(WordList.FetchAsync) // IObservable<string[]>
    .ObserveOn(this)
    .Subscribe(LoadWords);
```

---

Actually, `SelectMany` introduces the risk of messing order,<br>
`Switch(...)` takes `IObservable<IObservable<T>>` and<br>
flattens it by taking *last*:

```csharp
textChanges // IObservable<string>
    .Throttle(TimeSpan.FromMilliseconds(500))
    .DistinctUntilChanged()
    .Select(WordList.FetchAsync) // IObservable<Task<string[]>>
    .Switch() // IObservable<string[]>
    .ObserveOn(this)
    .Subscribe(LoadWords);
```

---

`Switch` is taking **last** result but it doen't **cancel** previous ones.<br>
Let's add some extension method:

```csharp
public static IObservable<U> SelectLatest<T, U>(
    this IObservable<T> observable,
    Func<T, CancellationToken, Task<U>> selector)
{
    return observable
        .Select(item => Observable.FromAsync(
            token => selector(item, token)))
        .Switch();
}
```

---

So the final implementation is:

```csharp
textChanges // IObservable<string>
    .Throttle(TimeSpan.FromMilliseconds(500))
    .DistinctUntilChanged()
    .SelectLatest(WordList.FetchAsyncWithCancel) 
    // IObservable<string[]>
    .ObserveOn(this)
    .Subscribe(LoadWords);
```

***

## Testing interactions in virtual time

* `ISheduler` / `HistoricalScheduler`
* `ReplaySubject<T>` 
* `output.Timestamped().Subscribe(e => el.Add(e))`

---

```csharp
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
```

***

### Spot the Pattern

---

```csharp
class Socket {
    void Send(byte[] message);
    IObservable<byte[]> Observe();
}
```

```csharp
class Server {
    IObservable<Socket> Listen(int port);
}
```

```csharp
class Client {
    IObservable<Socket> Connect(IPAddress host, int port);
}
```

---

```csharp
class MessageBus {
    void Publish(object message);
    IObservable<T> Subscribe<T>();
}
```

---

```csharp
class MessageQueue {
    void Send(T message);
    IObservable<T> Observe();
}
```

---

```csharp
public void Slideshow(string folder) {
    Observable
        .Interval(TimeSpan.FromSeconds(5))
        .Merge(keyboard.Where(key => key == ' '))
        .Zip(EnumerateImages(folder))
        .Subscribe(image => screen.Image = image);
}
```

---

```csharp
public void Pull<T>(
    this IQueue<T> queue, IObserver<T> observer) 
{
    new Thread(() => {
        while (true) observer.OnNext(queue.Dequeue());
    }).Start();
}
```

---

```csharp
public IObservable<T> Observe(this IQueue<T> queue) {
    return Observable.Create(output => {
        var cancel = new CancellationTokenSource();
        new Thread(() => {
            while (true) {
                cancel.Token.ThrowIfCancellationRequested();
                observer.OnNext(queue.Dequeue(cancel.Token));
            }
        }).Start();
        return () => cancel.Cancel();
    });
}
```

---

```csharp
public void Process<T>(this IObservable<T> observable) {
    var queue = new Queue<T>(); // thread-safe one
    observable.Subscribe(item => queue.Enqueue(item));
    new Thread(() => {
        while (true) {
            var item = queue.Dequeue();
            // ...do stuff...
        }
    }).Start();
}
```

---

```csharp
public class CancellationTokenSource {
    ISubject<bool> _token = new BehaviourSubject<bool>(false);
    IObservable<bool> Token { get { return _token; } }
    void Cancel() { _token.OnNext(true); }
}
```

***

## Observables as Networking and Persistence model

---

To write little *Paint* application<br>
we need and *output*:

```csharp
private static void PaintSegments(
    Graphics gc, Pen pen, IEnumerable<Point[]> segments)
{
    foreach (var segment in segments)
        gc.DrawLine(pen, segment[0], segment[1]);
}
```

---

...and *input*, which is mouse moves and button presses,<br> 
merged into long stream of events, so they can be easily passed around:

```csharp
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
```

---

There is an impedance between input model (`IObservable<MouseEventsArgs>`)<br>
and output model (`IEnumerable<Point[]>`), so we need to convert.

```csharp
private static IObservable<Point[]> MouseToSegments(
    IObservable<MouseEventArgs> mouseEvents)
{
    //...
}
```

---

We may need to split events into multiple streams again:

```csharp
var mouseDown = mouseEvents // IObservable<Button>
    .Select(e => e.Button & MouseButtons.Left)
    .Where(b => b != MouseButtons.None)
    .DistinctUntilChanged();
```

...mouse downs...

---

...mouse moves...

```csharp
var mouseMove = mouseEvents // IObservable<Point>
    .Select(e => e.Location)
    .DistinctUntilChanged();
```

---

...and mouse ups...

```csharp
var mouseUp = mouseEvents // IObservable<Button>
    .SkipUntil(mouseDown) // don't worry about mouseup before mouse downs
    .Select(e => e.Button & MouseButtons.Left)
    .Where(b => b == MouseButtons.None)
    .DistinctUntilChanged();
```

---

...then combine them all...

```csharp
return mouseMove // IObservable<Point>
    .SkipUntil(mouseDown) // ignore until mouse down
    .Pairwise() // IObservable<Point[]>
    .TakeUntil(mouseUp) // take until mouse up
    .Repeat(); // start again
```

---

Painting is constrained by GUI toolkit:

```csharp
private void AttachPainter(IObservable<Point[]> segments, Color color)
{
    var pen = new Pen(color, 6);
    var queue = new ConcurrentQueue<Point[]>();

    panel.Paint += (s, e) => PaintSegments(e.Graphics, pen, queue);

    segments
        .Do(queue.Enqueue)
        .Sample(Framerate) // limit framerate
        .ObserveOn(this) // synchronise to GUI thread
        // .Subscribe(_ => Paint(queue))
        .Subscribe(_ => panel.Invalidate());
}
```

---

### Networking

Having any mechanism to publish `byte[]` packets, we publish our events:

```csharp
private static void AttachPublisher(IObservable<Point[]> segments)
{
    ObservableMQ.Publish(
        segments
            .ObserveOn(TaskPoolScheduler.Default)
            .Select(JsonConvert.SerializeObject)
            .Select(Encoding.UTF8.GetBytes),
        Port);
}
```

---

...and subscribe to them:

```csharp
private void AttachSubscriber(Color color)
{
    var externalEvents =
        ObservableMQ.Subscribe(Address, Port)
            .Select(Encoding.UTF8.GetString)
            .Select(JsonConvert.DeserializeObject<Point[]>);

    AttachPainter(externalEvents, color);
}
```

---

### Persistence

Persisting objects:

```csharp
private static void AttachPersister(
    string fileName, IObservable<Point[]> segments)
{
    segments
        .Select(JsonConvert.SerializeObject)
        .Buffer(TimeSpan.FromSeconds(1)) // write in blocks
        .Subscribe(ll => File.AppendAllLines(fileName, ll));
}
```

---

Loading persisted objects back:

```csharp
private static IEnumerable<Point[]> RestoreSegments(string fileName)
{
    return !File.Exists(fileName)
        ? Enumerable.Empty<Point[]>()
        : File.ReadAllLines(fileName)
            .Select(JsonConvert.DeserializeObject<Point[]>);
}
```

***

## Event injection

---

```csharp
public void LongRunningOperation(bool debug) {
    while (!finished) {
        if (debug) executeDebugActions();
        executeStandardActions();
    }
}
```

---

```csharp
public void LongRunningOperation(Func<bool> debugQuery) {
    while (!finished) {
        if (debugQuery()) executeDebugActions();
        executeStandardActions();
    }
}
```

---

```csharp
public void LongRunningOperation(IObservable<bool> debugStream) {
    volatile bool debug = false; // ManualResetEvent, Interlocked
    debugStream.Subscribe(d => debug = d);
    while (!finished) {
        if (debug) executeDebugActions();
        executeStandardActions();
    }
}
```

***

## Live diff

```csharp
var fileName = "textfile.txt";

var empty = Observable.Return(string.Empty);

var content =
    Observable.Interval(TimeSpan.FromSeconds(1))
    .Select(_ => File.Exists(fileName)).Where(e => e)
    .Select(_ => File.ReadAllText(fileName));

var diffs = empty.Concat(content).Pairwise().SelectMany(Diff);

diffs.Subscribe(Console.WriteLine);

Console.ReadLine();
```

***

## Pairwise

```csharp
public static IObservable<T[]> Pairwise<T>(this IObservable<T> observable)
{
    return Observable.Create<T[]>(observer => {
        var prev = default(T);
        var initialized = false;
        return observable.Subscribe(
            next => {
                if (initialized) observer.OnNext(new[] { prev, next });
                initialized = true;
                prev = next;
            },
            observer.OnError,
            observer.OnCompleted);
    });
}
```

***

Other topics:
* Hot and Cold observables
* Schedulers / Virtual time
* INotifyProperyChanged / RxUI

***

