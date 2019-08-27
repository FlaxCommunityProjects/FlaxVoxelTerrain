using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FlaxEngine;
using FlaxEngine.GUI;
using FlaxEngine.Rendering;
using FlaxEngine.Utilities;
using Debug = FlaxEngine.Debug;

namespace VoxelTerrain.Source
{
    public class VoxelRayCastHit
    {
        public Int3 Position;
        public Block Block;
        // TODO: Switch for enum?
        public Int3 FaceNormal;
    }

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
        public const int ChunkRadius = 15;
        public const int ChunkUnloadOffset = 2;
        public const int LoaderThreadCount = 4;
        public const int UnloaderThreadCount = 4;
        public const int UpdateThreadCount = 4;

        // UI
        public UIControl DebugTextLabel;

        private readonly ConcurrentDictionary<Int2, Chunk> _chunks = new ConcurrentDictionary<Int2, Chunk>();

        public ConcurrentDictionary<Int2, Chunk> Chunks => _chunks;

        // Load queue
        private ChunkComparer _comparer;
        private readonly List<Chunk> _loadQueue = new List<Chunk>();
        private readonly object _loadQueueLocker = new object();
        private readonly Thread[] _loadWorkers = new Thread[LoaderThreadCount];

        // Unload queue
        /*private readonly Queue<Chunk> _unloadQueue = new Queue<Chunk>();
        private readonly object _unloadQueueLocker = new object();
        private readonly Thread[] _unloadWorkers = new Thread[UnloaderThreadCount];*/

        // UpdateSegment queue
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

            /*for (var i = 0; i < UnloaderThreadCount; i++)
                _unloadWorkers[i] = new Thread(UnloadWork) { IsBackground = true, Priority = ThreadPriority.Lowest };*/

            for (var i = 0; i < UpdateThreadCount; i++)
                _updateWorkers[i] = new Thread(UpdateWork) { IsBackground = true, Priority = ThreadPriority.Highest };

            for (var i = 0; i < LoaderThreadCount; i++)
                _loadWorkers[i].Start();

            /*for (var i = 0; i < UnloaderThreadCount; i++)
                _unloadWorkers[i].Start();*/

