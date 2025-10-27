using System;
using UnityEngine;

namespace Code.Networking.ClientPrediction
{
    [Serializable]
    public class CircularBuffer<T>
    {
        [SerializeField] T[] buffer;
        int bufferSize;
        
        public CircularBuffer(int bufferSize) 
        {
            this.bufferSize = bufferSize;
            buffer = new T[bufferSize];
        }
        
        public void Add(T item, int index) => buffer[index % bufferSize] = item;
        public T Get(int index) => buffer[index % bufferSize];
        public void Clear() => buffer = new T[bufferSize];
    }
}