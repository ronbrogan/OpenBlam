﻿using System.Collections.Generic;
using System.Linq;

namespace OpenBlam.Core.Collections
{
    public class ContinuationStack<T> 
    {
        private Stack<(T, bool)> store = new Stack<(T, bool)>();

        public void PushFull(T value)
        {
            this.store.Push((value, true));
            this.store.Push((value, false));
        }

        public void PushSeparate(T value, bool isContinuation)
        {
            this.store.Push((value, isContinuation));
        }

        public bool TryPop(out T value, out bool isContinuation)
        {
            if(this.store.Any())
            {
                var values = this.store.Pop();
                value = values.Item1;
                isContinuation = values.Item2;
                return true;
            }

            value = default;
            isContinuation = default;
            return false;
        }
    }
}