            for (var i = 0; i < UpdateThreadCount; i++)
                _updateWorkers[i].Start();
        }
        Stopwatch sw1 = Stopwatch.StartNew();
        private int ups = 0;
        private int lastUPS = 0;
        private int lastFUPS = 0;
        private int fups = 0;
        public override void OnUpdate()
        {
            ups++;
            if (sw1.ElapsedMilliseconds >= 1000)
            {
                sw1.Restart();
                lastUPS = ups;
                ups = 0;
                lastFUPS = fups;
                fups = 0;
            }
        }


        public override void OnFixedUpdate()
        {
            fups++;
            var camPos = Camera.MainCamera.Position;
            var intCamPos = new Int2((int) camPos.X, (int) camPos.Z);
            var currentChunk = intCamPos / Chunk.BLOCK_SIZE_CM / Chunk.SEGMENT_SIZE;

            // Fast way to sync chunk pos with negative numbers
            if (intCamPos.X < 0) currentChunk.X--;
            if (intCamPos.Y < 0) currentChunk.Y--;

            StringBuilder debugText = new StringBuilder();
            debugText.AppendLine("View mode: " + MainRenderTask.Instance.View.Mode);
            debugText.AppendLine($"Camera position (real):{camPos / Chunk.BLOCK_SIZE_CM} ({camPos})");
            debugText.AppendLine($"Chunk: X:{currentChunk.X} Z:{currentChunk.Y}");
            debugText.AppendLine($"FPS: {Time.FramesPerSecond}");
            debugText.AppendLine($"UPS (target): {lastUPS} ({Time.UpdateFPS})");
            debugText.AppendLine($"PUPS: {lastFUPS} ({Time.PhysicsFPS})");
            debugText.AppendLine("QUEUE STATS:");

            lock (_loadQueueLocker)
                debugText.AppendLine("    LOAD: " + _loadQueue.Count);

            /* lock (_unloadQueueLocker)
                 debugText.AppendLine("    UNLOAD: " + _unloadQueue.Count);*/

            lock (_updateQueueLocker)
                debugText.AppendLine("    UPDATE: " + _updateQueue.Count);


            if (DebugTextLabel != null && DebugTextLabel.Control is Label l2)
                l2.Text = debugText.ToString();

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
            ).Select(c => c.Value).ToArray();

            if (toRemove.Length == 0) return;

            foreach (var chunk in toRemove)
            {
                chunk.IsQueuedForUnload = true;
                Destroy(chunk.Actor);
            }

            /* lock (_unloadQueueLocker)
                 _unloadQueue.EnqueueRange(toRemove);*/
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


        public void UpdateChunk(Chunk chunk) => UpdateChunk(chunk.ChunkPosition);

        public void UpdateChunk(Int2 pos)
        {
            lock (_updateQueueLocker)
                if(!_updateQueue.Contains(pos))
                    _updateQueue.Add(pos);
        }

        public VoxelRayCastHit RayCast(Vector3 start, Vector3 end, bool isPreScaled = false)
        {
            /*if (!isPreScaled)
            {
                start *= Chunk.BLOCK_SIZE_CM;
                end *= Chunk.BLOCK_SIZE_CM;
            }*/
            var d = end - start;
            var len = d.Length;
            d /= len;

            var t = 0.0f;

            var ix = Mathf.FloorToInt(start.X);
            var iy = Mathf.FloorToInt(start.Y);
            var iz = Mathf.FloorToInt(start.Z);

            var stepX = d.X > 0 ? 1 : -1;
            var stepY = d.Y > 0 ? 1 : -1;
            var stepZ = d.Z > 0 ? 1 : -1;

            var txDelta = Mathf.Abs(1f / d.X);
            var tyDelta = Mathf.Abs(1f / d.Y);
            var tzDelta = Mathf.Abs(1f / d.Z);

            var xDist = (stepX > 0) ? (ix + 1 - start.X) : (start.X - ix);
            var yDist = (stepY > 0) ? (iy + 1 - start.Y) : (start.Y - iy);
            var zDist = (stepZ > 0) ? (iz + 1 - start.Z) : (start.Z - iz);

            var txMax = (txDelta < float.PositiveInfinity) ? txDelta * xDist : float.PositiveInfinity;
            var tyMax = (tyDelta < float.PositiveInfinity) ? tyDelta * yDist : float.PositiveInfinity;
            var tzMax = (tzDelta < float.PositiveInfinity) ? tzDelta * zDist : float.PositiveInfinity;

            var steppedIndex = -1;

            while (t <= len)
            {
                var block = GetBlock(ix, iy, iz);
                if (block != null && block.ID > 0)
                {
                    return new VoxelRayCastHit()
                    {
                        Block = block,
                        Position = new Int3(ix,iy,iz),
                        FaceNormal = new Int3(steppedIndex == 0 ? -stepX: 0, steppedIndex == 1 ? -stepY: 0, steppedIndex == 2 ? -stepZ : 0)
                    };
                }

                if (txMax < tyMax)
                {
                    if (txMax < tzMax)
                    {
                        ix += stepX;
                        t = txMax;
                        txMax += txDelta;
                        steppedIndex = 0;
                    }
                    else
                    {
                        iz += stepZ;
                        t = tzMax;
                        tzMax += tzDelta;
                        steppedIndex = 2;
                    }
                }
                else
                {
                    if (tyMax < tzMax)
                    {
                        iy += stepY;
                        t = tyMax;
                        tyMax += tyDelta;
                        steppedIndex = 1;
                    }
                    else
                    {
                        iz += stepZ;
                        t = tzMax;
                        tzMax += tzDelta;
                        steppedIndex = 2;
                    }
                }
            }
            return null;
        }

        public Block GetBlock(int x, int y, int z)
        {
            var offsetX = x >= 0 ? 0 : Chunk.SEGMENT_SIZE;
            var offsetZ = z >= 0 ? 0 : Chunk.SEGMENT_SIZE;

            var chunkPos = new Int2((x - offsetX) / Chunk.SEGMENT_SIZE, (z - offsetZ) / Chunk.SEGMENT_SIZE);
            return Chunks.TryGetValue(chunkPos, out var chunk) ? chunk.GetBlockRelative(offsetX + x % Chunk.SEGMENT_SIZE, y, offsetZ + z % Chunk.SEGMENT_SIZE) : null;
        }

        public void SetBlock(int x, int y, int z, Block block)
        {
            var offsetX = x >= 0 ? 0 : Chunk.SEGMENT_SIZE;
            var offsetZ = z >= 0 ? 0 : Chunk.SEGMENT_SIZE;

            var chunkPos = new Int2((x - offsetX) / Chunk.SEGMENT_SIZE, (z - offsetZ) / Chunk.SEGMENT_SIZE);

            if (Chunks.TryGetValue(chunkPos, out var chunk))
            {
                chunk.SetBlockRelative(offsetX + x % Chunk.SEGMENT_SIZE, y, offsetZ + z % Chunk.SEGMENT_SIZE, block);
            }
        }

        public  void AddBlock(int x, int y, int z) => SetBlock(x,y,z, new Block {ID = 1, Transparent = false});
        public void RemoveBlock(int x, int y, int z) => SetBlock(x, y, z, null);

        public override void OnDestroy()
        {
            lock (_loadQueueLocker)
                _loadQueue.Clear();

            lock(_updateQueueLocker)
                _updateQueue.Clear();

            _workerExitFlag = true;
            for (var i = 0; i < LoaderThreadCount; i++)
                _loadWorkers[i].Abort();

            /*for (var i = 0; i < UnloaderThreadCount; i++)
                _unloadWorkers[i].Join();*/

            for (var i = 0; i < UpdateThreadCount; i++)
                _updateWorkers[i].Abort();
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
                    UpdateChunk(chunk.ChunkPosition - Int2.UnitX);
                    UpdateChunk(chunk.ChunkPosition + Int2.UnitX);
                    UpdateChunk(chunk.ChunkPosition - Int2.UnitY);
                    UpdateChunk(chunk.ChunkPosition + Int2.UnitY);
                }

                Thread.Sleep(chunk == null ? 250 : 25);
            }
        }

        /*private void UnloadWork()
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
        }*/

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
                            //TODO: Sort and update nearest chunks and chunks with block change first...
                            //NOTE: Use separate comparer??? or extend existing one???
                            pos = _updateQueue[0];
                            _updateQueue.RemoveAt(0);
                        }
                    }

                    if (pos != null && _chunks.TryGetValue(pos.Value, out var chunk))
                    {
                        chunk.UpdateChunk();
                    }

                    Thread.Sleep(pos == null ? 250 : 25);
                }
                catch (ThreadAbortException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
                
                
        }
    }
}