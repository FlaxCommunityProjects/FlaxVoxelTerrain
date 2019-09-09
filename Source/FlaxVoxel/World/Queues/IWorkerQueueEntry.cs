using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlaxVoxel
{
    public interface IWorkerQueueEntry
    {
        void PerformAction(VoxelWorld world);
    }
}
