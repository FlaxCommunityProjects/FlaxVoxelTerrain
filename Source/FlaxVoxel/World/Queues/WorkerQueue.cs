using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FlaxEngine;

namespace FlaxVoxel
{
    public class WorkerQueue<T> where T: IWorkerQueueEntry
    {
        private readonly ConcurrentQueue<T> _updateQueue = new ConcurrentQueue<T>();
        private Thread[] _updateThreads;
        private volatile bool _exitFlag = false;
        private VoxelWorld _world;
        public WorkerQueue(uint workerCount, VoxelWorld world)
        {
            _world = world;
            _updateThreads = new Thread[workerCount];
            for (var i = 0; i < workerCount; i++)
                _updateThreads[i] = new Thread(work);
        }

        public void Start()
        {
            for (var i = 0; i < _updateThreads.Length; i++)
                _updateThreads[i].Start();
        }

        public void Stop()
        {
            _exitFlag = true;
            for (var i = 0; i < _updateThreads.Length; i++)
                _updateThreads[i].Join();
        }

        public void Enqueue(T entry) => _updateQueue.Enqueue(entry);
        public bool TryDequeue(out T entry) => _updateQueue.TryDequeue(out entry);

        private void work()
        {
            while (!_exitFlag)
            {
                if (_updateQueue.TryDequeue(out var entry))
                {
                   // Profiler.BeginEvent("Chunk update");

                    entry.PerformAction(_world);

                   // Profiler.EndEvent();

                    Thread.Sleep(25);
                    return;
                }

                Thread.Sleep(250);
            }
        }
    }
}
