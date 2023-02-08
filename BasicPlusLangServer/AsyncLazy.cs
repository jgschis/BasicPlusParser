using System.Runtime.CompilerServices;

/// <summary>
/// Provides support for asynchronous lazy initialization. This type is fully threadsafe.
/// </summary>
/// <typeparam name="T">The type of object that is being asynchronously initialized.</typeparam>
public sealed class AsyncLazy<T> : Lazy<Task<T>>
{
	public AsyncLazy(Func<T> valueFactory) :
        base(() => Task.Factory.StartNew(valueFactory)) { }

    public AsyncLazy(Func<Task<T>> taskFactory) :
        base(() => Task.Factory.StartNew(() => taskFactory()).Unwrap()) { }

	public TaskAwaiter<T> GetAwaiter() { return Value.GetAwaiter(); }
}