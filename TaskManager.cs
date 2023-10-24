using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Collections;

namespace TaskManager_Priotity
{
    class PriorityQueue<TValue> : PriorityQueue<TValue, int> where TValue : INotifyPropertyChanged { }
    class PriorityQueue<TValue, TPriority> : IEnumerable<TValue>, INotifyCollectionChanged
        where TValue : INotifyPropertyChanged
        where TPriority : IComparable
    {
        private SortedDictionary<TPriority, Queue<TValue>> dict = new SortedDictionary<TPriority, Queue<TValue>>(new ReverseComparer());
        private int count = 0;

        public int Count { get { return count; } }
        public bool Empty { get { return Count == 0; } }

        public void ChangePriority(TValue value, TPriority oldPriority, TPriority newPriority)
        {
            if (!dict.ContainsKey(oldPriority)) return;
            var oldQueue = dict[oldPriority];
            if (!oldQueue.Contains(value)) return;
            dict.Remove(oldPriority);
            count -= oldQueue.Count;
            var newQueue = new Queue<TValue>(oldQueue.Where(x => !x.Equals(value)));
            if(newQueue.Count != 0)
            {
                dict.Add(oldPriority, newQueue);
                count += newQueue.Count;
            }
            Enqueue(value, newPriority);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        private class ReverseComparer : IComparer<TPriority>
        {
            public int Compare(TPriority x, TPriority y) { return y.CompareTo(x); }
        }

        public void Enqueue(TValue val, TPriority pri = default(TPriority))
        {
            ++count;
            if (!dict.ContainsKey(pri)) dict[pri] = new Queue<TValue>();
            dict[pri].Enqueue(val);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, val));
        }

        public TValue Dequeue()
        {
            --count;
            var pair = dict.First();
            var queue = pair.Value;
            var val = queue.Dequeue();
            if (queue.Count == 0) dict.Remove(pair.Key);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            return val;
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (CollectionChanged != null)
            {
                CollectionChanged(this, e);
            }
        }

        public IEnumerator<TValue> GetEnumerator()
        {
            foreach (var queue in dict.Values)
            {
                foreach (var value in queue)
                {
                    yield return value;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class TaskEntry : INotifyPropertyChanged
    {
        public TaskEntry(string n, float value, int priority)
        {
            name = n;
            TimeLeft = value;
            Priority = priority;
        }
        private string name;
        public string Name
        {
            get => name;
            set
            {
                name = value;
                OnPropertyChanged("Name");
            }
        }
        private float timeLeft;
        public float TimeLeft
        {
            get => timeLeft;
            set
            {
                timeLeft = value;
                OnPropertyChanged("TimeLeft");
            }
        }
        private int priority;
        public int Priority
        {
            get => priority;
            set
            {
                priority = value;
                OnPropertyChanged("Priority");
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
    }
    public class TaskManager
    {
        class TaskEntryPriorityComparer : IComparer<TaskEntry>
        {
            public int Compare(TaskEntry? x, TaskEntry? y) => x!.Priority.CompareTo(y!.Priority);
        }
        private PriorityQueue<TaskEntry, int> tasks;

        public TaskManager()
        {
            tasks = new PriorityQueue<TaskEntry, int>();
        }

        public float TimeQuant { get; set; } = 10;
        public INotifyCollectionChanged Tasks => tasks;
        public TaskEntry? NextTask { get; set; }
        public void Iterate()
        {
            if (tasks.Count > 0)
            {
                UpdateNext();
                var t = NextTask;
                if (NextTask==null) {
                    UpdateNext();
                    return;
                }
                t.TimeLeft -= TimeQuant;
                if (t!.TimeLeft <= 0)
                {
                    tasks.Dequeue();
                }
            }
            UpdateNext();
        }

        internal void UpdateNext()
        {
            var e = tasks.GetEnumerator();
            if (e.MoveNext())
                NextTask = e.Current;
            else
                NextTask = null;
        }
        internal void RemoveTask()
        {
            tasks.Dequeue();
            UpdateNext();
        }

        internal int TaskCount()
        {
            return tasks.Count();
        }

        internal void Clear()
        {
            while (tasks.Count > 0)
                tasks.Dequeue();
            UpdateNext();
        }
        public void ChangePriority(TaskEntry taskEntry, int priority)
        {
            tasks.ChangePriority(taskEntry, taskEntry.Priority, priority);
            taskEntry.Priority = priority;

            OnCollectionChanged();
            UpdateNext();
        }
        internal void Add(TaskEntry taskEntry)
        {
            tasks.Enqueue(taskEntry, taskEntry.Priority);
            UpdateNext();
        }

        internal void OnCollectionChanged()
        {
            UpdateNext();
            tasks.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }

}
