using System;
using System.Collections.Generic;

public class MinHeap<T>
{
    private List<Tuple<double, T>> heap;

    public MinHeap()
    {
        heap = new List<Tuple<double, T>>();
    }

    public int Size()
    {
        return heap.Count;
    }

    public bool IsEmpty()
    {
        return Size() == 0;
    }

    public void Add(double key, T value)
    {
        heap.Add(new Tuple<double, T>(key, value));
        HeapifyUp();
    }

    public Tuple<double, T> Peek()
    {
        if (IsEmpty())
        {
            throw new InvalidOperationException("Heap is empty");
        }
        return heap[0];
    }

    public Tuple<double, T> Poll()
    {
        if (IsEmpty())
        {
            throw new InvalidOperationException("Heap is empty");
        }

        Tuple<double, T> minValue = heap[0];
        heap[0] = heap[Size() - 1];
        heap.RemoveAt(Size() - 1);

        HeapifyDown();

        return minValue;
    }

    private void HeapifyUp()
    {
        int index = Size() - 1;

        while (HasParent(index) && Parent(index).Item1 > heap[index].Item1)
        {
            Swap(GetParentIndex(index), index);
            index = GetParentIndex(index);
        }
    }

    private void HeapifyDown()
    {
        int index = 0;

        while (HasLeftChild(index))
        {
            int smallerChildIndex = GetLeftChildIndex(index);

            if (HasRightChild(index) && RightChild(index).Item1 < LeftChild(index).Item1)
            {
                smallerChildIndex = GetRightChildIndex(index);
            }

            if (heap[index].Item1 < heap[smallerChildIndex].Item1)
            {
                break;
            }
            else
            {
                Swap(index, smallerChildIndex);
            }

            index = smallerChildIndex;
        }
    }

    private bool HasParent(int index) => GetParentIndex(index) >= 0;

    private int GetParentIndex(int childIndex) => (childIndex - 1) / 2;

    private Tuple<double, T> Parent(int index) => heap[GetParentIndex(index)];

    private bool HasLeftChild(int index) => GetLeftChildIndex(index) < Size();

    private int GetLeftChildIndex(int parentIndex) => 2 * parentIndex + 1;

    private Tuple<double, T> LeftChild(int index) => heap[GetLeftChildIndex(index)];

    private bool HasRightChild(int index) => GetRightChildIndex(index) < Size();

    private int GetRightChildIndex(int parentIndex) => 2 * parentIndex + 2;

    private Tuple<double, T> RightChild(int index) => heap[GetRightChildIndex(index)];

    private void Swap(int indexOne, int indexTwo)
    {
        Tuple<double, T> temp = heap[indexOne];
        heap[indexOne] = heap[indexTwo];
        heap[indexTwo] = temp;
    }
}