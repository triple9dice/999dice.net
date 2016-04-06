using System;
using System.Threading;

namespace Dice.Client.Web
{
    /// <summary>
    /// Supports the .NET 3.5 Begin/End async pattern.
    /// </summary>
    internal sealed class DiceAsyncResult<T> : IAsyncResult, IDisposable where T : DiceResponse
    {
        public T Response;

        public object AsyncState { get; set; }
        ManualResetEvent _AsyncWaitHandle;
        public WaitHandle AsyncWaitHandle
        {
            get
            {
                if (_AsyncWaitHandle != null)
                    return _AsyncWaitHandle;
                ManualResetEvent w = new ManualResetEvent(false);
                if (Interlocked.CompareExchange(ref _AsyncWaitHandle, w, null) != null)
                    w.Close();
                else if (IsCompleted)
                    w.Set();
                return _AsyncWaitHandle;
            }
        }
        public bool CompletedSynchronously { get; set; }
        int Completed;
        public bool IsCompleted { get { return Interlocked.CompareExchange(ref Completed, 1, 1) != 0; } }
        public AsyncCallback Callback;
        public Exception Exception;

        public void Complete(Exception e)
        {
            Exception = e;
            Complete((T)null);
        }
        public void Complete(T response)
        {
            if (response != null)
                Response = response;
            Interlocked.Exchange(ref Completed, 1);
            ManualResetEvent w = _AsyncWaitHandle;
            if (w != null)
                w.Set();
            AsyncCallback c = Callback;
            if (c != null)
                c(this);
        }
        public DiceAsyncResult(AsyncCallback callback, object state)
        {
            Callback = callback;
            AsyncState = state;
        }

        public void Dispose()
        {
            WaitHandle w = _AsyncWaitHandle;
            if (w != null)
                w.Close();
        }
    }
}
