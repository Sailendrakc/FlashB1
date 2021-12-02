using System;
using System.Collections.Generic;
using System.Text;

namespace FlashB1
{
    /// <summary>
    /// This class is used to pool different kinds of frequently used objects.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class BasePooling<T>
    {
        /// <summary>
        /// The queue that stores objects
        /// </summary>
        private readonly Queue<T> Pool;

        /// <summary>
        /// Object generation functions
        /// </summary>
        private readonly Func<T> ObjectGenerator;

        /// <summary>
        /// Total number of objects that can exist in memory.
        /// </summary>
        public int maxObjects;

        /// <summary>
        /// Total numer of objects in the Queue.
        /// </summary>
        public int count { get; private set; }

        /// <summary>
        /// Total number of objects that exists in memory
        /// </summary>
        private int totalCount;

        /// <summary>
        /// Locking object for concurrency within an instance of base pool.
        /// </summary>
        private readonly object lockobjec = new object();

        public BasePooling(int initNumberOfObjects, int maxNumberOfObjects, Func<T> generationFunction)
        {
            Pool = new Queue<T>();
            ObjectGenerator = generationFunction;
            for (int i = 0; i < initNumberOfObjects; i++)
            {
                Pool.Enqueue(ObjectGenerator());
            }

            totalCount = initNumberOfObjects;
            maxObjects = maxNumberOfObjects;
        }

        /// <summary>
        /// Call this function to get an object.
        /// </summary>
        /// <returns></returns>
        public T GetObjectFromPool()
        {
            if(count > 0)
            {
                lock (lockobjec)
                {
                    count--;
                    return Pool.Dequeue();
                }
            }
            else
            {
                if(totalCount == maxObjects)
                {
                    //maybe just freeze until next object is available?
                    throw new Exception(" The max number of pooled object reached in " + nameof(T));
                }
                totalCount++;
                return ObjectGenerator();
            }
        }

        /// <summary>
        /// This function returns object to its pool.
        /// </summary>
        /// <param name="ObjectToReturn">The object to return</param>
        public void SetObjectIntoPool(T ObjectToReturn)
        {
            lock (lockobjec)
            {
                if (count < maxObjects)
                {
                    Pool.Enqueue(ObjectToReturn);
                    count++;
                }
            }
        }
    }
}
