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
        [NoSerialize] public WorkerQueue<GenerateChunk> GeneratorQueue;
        [NoSerialize] public WorkerQueue<IWorkerQueueEntry> UpdateQueue;
        private void InitializeQueues()
        {
            GeneratorQueue = new WorkerQueue<GenerateChunk>(8, this);
            UpdateQueue = new WorkerQueue<IWorkerQueueEntry>(8, this);
        }

        private void StartQueues()
        {
            GeneratorQueue.Start();
            UpdateQueue.Start();
        }

        private void StopQueues()
        {
            UpdateQueue.Stop();
            GeneratorQueue.Stop();
        }
    }
}
