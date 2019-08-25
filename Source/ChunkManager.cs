using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FlaxEngine;
using FlaxEngine.GUI;
using FlaxEngine.Utilities;

namespace VoxelTerrain.Source
{
    public class ChunkComparer : IComparer<Chunk>
    {
        public Camera TargetCamera;

        public int Compare(Chunk x, Chunk y)
        {
            if (x == null) return 1;
            if (y == null) return -1;

            var frustum = new BoundingFrustum(TargetCamera.Projection * TargetCamera.View);

            var xContains = frustum.Contains(x.Bounds) != ContainmentType.Disjoint;
            var yContains = frustum.Contains(y.Bounds) != ContainmentType.Disjoint;

            var contains = yContains.CompareTo(xContains);
            if (contains != 0) return contains;

            var cameraPosition = TargetCamera.Position;

            var xDist = Vector3.Distance(x.Actor.Position, cameraPosition);
            var yDist = Vector3.Distance(y.Actor.Position, cameraPosition);

            return xDist.CompareTo(yDist);
        }
    }

    public class ChunkManager : Script
    {
        // Constants
        public const int ChunkRadius = 10;
        public const int ChunkUnloadOffset = 2;
        public const int LoaderThreadCount = 4;
        public const int UnloaderThreadCount = 4;
        public const int UpdateThreadCount = 4;

        // UI
        public UIControl CamPosLabel;
        public UIControl ChunkPosLabel;

        private readonly ConcurrentDictionary<Int2, Chunk> _chunks = new ConcurrentDictionary<Int2, Chunk>();

        public ConcurrentDictionary<Int2, Chunk> Chunks => _chunks;

        // Load queue
        private ChunkComparer _comparer;
        private readonly List<Chunk> _loadQueue = new List<Chunk>();
        private readonly object _loadQueueLocker = new object();
        private readonly Thread[] _loadWorkers = new Thread[LoaderThreadCount];

        // Unload queue
        private readonly Queue<Chunk> _unloadQueue = new Queue<Chunk>();
        private readonly object _unloadQueueLocker = new object();
        private readonly Thread[] _unloadWorkers = new Thread[UnloaderThreadCount];

        // Update queue
        private readonly List<Int2> _updateQueue = new List<Int2>();
        private readonly object _updateQueueLocker = new object();
        private readonly Thread[] _updateWorkers = new Thread[UpdateThreadCount];

        // Load + Unload workers
        private volatile bool _workerExitFlag;

        public override void OnStart()
        {
            Actor.Scale = new Vector3(Chunk.BLOCK_SIZE_CM);

            _comparer = new ChunkComparer {TargetCamera = Camera.MainCamera};

            for (var i = 0; i < LoaderThreadCount; i++)
                _loadWorkers[i] = new Thread(LoadWork) {IsBackground = true, Priority = ThreadPriority.Normal};

            for (var i = 0; i < UnloaderThreadCount; i++)
                _unloadWorkers[i] = new Thread(UnloadWork) { IsBackground = true, Priority = ThreadPriority.Lowest };

            for (var i = 0; i < UpdateThreadCount; i++)
                _updateWorkers[i] = new Thread(UpdateWork) { IsBackground = true, Priority = ThreadPriority.Highest };

            for (var i = 0; i < LoaderThreadCount; i++)
                _loadWorkers[i].Start();

            for (var i = 0; i < UnloaderThreadCount; i++)
                _unloadWorkers[i].Start();

            for (var i = 0; i < UpdateThreadCount; i++)
                _updateWorkers[i].Start();
        }

        public override void OnFixedUpdate()
        {
            var camPos = Camera.MainCamera.Position;
            var intCamPos = new Int2((int) camPos.X, (int) camPos.Z);
            var currentChunk = intCamPos / Chunk.BLOCK_SIZE_CM / Chunk.SEGMENT_SIZE;

            // Fast way to sync chunk pos with negative numbers
            if (intCamPos.X < 0) currentChunk.X--;
            if (intCamPos.Y < 0) currentChunk.Y--;

            if (ChunkPosLabel != null && ChunkPosLabel.Control is Label l)
                l.Text = $"Chunk: X:{currentChunk.X} Z:{currentChunk.Y}";

            if (CamPosLabel != null && CamPosLabel.Control is Label l2)
                l2.Text = "Camera position: " + camPos;

            // Spawn chunks to fill the grid
            for (var x = -ChunkRadius; x <= ChunkRadius; x++)
            {
                for (var z = -ChunkRadius; z <= ChunkRadius; z++)
                {
                    if (x * x + z * z > ChunkRadius * ChunkRadius) continue;
                    var chunkX = x + currentChunk.X;
                    var chunkZ = z + currentChunk.Y;
                    var chunkPos = new Int2(chunkX, chunkZ);

                    if (!_chunks.TryGetValue(chunkPos, out _))
                        SpawnChunk(chunkPos);
                }
            }


            // Remove chunks outside of view

            var toRemove = _chunks.AsParallel().Where(c =>
                Int2.Distance(c.Key, currentChunk) > ChunkRadius + ChunkUnloadOffset && 
                !c.Value.IsQueuedForUnload &&
                !c.Value.IsQueuedForLoad
            ).Select(c=>c.Value).ToArray();

            if (toRemove.Length == 0) return;

            foreach (var chunk in toRemove)
                chunk.IsQueuedForUnload = true;

            lock (_unloadQueueLocker)
                _unloadQueue.EnqueueRange(toRemove);
        }

