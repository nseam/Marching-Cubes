using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Burst;
using UnityEngine;
using WARP.Terraform.API;

namespace MarchingCubesProject
{
    public interface IVertexData
    {
        public Vector3 Position { get; set; }

        public float Density { get; set; }
    }

    /// <summary>
    /// A helper class to hold voxel data.
    /// </summary>
    [Serializable]
    public class VoxelArray
    {
        /// <summary>
        /// Create a new voxel array.
        /// </summary>
        /// <param name="width">The size of the voxels on the x axis.</param>
        /// <param name="height">The size of the voxels on the y axis.</param>
        /// <param name="depth">The size of the voxels on the z axis.</param>
        public VoxelArray(int width, int height, int depth, float density = 0.0f)
        {
            Voxels      = new TerraformPoint[width * height * depth];
            Width       = width;
            Height      = height;
            Depth       = depth;

            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    for (int z = 0; z < depth; z++)
                        GetVoxel(x, y, z).Density = density;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public VoxelArray(Vector3Int size) : this(size.x, size.y, size.z)
        {
        }

        /// <summary>
        /// The size of the voxels on the x axis.
        /// </summary>
        public int Width;

        /// <summary>
        /// The size of the voxels on the y axis.
        /// </summary>
        public int Height;

        /// <summary>
        /// The size of the voxels on the z axis.
        /// </summary>
        public int Depth;

        /// <summary>
        /// The size of the voxels.
        /// </summary>
        public Vector3Int Size => new(Width, Height, Depth);

        /// <summary>
        /// 
        /// </summary>
        public bool FlipNormals { get; set; }

        /// <summary>
        /// Get/set the voxel data.
        /// </summary>
        /// <param name="x">The index on the x axis.</param>
        /// <param name="y">The index on the y axis.</param>
        /// <param name="z">The index on the z axis.</param>
        /// <returns>The voxels data.</returns>
        public ref TerraformPoint this[int x, int y, int z] => ref Voxels[x + y * Width + z * Width * Height];

        /// <summary>
        /// The voxel data.
        /// </summary>
        public TerraformPoint[] Voxels;

        /// <summary>
        /// Get the voxel data at clamped indices x,y,z.
        /// </summary>
        /// <param name="x">The index on the x axis.</param>
        /// <param name="y">The index on the y axis.</param>
        /// <param name="z">The index on the z axis.</param>
        /// <returns>The voxels data.</returns>
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref TerraformPoint GetVoxel(int x, int y, int z)
        {
            x = x < 0 ? 0 : (x >= Width ? Width - 1 : x);
            y = y < 0 ? 0 : (y >= Height ? Height - 1 : y);
            z = z < 0 ? 0 : (z >= Depth ? Depth - 1 : z);
            return ref Voxels[x + y * Width + z * Width * Height];
        }

        /// <summary>
        /// Get the voxel data at clamped indices x,y,z.
        /// </summary>
        /// <param name="x">The index on the x axis.</param>
        /// <param name="y">The index on the y axis.</param>
        /// <param name="z">The index on the z axis.</param>
        /// <returns>The voxels data.</returns>
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetVoxelDensity(int x, int y, int z) => GetVoxel(x , y, z).Density;

        /// <summary>
        /// Get the voxel data at normalized (0-1) clamped indices u,v,w.
        /// </summary>
        /// <param name="u">The normalized index (0-1) on the x axis.</param>
        /// <param name="v">The normalized index (0-1) on the y axis.</param>
        /// <param name="w">The normalized index (0-1) on the z axis.</param>
        /// <returns>The voxel data</returns>
        [BurstCompile]
        public float GetVoxelDensity(float u, float v, float w)
        {
            float x = u * (Width - 1);
            float y = v * (Height - 1);
            float z = w * (Depth - 1);

            int xi = (int) Mathf.Floor(x);
            int yi = (int) Mathf.Floor(y);
            int zi = (int) Mathf.Floor(z);

            float v000 = GetVoxelDensity(xi,     yi,     zi);
            float v100 = GetVoxelDensity(xi + 1, yi,     zi);
            float v010 = GetVoxelDensity(xi,     yi + 1, zi);
            float v110 = GetVoxelDensity(xi + 1, yi + 1, zi);

            float v001 = GetVoxelDensity(xi,     yi,     zi + 1);
            float v101 = GetVoxelDensity(xi + 1, yi,     zi + 1);
            float v011 = GetVoxelDensity(xi,     yi + 1, zi + 1);
            float v111 = GetVoxelDensity(xi + 1, yi + 1, zi + 1);

            float tx = Mathf.Clamp01(x - xi);
            float ty = Mathf.Clamp01(y - yi);
            float tz = Mathf.Clamp01(z - zi);

            //use bilinear interpolation the find these values.
            float v0 = BLerp(v000, v100, v010, v110, tx, ty);
            float v1 = BLerp(v001, v101, v011, v111, tx, ty);

            //Now lerp those values for the final trilinear interpolation.
            return Lerp(v0, v1, tz);
        }

        /// <summary>
        /// Get the voxels normal at the indices x,y,z.
        /// The normal will be all zeros in any areas of the voxels that are constant.
        /// </summary>
        /// <param name="x">The index on the x axis.</param>
        /// <param name="y">The index on the y axis.</param>
        /// <param name="z">The index on the z axis.</param>
        /// <returns></returns>
        [BurstCompile]
        public Vector3 GetNormal(int x, int y, int z)
        {
            var n = GetFirstDerivative(x, y, z);

            if (FlipNormals)
                return n.normalized * -1;
            else
                return n.normalized;
        }

        /// <summary>
        /// Get the voxels normal at the normalized indices u,v,w.
        /// The normal will be all zeros in any areas of the voxels that are constant.
        /// </summary>
        /// <param name="u">The normalized index (0-1) on the x axis.</param>
        /// <param name="v">The normalized index (0-1 on the y axis.</param>
        /// <param name="w">The normalized index (0-1 on the z axis.</param>
        /// <returns></returns>
        [BurstCompile]
        public Vector3 GetNormal(float u, float v, float w)
        {
            var n = GetFirstDerivative(u, v, w);

            if (FlipNormals)
                return n.normalized * -1;
            else
                return n.normalized;
        }

        /// <summary>
        /// Get the voxels first derivative at the indices x,y,z.
        /// The derivative will be all zeros in any areas of the voxels that are constant.
        /// The derivative is calculated using back and forward finite differance.
        /// </summary>
        /// <param name="x">The index on the x axis.</param>
        /// <param name="y">The index on the y axis.</param>
        /// <param name="z">The index on the z axis.</param>
        /// <returns></returns>
        [BurstCompile]
        public Vector3 GetFirstDerivative(int x, int y, int z)
        {
            float dx_p1 = GetVoxelDensity(x + 1, y,     z);
            float dy_p1 = GetVoxelDensity(x,     y + 1, z);
            float dz_p1 = GetVoxelDensity(x,     y,     z + 1);

            float dx_m1 = GetVoxelDensity(x - 1, y,     z);
            float dy_m1 = GetVoxelDensity(x,     y - 1, z);
            float dz_m1 = GetVoxelDensity(x,     y,     z - 1);

            float dx = (dx_p1 - dx_m1) * 0.5f;
            float dy = (dy_p1 - dy_m1) * 0.5f;
            float dz = (dz_p1 - dz_m1) * 0.5f;

            return new Vector3(dx, dy, dz);
        }

        /// <summary>
        /// Get the voxels first derivative at the normalized indices u,v,w.
        /// The first derivative will be all zeros in any areas of the voxels that are constant.
        /// The derivative is calculated using back and forward finite differance.
        /// </summary>
        /// <param name="u">The normalized index (0-1) on the x axis.</param>
        /// <param name="v">The normalized index (0-1 on the y axis.</param>
        /// <param name="w">The normalized index (0-1 on the z axis.</param>
        /// <returns></returns>
        [BurstCompile]
        public Vector3 GetFirstDerivative(float u, float v, float w)
        {
            const float h  = 0.005f;
            const float hh = h * 0.5f;
            const float ih = 1.0f / h;

            float dx_p1 = GetVoxelDensity(u + hh, v,      w);
            float dy_p1 = GetVoxelDensity(u,      v + hh, w);
            float dz_p1 = GetVoxelDensity(u,      v,      w + hh);

            float dx_m1 = GetVoxelDensity(u - hh, v,      w);
            float dy_m1 = GetVoxelDensity(u,      v - hh, w);
            float dz_m1 = GetVoxelDensity(u,      v,      w - hh);

            float dx = (dx_p1 - dx_m1) * ih;
            float dy = (dy_p1 - dy_m1) * ih;
            float dz = (dz_p1 - dz_m1) * ih;

            return new Vector3(dx, dy, dz);
        }

        /// <summary>
        /// Linear interpolation.
        /// </summary>
        /// <param name="v0"></param>
        /// <param name="v1"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        [BurstCompile]
        private static float Lerp(float v0, float v1, float t)
        {
            return v0 + (v1 - v0) * t;
        }

        /// <summary>
        /// Bilinear interpolation.
        /// </summary>
        /// <param name="v00"></param>
        /// <param name="v10"></param>
        /// <param name="v01"></param>
        /// <param name="v11"></param>
        /// <param name="tx"></param>
        /// <param name="ty"></param>
        /// <returns></returns>
        [BurstCompile]
        private static float BLerp(float v00, float v10, float v01, float v11, float tx, float ty)
        {
            return Lerp(Lerp(v00, v10, tx), Lerp(v01, v11, tx), ty);
        }
    }
}