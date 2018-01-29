using System;
using System.Collections;
using System.Collections.Generic;

namespace Sharpen
{
    public interface IIterator
    {
        bool HasNext();
        object Next();
        void Remove();
    }

    public abstract class Iterator<T> : IEnumerator<T>, IIterator
    {
        private T _lastValue;

        object IIterator.Next() => Next();
        public abstract bool HasNext();
        public abstract T Next();
        public abstract void Remove();

        bool IEnumerator.MoveNext()
        {
            if (!HasNext())
                return false;

            _lastValue = Next();
            return true;
        }

        void IEnumerator.Reset() => throw new NotImplementedException();
        void IDisposable.Dispose() {}
        T IEnumerator<T>.Current => _lastValue;
        object IEnumerator.Current => _lastValue;
    }
}