        private void SpawnChunk(Int2 pos)
        {
            var actor = Actor.AddChild<EmptyActor>();
            var chunk = actor.AddScript<Chunk>();
            actor.LocalPosition = new Vector3(pos.X * Chunk.SEGMENT_SIZE, 0, pos.Y * Chunk.SEGMENT_SIZE);
            chunk.ChunkPosition = pos;
            chunk.Initialize(this);

            if(!_chunks.TryAdd(pos, chunk))
                Debug.LogError("Failed to spawn chunk");

            chunk.IsQueuedForLoad = true;
            lock (_loadQueueLocker)
                _loadQueue.Add(chunk);
        }

        public override void OnDestroy()
        {
            _workerExitFlag = true;
            for (var i = 0; i < LoaderThreadCount; i++)
                _loadWorkers[i].Join();

            for (var i = 0; i < UnloaderThreadCount; i++)
                _unloadWorkers[i].Join();

            for (var i = 0; i < UpdateThreadCount; i++)
                _updateWorkers[i].Join();
        }

        private void LoadWork()
        {
            while (!_workerExitFlag)
            {
                Chunk chunk = null;
                lock (_loadQueueLocker)
                {
                    if (_loadQueue.Count > 0)
                    {
                        _loadQueue.Sort(_comparer);
                        chunk = _loadQueue[0];
                        _loadQueue.RemoveAt(0);
                    }
                }

                if (chunk != null)
                {
                    chunk.IsLoading = true;
                    chunk.GenerateChunk();
                    chunk.IsLoading = false;
                    chunk.IsLoaded = true;
                    chunk.IsQueuedForLoad = false;

                    // Enqueue mesh rebuild so we remove invisible faces
                    lock (_updateQueueLocker)
                    {
                        _updateQueue.Add(chunk.ChunkPosition - Int2.UnitX);
                        _updateQueue.Add(chunk.ChunkPosition + Int2.UnitX);
                        _updateQueue.Add(chunk.ChunkPosition - Int2.UnitY);
                        _updateQueue.Add(chunk.ChunkPosition + Int2.UnitY);
                    }
                }

                Thread.Sleep(chunk == null ? 250 : 25);
            }
        }

        private void UnloadWork()
        {
            while (!_workerExitFlag)
                try
                {
                    Chunk chunk = null;
                    lock (_unloadQueueLocker)
                    {
                        if (_unloadQueue.Count > 0)
                            chunk = _unloadQueue.Dequeue();
                    }

                    if (chunk)
                    {
                        chunk.IsUnloading = true;
                        if (_chunks.TryRemove(chunk.ChunkPosition, out _))
                        {
                            // Enqueue mesh rebuild so we add invisible faces
                            lock (_updateQueueLocker)
                            {
                                _updateQueue.Add(chunk.ChunkPosition - Int2.UnitX);
                                _updateQueue.Add(chunk.ChunkPosition + Int2.UnitX);
                                _updateQueue.Add(chunk.ChunkPosition - Int2.UnitY);
                                _updateQueue.Add(chunk.ChunkPosition + Int2.UnitY);
                            }
                        }

                        Destroy(chunk.Actor);
                    }

                    Thread.Sleep(chunk == null ? 250 : 25);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    Thread.Sleep(1000);
                }
        }

        private void UpdateWork()
        {
            while(!_workerExitFlag)
                try
                {
                    Int2? pos = null;
                    lock (_updateQueueLocker)
                    {
                        if (_updateQueue.Count > 0)
                        {
                            pos = _updateQueue[0];
                            _updateQueue.RemoveAt(0);
                        }
                    }

                    if (pos!= null && _chunks.TryGetValue(pos.Value, out var chunk))
                    {
                        chunk.UpdateChunk();
                    }

                    Thread.Sleep(pos == null ? 250 : 25);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
        }
    }
}