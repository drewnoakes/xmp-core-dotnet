using System;
using System.Collections.Generic;

namespace Sharpen
{
    public sealed class EnumeratorWrapper<T> : Iterator<T>
    {
        private readonly object _collection;
        private IEnumerator<T> _e;
        private T _lastVal;
        private bool _more;
        private bool _copied;

        public EnumeratorWrapper(object collection, IEnumerator<T> e)
        {
            _e = e;
            _collection = collection;
            _more = e.MoveNext();
        }

        public override bool HasNext() => _more;

        public override T Next()
        {
            if (!_more)
                throw new InvalidOperationException();
            _lastVal = _e.Current;
            _more = _e.MoveNext();
            return _lastVal;
        }

        public override void Remove()
        {
            if (!(_collection is ICollection<T> col))
                throw new NotSupportedException();

            if (_more && !_copied)
            {
                // Read the remaining elements, since the current enumerator
                // will be invalid after removing the element
                var remaining = new List<T>();
                do
                {
                    remaining.Add(_e.Current);
                } while (_e.MoveNext());
                _e = remaining.GetEnumerator();
                _e.MoveNext();
                _copied = true;
            }
            col.Remove(_lastVal);
        }
    }
}
