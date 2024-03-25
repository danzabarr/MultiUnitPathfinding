using System.Collections;
using System.Collections.Generic;

	public class PriorityQueue<T> : System.Collections.IEnumerable
	{
		IComparer<T> comparer;

		List<T> heap = new List<T>();

		public PriorityQueue(IComparer<T> comparer)
		{
			this.comparer = comparer;
		}

		public PriorityQueue(IComparer<T> comparer, ICollection<T> collection)
		{
			this.comparer = comparer;
			foreach (T item in collection)
				Enqueue(item);
		}

		public bool Contains(T item)
		{
			return heap.Contains(item);
		}

		public void Enqueue(T item)
		{
			heap.Add(item);
			int ci = heap.Count - 1; // child index; position at end
			while (ci > 0)
			{
				int pi = (ci - 1) / 2; // parent index
				if (comparer.Compare(heap[ci], heap[pi]) >= 0) break; // child item is larger than (or equal) parent so we're done
				T tmp = heap[ci]; heap[ci] = heap[pi]; heap[pi] = tmp;
				ci = pi;
			}
		}
		public T Dequeue()
		{
			// assumes pq is not empty; up to calling code
			int li = heap.Count - 1; // last index (before removal)
			T frontItem = heap[0];   // fetch the front
			heap[0] = heap[li];
			heap.RemoveAt(li);

			--li; // last index (after removal)
			int pi = 0; // parent index. position at front of pq
			while (true)
			{
				int ci = pi * 2 + 1; // left child index of parent
				if (ci > li) break;  // no children so done
				int rc = ci + 1;     // right child index
				if (rc <= li && comparer.Compare(heap[rc], heap[ci]) < 0) // if there is a rc (ci + 1), and it is smaller than left child, use the rc instead
					ci = rc;
				if (comparer.Compare(heap[pi], heap[ci]) <= 0) break; // parent is smaller than (or equal to) smallest child so done
				T tmp = heap[pi]; heap[pi] = heap[ci]; heap[ci] = tmp; // swap parent and child
				pi = ci;
			}
			return frontItem;
		}

		public int Count => heap.Count;

		public bool IsEmpty => heap.Count == 0;

		public IEnumerator GetEnumerator()
		{
			return ((IEnumerable)heap).GetEnumerator();
		}
	}
