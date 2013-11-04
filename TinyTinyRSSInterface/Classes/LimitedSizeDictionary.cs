using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TinyTinyRSS.Interface.Classes
{
    public class LimitedSizeDictionary<TKey, TValue> : Dictionary<TKey, TValue>
    {
        Queue<TKey> queue;
        int size;

        public LimitedSizeDictionary(int size) : base()
        {
            this.size = size;
            queue = new Queue<TKey>(size);
        }

        public new void Add(TKey key, TValue value)
        {
            base.Add(key, value);
            if (queue.Count == size)
                base.Remove(queue.Dequeue());
            queue.Enqueue(key);
        }

        public new bool Remove(TKey key)
        {
            if (base.Remove(key))
            {
                Queue<TKey> newQueue = new Queue<TKey>(size);
                foreach (TKey item in queue)
                    if (!base.Comparer.Equals(item, key))
                        newQueue.Enqueue(item);
                queue = newQueue;
                return true;
            }
            else
                return false;
        }
    }
}
