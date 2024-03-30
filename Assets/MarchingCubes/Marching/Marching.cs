using System.Collections.Generic;
using Unity.Collections;
using WARP.Terraform.API;

namespace MarchingCubesProject
{
    public abstract class Marching
    {
       
        /// <summary>
        /// Winding order of triangles use 2,1,0 or 0,1,2
        /// </summary>
        protected int[] WindingOrder { get; private set; }

        public TerraformPoint[] InPoints;
        
        /// <summary>
        /// Constructor.
        /// </summary>
        public Marching()
        {
            WindingOrder = new[] { 0, 1, 2 };
            InPoints = new TerraformPoint[8];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="voxels"></param>
        /// <param name="verts"></param>
        /// <param name="outIndices"></param>
        public virtual void Generate(float surface, VoxelArray voxels, NativeList<TerraformPoint> outPoints, NativeList<int> outIndices)
        {
            int width = voxels.Width;
            int height = voxels.Height;
            int depth = voxels.Depth;

            UpdateWindingOrder(surface);

            int x, y, z, i;
            int ix, iy, iz;
            for (x = 0; x < width - 1; x++)
            {
                for (y = 0; y < height - 1; y++)
                {
                    for (z = 0; z < depth - 1; z++)
                    {
                        //Get the values in the 8 neighbours which make up a cube
                        for (i = 0; i < 8; i++)
                        {
                            ix = x + VertexOffset[i, 0];
                            iy = y + VertexOffset[i, 1];
                            iz = z + VertexOffset[i, 2];

                            InPoints[i] = voxels.Voxels[ix + iy * width + iz * width * height];
                        }

                        //Perform algorithm
                        March(surface, x, y, z, InPoints, outPoints, outIndices);
                    }
                }
            }

        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void Generate(float surface, NativeList<TerraformPoint> voxels, int width, int height, int depth, NativeList<TerraformPoint> outPoints, NativeList<int> outIndices)
        {
            UpdateWindingOrder(surface);

            int x, y, z, i;
            int ix, iy, iz;
            for (x = 0; x < width - 1; x++)
            {
                for (y = 0; y < height - 1; y++)
                {
                    for (z = 0; z < depth - 1; z++)
                    {
                        //Get the values in the 8 neighbours which make up a cube
                        for (i = 0; i < 8; i++)
                        {
                            ix = x + VertexOffset[i, 0];
                            iy = y + VertexOffset[i, 1];
                            iz = z + VertexOffset[i, 2];

                            InPoints[i] = voxels[ix + iy * width + iz * width * height];
                        }

                        //Perform algorithm
                        March(surface, x, y, z, InPoints, outPoints, outIndices);
                    }
                }
            }

        }

        /// <summary>
        /// Update the winding order. 
        /// This determines how the triangles in the mesh are orientated.
        /// </summary>
        protected virtual void UpdateWindingOrder(float surface)
        {
            if (surface > 0.0f)
            {
                WindingOrder[0] = 2;
                WindingOrder[1] = 1;
                WindingOrder[2] = 0;
            }
            else
            {
                WindingOrder[0] = 0;
                WindingOrder[1] = 1;
                WindingOrder[2] = 2;
            }
        }

         /// <summary>
        /// MarchCube performs the Marching algorithm on a single cube
        /// </summary>
        protected abstract void March(float surface, float x, float y, float z, TerraformPoint[] inPoints, NativeList<TerraformPoint> outPoints, NativeList<int> outIndices);

        /// <summary>
        /// GetOffset finds the approximate point of intersection of the surface
        /// between two points with the values v1 and v2
        /// </summary>
        public static float GetOffset(float surface, float v1, float v2)
        {
            float delta = v2 - v1;
            return (delta == 0.0f) ? surface : (surface - v1) / delta;
        }

        /// <summary>
        /// VertexOffset lists the positions, relative to vertex0, 
        /// of each of the 8 vertices of a cube.
        /// vertexOffset[8][3]
        /// </summary>
        protected static readonly int[,] VertexOffset = new int[,]
	    {
	        {0, 0, 0},{1, 0, 0},{1, 1, 0},{0, 1, 0},
	        {0, 0, 1},{1, 0, 1},{1, 1, 1},{0, 1, 1}
	    };
    }

}
