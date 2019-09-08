using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FlaxEngine;

namespace FlaxVoxel
{
    public partial class VoxelWorld
    {
        // TODO: Sorting??
        public readonly ConcurrentQueue<UpdateEntry> UpdateQueue = new ConcurrentQueue<UpdateEntry>();
        private Thread[] UpdateThreads = new Thread[Configuration.ChunkUpdateWorkers];
        private volatile bool Cancel = false;
        private void StartQueues()
        {
            for (var i = 0; i < Configuration.ChunkUpdateWorkers; i++)
                UpdateThreads[i] = new Thread(UpdateWorker);

            for (var i = 0; i < Configuration.ChunkUpdateWorkers; i++)
                UpdateThreads[i].Start();
        }

        private void StopQueues()
        {
            Cancel = true;
            for (var i = 0; i < Configuration.ChunkUpdateWorkers; i++)
                UpdateThreads[i].Join();
        }

        private void UpdateWorker()
        {
            while (!Cancel)
            {
                if (UpdateQueue.TryDequeue(out var entry))
                {
                    Profiler.BeginEvent("Chunk update");
                    
                    entry.PerformUpdate(this); // TODO: Probably pass world instance in start since this might get moved to separate file and is not that intuitive

                    Profiler.EndEvent();

                    Thread.Sleep(25);
                    return;
                }

                Thread.Sleep(250);
            }
        }
    }
}
