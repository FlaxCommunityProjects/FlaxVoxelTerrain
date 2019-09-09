using System;
using System.Collections.Generic;
using FlaxEngine;
using FlaxEngine.Rendering;
using FlaxVoxel;
using FlaxVoxel.TerraGenesis;

//using VoxelTerrain.Source;

namespace BasicTemplate
{
    public class FreeCamera : Script
    {
        [Limit(0, 100), Tooltip("Camera movement speed factor")]
        public float MoveSpeed { get; set; } = 1;

        [Tooltip("Camera rotation smoothing factor")]
        public float CameraSmoothing { get; set; } = 20.0f;

        private float pitch;
        private float yaw;

        /*public ChunkManager VoxelWorld;
        public Actor SelectedCubeActor;*/

        public VoxelWorld World;

        private int ci = -4;
        public override void OnUpdate()
        {
           /* Screen.CursorVisible = false;
            Screen.CursorLock = CursorLockMode.Locked;*/

           /* Vector2 mouseDelta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
            pitch = Mathf.Clamp(pitch + mouseDelta.Y, -88, 88);
            yaw += mouseDelta.X;
            */
            /*
                        if (Input.GetKeyDown(Keys.E))
                            MainRenderTask.Instance.View.Mode = ViewMode.Default;
                        if (Input.GetKeyDown(Keys.Q))
                            MainRenderTask.Instance.View.Mode = ViewMode.Wireframe;*/

            if (World !=  null && Input.GetKeyDown(Keys.Q))
            {
                for (var x = 0; x < 3; x++)
                    for (var z = 0; z < 3; z++)
                       //World.GeneratorQueue.Enqueue(new GenerateChunk(World.SpawnChunk(new Int2(x, z))));
                       World.GeneratorQueue.Enqueue(new GenerateChunk(new Int2(x,z)));
            }

        }

       /* public override void OnDebugDraw()
        {
            var start = Actor.Transform.Translation / Chunk.BLOCK_SIZE_CM;
            var end = start + Actor.Transform.Forward * 8;

            DebugDraw.DrawLine(start * Chunk.BLOCK_SIZE_CM, end * Chunk.BLOCK_SIZE_CM, Color.Blue);
        }*/

        /*DateTime lastBuildTime = DateTime.MinValue;
        DateTime lastDestroyTime = DateTime.MinValue;*/

        private VoxelChunk SpawnChunk(Int2 pos)
        {
            var chunkActor = World.Actor.AddChild<EmptyActor>();
            var chunk = chunkActor.AddScript<VoxelChunk>();
            chunk.WorldPosition = pos;
            chunkActor.LocalPosition = new Vector3(pos.X * VoxelWorld.Configuration.ChunkSegmentSize, 0, pos.Y * VoxelWorld.Configuration.ChunkSegmentSize);
            chunkActor.Name = $"Chunk[{chunk.WorldPosition.X},{chunk.WorldPosition.Y}]";
            chunk.World = World;
            World.Chunks.TryAdd(chunk.WorldPosition, chunk);
            return chunk;
        }
        public override void OnFixedUpdate()
        {
            var camTrans = Actor.Transform;
            var camFactor = Mathf.Saturate(CameraSmoothing * Time.DeltaTime);

            camTrans.Orientation = Quaternion.Lerp(camTrans.Orientation, Quaternion.Euler(pitch, yaw, 0), camFactor);

            var inputH = Input.GetAxis("Horizontal");
            var inputV = Input.GetAxis("Vertical");
            var move = new Vector3(inputH, 0.0f, inputV);
            move.Normalize();
            move = camTrans.TransformDirection(move);

            camTrans.Translation += move * MoveSpeed;

            Actor.Transform = camTrans;

           /* var start = Actor.Transform.Translation / Chunk.BLOCK_SIZE_CM;
            var end = start + Actor.Transform.Forward * 8;

            if (VoxelWorld == null || SelectedCubeActor == null) return;

            var hit = VoxelWorld.RayCast(start, end, true);

            SelectedCubeActor.IsActive = hit != null;

            if (hit == null) return;

            var tt = SelectedCubeActor.Transform;
            tt.Translation = new Vector3(hit.Position.X + 0.5f, hit.Position.Y + 0.5f, hit.Position.Z + 0.5f) * Chunk.BLOCK_SIZE_CM;
            SelectedCubeActor.Transform = tt;
            var now = DateTime.Now;

            if ((Input.GetMouseButton(MouseButton.Right) || Input.GetMouseButtonDown(MouseButton.Right)) && now - lastBuildTime > TimeSpan.FromMilliseconds(200))
            {
                VoxelWorld.AddBlock(hit.Position.X + hit.FaceNormal.X, hit.Position.Y + hit.FaceNormal.Y,
                    hit.Position.Z + hit.FaceNormal.Z);
                lastBuildTime = now;
            }

            if ((Input.GetMouseButton(MouseButton.Left) || Input.GetMouseButtonDown(MouseButton.Left)) && now - lastDestroyTime > TimeSpan.FromMilliseconds(200))
            {
                VoxelWorld.RemoveBlock(hit.Position.X, hit.Position.Y, hit.Position.Z);
                lastDestroyTime = now;
            }

            if(Input.GetMouseButtonUp(MouseButton.Left)) lastDestroyTime = DateTime.MinValue;
            if(Input.GetMouseButtonUp(MouseButton.Right)) lastBuildTime = DateTime.MinValue;*/
        }
    }
}