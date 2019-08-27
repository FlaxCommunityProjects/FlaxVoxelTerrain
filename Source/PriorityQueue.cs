using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace System.Collections.Generic
{
    /// <summary>
    /// Implements a min priority queue (https://en.wikipedia.org/wiki/Priority_queue) using a binary heap
    /// </summary>
    [DebuggerTypeProxy(typeof(PriorityQueue<>.DebugView))]
    [DebuggerDisplay("Count = {Count}")]
    public sealed class PriorityQueue<T> : ICollection<T>, IReadOnlyCollection<T>
    {
        /// <summary>
        /// Re-usable empty array instance
        /// </summary>
        // Enumerable.Empty is currently implemented as a re-used empty array, so try to use that
        private static readonly T[] EmptyArray = Enumerable.Empty<T>() as T[] ?? new T[0];

        /// <summary>
        /// The binary heap. May contain excess space at the end
        /// </summary>
        private T[] heap;
        /// <summary>
        /// Incremented by mutating operations to allow the <see cref="IEnumerator{T}"/> to throw
        /// exceptions when the collection is modified concurrently with enumeration
        /// </summary>
        private int version;

        #region ---- Constructors ----
        /// <summary>
        /// Constructs an instance of <see cref="PriorityQueue{T}"/>, optionaly using a specified <see cref="IComparer{T}"/>
        /// </summary>
        public PriorityQueue(IComparer<T> comparer = null)
        {
            this.Comparer = comparer ?? Comparer<T>.Default;
            this.heap = EmptyArray;
        }

        /// <summary>
        /// Constructs an instance of <see cref="PriorityQueue{T}"/> specifying an initial internal capacity
        /// as well as an optional <see cref="IComparer{T}"/>
        /// </summary>
        public PriorityQueue(int initialCapacity, IComparer<T> comparer = null)
            : this(comparer)
        {
            if (initialCapacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), initialCapacity, "must be positive");
            }

            if (initialCapacity > 0)
            {
                this.heap = new T[initialCapacity];
            }
        }

        /// <summary>
        /// Constructs an instance of <see cref="PriorityQueue{T}"/> initially populated with the
        /// given <paramref name="items"/>. Optionally uses the given <see cref="IComparer{T}"/>
        /// </summary>
        public PriorityQueue(IEnumerable<T> items, IComparer<T> comparer = null)
            : this(comparer)
        {
            this.EnqueueRange(items);
        }
        #endregion

        #region ---- Priority Queue API ----
        /// <summary>
        /// The <see cref="IComparer{T}"/> instances which determines the sort order for the queue
        /// </summary>
        public IComparer<T> Comparer { get; }

        /// <summary>
        /// Adds <paramref name="item"/> to the <see cref="PriorityQueue{T}"/>. This operation is
        /// amortized O(log(N)) in complexity
        /// </summary>
        public void Enqueue(T item)
        {
            if (this.heap.Length == this.Count)
            {
                // note: we don't need checked here because Count can never be int.MaxValue (see Expand())
                this.Expand(this.Count + 1);
            }

            var lastIndex = this.Count;
            ++this.Count;
            this.Swim(lastIndex, item);
            unchecked { ++this.version; }
        }

        /// <summary>
        /// Removes and returns the minimum element of the <see cref="PriorityQueue{T}"/> as determined
        /// by its <see cref="Comparer"/>. This operation is O(log(N)) in complexity.
        /// 
        /// Throws <see cref="InvalidOperationException"/> if the queue is empty
        /// </summary>
        public T Dequeue()
        {
            if (this.Count == 0) { throw new InvalidOperationException("The priority queue is empty"); }

            var result = this.heap[0];
            --this.Count;
            if (this.Count > 0)
            {
                var last = this.heap[this.Count];
                // don't hold a reference
                this.heap[this.Count] = default(T);
                this.Sink(0, last);
            }
            else
            {
                // don't hold a reference
                this.heap[0] = default(T);
            }

            unchecked { ++this.version; }
            return result;
        }

        /// <summary>
        /// Removes, but does not return the minimum element of the <see cref="PriorityQueue{T}"/> as determined
        /// by its <see cref="Comparer"/>. This operation is O(1) in complexity.
        /// 
        /// Throws <see cref="InvalidOperationException"/> if the queue is empty
        /// </summary>
        public T Peek()
        {
            if (this.Count == 0) { throw new InvalidOperationException("The priority queue is empty"); }

            return this.heap[0];
        }

        /// <summary>
        /// Adds all of the given <paramref name="items"/> to the <see cref="PriorityQueue{T}"/>. This operation
        /// may run faster than repeated calls to <see cref="Enqueue(T)"/>
        /// </summary>
        public void EnqueueRange(IEnumerable<T> items)
        {
            if (items == null) { throw new ArgumentNullException(nameof(items)); }

            var initialCount = this.Count;
            this.AppendItems(items);

            if (this.Count == initialCount)
            {
                return; // no version bump
            }

            if (this.Count - initialCount > initialCount)
            {
                // Heapify() only operates on half the heap, so if we more than doubled the size
                // then we should just heapify rather than swimming each new item
                this.Heapify();
            }
            else
            {
                // otherwise, just swim each item as if we had called Enqueue() multiple times
                for (var i = initialCount; i < this.Count; ++i)
                {
                    this.Swim(i, this.heap[i]);
                }
            }

            unchecked { ++this.version; }
        }
        #endregion

        #region ---- Heap Maintenance Methods ----
        /// <summary>
        /// Expands the queue's capacity to store at least <paramref name="requiredCapacity"/> items in total
        /// </summary>
        private void Expand(int requiredCapacity)
        {
            // based on the internal Array.MaxArrayLength constant used by List<T>
            // https://referencesource.microsoft.com/#mscorlib/system/array.cs,2d2b551eabe74985,references
            const int MaxArrayLength = 0x7FEFFFFF;
            const int InitialCapacity = 16;

            if (requiredCapacity > MaxArrayLength)
            {
                throw new InvalidOperationException("Queue cannot be further expanded");
            }

            var currentCapacity = this.heap.Length;
            var nextNaturalGrowthCapacity = unchecked(
                // small heaps grow faster than large ones
                // inspired by the java implementation http://grepcode.com/file/repository.grepcode.com/java/root/jdk/openjdk/6-b14/java/util/PriorityQueue.java#243
                currentCapacity < InitialCapacity ? InitialCapacity
                    : currentCapacity < 64 ? 2 * (currentCapacity + 1)
                    : 3 * (currentCapacity / 2)
            );

            // if we overflow, expand to max length. Otherwise use the larger of required and next natural
            var newCapacity = nextNaturalGrowthCapacity < 0 ? MaxArrayLength
                : nextNaturalGrowthCapacity < requiredCapacity ? requiredCapacity
                : nextNaturalGrowthCapacity;

            Array.Resize(ref this.heap, newCapacity);
        }

        /// <summary>
        /// Performs the heap "sink" operation starting at <paramref name="index"/>. <paramref name="item"/>
        /// will be placed in its determined final position
        /// </summary>
        private void Sink(int index, T item)
        {
            var half = this.Count >> 1;
            var i = index;
            while (i < half)
            {
                // pick the lesser of the left and right children
                var childIndex = (i << 1) + 1;
                var childItem = this.heap[childIndex];
                var rightChildIndex = childIndex + 1;
                if (rightChildIndex < this.Count && this.Comparer.Compare(childItem, this.heap[rightChildIndex]) > 0)
                {
                    childItem = this.heap[childIndex = rightChildIndex];
                }

                // if item is <= either child, stop
                if (this.Comparer.Compare(item, childItem) <= 0)
                {
                    break;
                }

                // move smaller child up and move our pointer down the heap
                this.heap[i] = childItem;
                i = childIndex;
            }

            // finally, put the initial item in the final position
            this.heap[i] = item;
        }

        /// <summary>
        /// Performs the heap "swim" operation starting at <paramref name="index"/>. <paramref name="item"/>
        /// will be placed in its determined final position
        /// </summary>
        private void Swim(int index, T item)
        {
            var i = index;
            while (i > 0)
            {
                // find the parent index/item
                var parentIndex = (i - 1) >> 1;
                var parentItem = this.heap[parentIndex];

                // if the parent is <= item, we're done
                if (this.Comparer.Compare(item, parentItem) >= 0)
                {
                    break;
                }

                // shift the parent down and traverse our pointer up
                this.heap[i] = parentItem;
                i = parentIndex;
            }

            // finally, leave item at the final position
            this.heap[i] = item;
        }

        /// <summary>
        /// Builds a binary heap from an unstructured array
        /// </summary>
        private void Heapify()
        {
            // heapify works by starting at the end and sinking each element until we reach the front. Because only
            // elements in the front half of the heap have children, we can skip half the work by starting from the middle
            for (var i = (this.Count >> 1) - 1; i >= 0; --i)
            {
                this.Sink(i, this.heap[i]);
            }
        }

        /// <summary>
        /// Appends the given <paramref name="items"/> to the heap without regard for maintaining
        /// heap order
        /// </summary>
        private void AppendItems(IEnumerable<T> items)
        {
            if (items is ICollection<T> itemsCollection)
            {
                var itemsCollectionCount = itemsCollection.Count;
                if (itemsCollectionCount > 0)
                {
                    var newCount = checked(this.Count + itemsCollectionCount);
                    if (newCount > this.heap.Length)
                    {
                        this.Expand(newCount);
                    }
                    itemsCollection.CopyTo(this.heap, arrayIndex: this.Count);
                    this.Count = newCount;
                }
                return;
            }

            if (items is IReadOnlyCollection<T> readOnlyItemsCollection)
            {
                var readOnlyItemsCollectionCount = readOnlyItemsCollection.Count;
                if (readOnlyItemsCollectionCount > 0)
                {
                    var newCount = checked(this.Count + readOnlyItemsCollectionCount);
                    if (newCount > this.heap.Length)
                    {
                        this.Expand(newCount);
                    }

                    foreach (var item in readOnlyItemsCollection)
                    {
                        this.heap[this.Count++] = item;
                    }
                }
                return;
            }

            foreach (var item in items)
            {
                // note: we don't need checked here because Count can never be as high as int.MaxValue (see Expand)
                if (this.Count == this.heap.Length)
                {
                    this.Expand(this.Count + 1);
                }
                this.heap[this.Count++] = item;
            }
        }
        #endregion

        #region ---- Interface Members ----
        /// <summary>
        /// The number of items in the <see cref="PriorityQueue{T}"/>
        /// </summary>
        public int Count { get; private set; }

        bool ICollection<T>.IsReadOnly => false;

        /// <summary>
        /// Equivalent to <see cref="Enqueue(T)"/>. This method is exposed to allow <see cref="PriorityQueue{T}"/>
        /// to be used with collection initializer syntax
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)] // encourage use of Enqueue
        public void Add(T item) => this.Enqueue(item);

        /// <summary>
        /// Removes all items from the <see cref="PriorityQueue{T}"/>
        /// </summary>
        public void Clear()
        {
            const int MaxRetainedCapacity = 1024;

            if (this.heap.Length > MaxRetainedCapacity)
            {
                this.heap = EmptyArray;
            }
            else
            {
                Array.Clear(this.heap, index: 0, length: this.Count);
            }

            this.Count = 0;
            unchecked { ++this.version; }
        }

        /// <summary>
        /// Returns true if an only if the <see cref="PriorityQueue{T}"/> contains
        /// the given <paramref name="item"/>. <see cref="Comparer"/> is used to determine
        /// item equality
        /// </summary>
        public bool Contains(T item) => this.Find(item).HasValue;

        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            // performs any bounds checks
            Array.Copy(sourceArray: this.heap, sourceIndex: 0, destinationArray: array, destinationIndex: arrayIndex, length: this.Count);
        }

        /// <summary>
        /// Returns an <see cref="IEnumerator{T}"/> over the items in the <see cref="PriorityQueue{T}"/>.
        /// Note that the items are not enumerated over in the order that they would be returned by <see cref="Dequeue"/>
        /// </summary>
        public IEnumerator<T> GetEnumerator()
        {
            var version = this.version;

            for (var i = 0; i < this.Count; ++i)
            {
                if (this.version != version) { throw new InvalidOperationException("Collection was modified; enumeration operation may not execute."); }
                yield return this.heap[i];
            }
        }

        /// <summary>
        /// Removes up to one instance of <paramref name="item"/> from the <see cref="PriorityQueue{T}"/> if
        /// it exists, using <see cref="Comparer"/> to determine equality. Returns true only if the queue was modified
        /// </summary>
        public bool Remove(T item)
        {
            var itemIndex = this.Find(item);

            if (!itemIndex.HasValue) return false;
            var indexToRemove = itemIndex.Value;
            --this.Count;
            var lastItem = this.heap[this.Count];
            this.heap[this.Count] = default(T);
            if (indexToRemove != this.Count)
            {
                this.Sink(indexToRemove, lastItem);
            }

            unchecked { ++this.version; }
            return true;

        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
        #endregion

        /// <summary>
        /// Searches for <paramref name="item"/> in the <see cref="PriorityQueue{T}"/>. Returns either
        /// an index where <paramref name="item"/> resides or null if no such index was found
        /// </summary>
        private int? Find(T item) => this.Count > 0 ? this.FindHelper(item, startIndex: 0) : null;

        private int? FindHelper(T item, int startIndex)
        {
            var cmp = this.Comparer.Compare(item, this.heap[startIndex]);
            if (cmp == 0)
            {
                // found it!
                return startIndex;
            }
            if (cmp < 0)
            {
                // if item < root of sub-heap, it can't appear in the sub-heap
                return null;
            }

            var leftChildIndex = (startIndex << 1) + 1;
            return leftChildIndex >= this.Count || leftChildIndex < 0
                // return null if left child index oveflowed or was past the end of the heap
                ? null
                : (
                    // else search the left subtree
                    this.FindHelper(item, leftChildIndex)
                    // if that returns null, search the right subtree unless left child index was the end of the heap
                    // in which case right child index would be past the end
                    ?? (leftChildIndex == this.Count - 1 ? null : this.FindHelper(item, leftChildIndex + 1))
                 );
        }

        /// <summary>
        /// Provides a cleaner view of the <see cref="PriorityQueue{T}"/> in the debugger
        /// </summary>
        private sealed class DebugView
        {
            private readonly ICollection<T> queue;

            public DebugView(PriorityQueue<T> queue)
            {
                this.queue = queue ?? throw new ArgumentNullException(nameof(queue));
            }

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public T[] Items
            {
                get
                {
                    // note: this can't simply use ToArray() since that gives an error in the debugger
                    var array = new T[this.queue.Count];
                    queue.CopyTo(array, arrayIndex: 0);
                    return array;
                }
            }
        }
    }
}
