using System;

namespace Immaterium
{
    public class Subscriber<T>
    {
        private readonly Action<T> _action;

        public event EventHandler<T> OnMessage;

        public Subscriber(Action<T> action)
        {
            _action = action;
        }

        public void Invoke(T result)
        {
            _action?.Invoke(result);
            OnMessage?.Invoke(this, result);
        }
    }

    public class Subscriber : Subscriber<ImmateriumMessage>
    {
        public Subscriber(Action<ImmateriumMessage> action) : base(action)
        {
        }
    }
}