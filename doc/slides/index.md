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
- BLOBAs @ Sepura
- self proclaimed FRP evangelist
- first line of code written in ~1984
- C, C++, C#, SQL, Java, Delphi
- (Iron)Python, F#, Scala, Kotlin

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
        handler.Raise(this, newValue);
    }
}
```

---

My personal winner:

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
public interface ISubject<T> {
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

### Rx interfaces

```csharp
// Action<T>
public interface IObserver<T> {
    void OnNext(T item);
    // void OnComplete();
    // void OnError(Exception);
}

// Func<Action<T>, Action>
public interface IObservable<T> {
    IDisposable Subscribe(IObserver<T> observer);
}

public interface ISubject<T>: IObservable<T>, IObserver<T> { }
```

---

### So how can I use Rx for events?

The best way to implement events with Reactive Extensions is to use `Subject<T>` class (`PublishSubject<T>`). The idea behind `PublishSubject<T>` is that you publish a value (an event) and whoever is observing subject receives it.

```csharp
var subject = new Subject<string>();
//...
subject.OnNext("is anyone listening?");
```

---

Also, `Subscribe(...)` has some overloads which allow to use lambdas instead of `IObserver<T>` (although, it creates lightweight `IObserver<T>` behind the scene).

```csharp
keyPressed.Subscribe(k => Console.WriteLine("Pressed: {0}", k));
```

---

`Subscribe` returns `IDisposable` if unregistration is needed:

```csharp
var subscription = keyPressed.Subscribe(...);
//...
subscription.Dispose();
```

---

Note, it's worth to invest into some kind of `DisposableBag`:

```csharp
class DisposableBag: IDisposable {
    private List<IDisposable> _bag = new List<IDisposable>();
    public void Add(IDisposable other) { _bag.Add(other); }
    public void Dispose() { _bag.ForEach(d => d.Dispose()); }
}
```

(note, it's the simplest possible implementation)

---

so multiple subscriptions can be managed:

```csharp
bag.Add(eventA.Subscribe(...));
bag.Add(eventB.Subscribe(...));
bag.Add(eventC.Subscribe(...));
//...
bad.Dispose();
```

---

What we have already:

* `IObservable<T>` can be passed around
* Lambda-friendly `Unsubscribe`
* Null-aware `OnNext(...)`

---

Event emitter implemented with `IObservable`...

```csharp
class Dice {
    private Random _generator = new Random();

    private ISubject<int> _rolled = new Subject<int>();
    public IObservable<int> OnRolled { get { return _rolled; } }

    public void Roll() {
        _rolled.OnNext(_generator.Next(6) + 1);
    }
}
```

---

...and event handler implemented with (implicit) `IObserver`:

```csharp
class Player {
    public Player(Dice dice) {
        dice.OnRolled.Subscribe(
            d => Console.WriteLine("I see dice: {0}", d));
    }
}
```

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

Let's do this for `IEnumerable` and `IEnumerator`<br>(step by step)

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

(note, `IDisposable` is technical detail, not essence)

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

`PutNext` is `OnNext`

```csharp
interface IObserver<T> {
    void OnNext(T | void | Exception);
}
```

with no discriminated unions, `OnNext` is implemented as three methods:

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
```

familiar?

***

### Rx observables are kind of like Promises

> In computer science, future (or promise) describes an object that acts as a
proxy for a result that is initially unknown, usually because the computation
of its value is yet incomplete. -- *Wikipedia* (kind of)

---

(...insert three hour talk about M-things...)

---

So `Promise[T]` is the type which encapsulates the possibility of having `T` in the future. In .NET the implementation of `Promise[T]` is `Task<T>`.

```csharp
task.ContinueWith(
    t => Console.WriteLine("Result finally received: {0}", t.Result));
```

When `task` returns/produces a value continuation is called with the value passed in `Result` property.

---

...and that's exactly what `IObservable<T>` does as well: it calls `OnNext(T)` for all subscribers when result is produced.

---

`Task<T>` is a little bit more than just `Promise[T]`, it is actually `Promise[Either[T, Exception]]`, as it may deliver `Exception` instead of `T`.

```csharp
task.ContinueWith(
    t => Console.WriteLine("Task crashed with: {0}", t.Exception),
    TaskContinuationOptions.OnlyOnFaulted);
```

When `task` throws an exception continuation is called with exception passed in `Exception` property.

---

...and that's exactly what `IObservable<T>` does as well: it calls `OnError(Exception)` for all subscribers when exception is thrown.

---

`Task` (no `<T>`) does not really deliver value, it just finishes at some point in time. Let's say it is `Promise[Either[Unit, Exception]]` or `Promise[Maybe[Exception]]`.

```csharp
task.ContinueWith(
    t => Console.WriteLine("Yup, done."),
    TaskContinuationOptions.OnlyOnRanToCompletion);
```

When `task` finishes it does not deliver value, it calls continuation with information saying "yup, it's finished".

---

...and that's exactly what `IObservable<T>` does as well: it calls `OnComplete()` for all subscribers when sequence is finished.

***

|                    | One       | Many             |
|-------------------:|:---------:|:----------------:|
| **Synchronously**  | `T`       | `IEnumerable<T>` |
| **Asynchronously** | `Task<T>` | `IObservable<T>` |

***

### So how to use it?

---



***

### http://reactivex.io

**Bart de Smet** @ DevCamp 2010<br>
"Rx: Curing your asynchronous programming blues"<br>
http://bit.ly/1qsXfsx

**Mike Taulty** @ DevDays 2011<br>
"Reactive Extensions for .NET for the Rest of Us"<br>
http://bit.ly/1PSoukV
