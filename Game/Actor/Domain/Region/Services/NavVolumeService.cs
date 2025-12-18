using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Actor.Domain.Region.Services
{
    public class NavVolumeService
    {
        private Vector3 volumeOrigin;   // 体素体积的原点
        private Vector3 volumeSize;     // 体素体积的尺寸
        private float voxelSize;        // 每个体素的大小（单位：世界空间单位）
        private BitArray voxelData;     // 体素数据位图，保存体素是否有效

        public NavVolumeService(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Voxel data file not found: {filePath}");
            }

            try
            {
                using (var fs = new FileStream(filePath, FileMode.Open))
                using (var gzip = new GZipStream(fs, CompressionMode.Decompress))
                using (var br = new BinaryReader(gzip))
                {
                    // 读取魔数和版本号
                    int magic = br.ReadInt32();
                    int version = br.ReadInt32();
                    if (magic != 0x564F584C) // 这里假设魔数为 "VOXL"（四字节）
                    {
                        Console.WriteLine("Invalid voxel data file format.");
                        return;
                    }

                    // 读取体素网格尺寸
                    int x = br.ReadInt32();
                    int y = br.ReadInt32();
                    int z = br.ReadInt32();

                    // 读取体素体积信息
                    volumeOrigin = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                    voxelSize = br.ReadSingle();
                    volumeSize = new Vector3(x * voxelSize, y * voxelSize, z * voxelSize);
                    // 读取采样半径和区域掩码（如果有）
                    int areaMask = br.ReadInt32();
                    float sampleRadius = br.ReadSingle();

                    // 读取体素数据位图
                    int byteLen = br.ReadInt32();
                    byte[] bytes = br.ReadBytes(byteLen);
                    voxelData = new BitArray(bytes);

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading voxel data: {ex.Message}");
                return;
            }
        }

        public (Vector2 min, Vector2 max) GetMapBoundsXZ()
        {
            float minX = volumeOrigin.X;
            float maxX = volumeOrigin.X + volumeSize.X;

            float minZ = volumeOrigin.Z;
            float maxZ = volumeOrigin.Z + volumeSize.Z;

            return (new Vector2(minX, minZ), new Vector2(maxX, maxZ));
        }

        public Vector3 ProjectToValidHeight(Vector3 worldPos)
        {
            var originGrid = Vector3ToGrid(worldPos);
            int baseY = originGrid.Y;

            // 在 ±3 层范围内找第一个有效的 voxel（一般就 0~4，总共 5 层，暴力搜爆快）
            for (int dy = -3; dy <= 3; dy++)
            {
                int testY = baseY + dy;
                if (testY < 0) continue;

                var testGrid = new Vector3Int(originGrid.X, testY, originGrid.Z);
                if (IsValidGrid(testGrid) && IsVoxelValid(testGrid))
                {
                    // 找到有效层！返回这个层的中心世界坐标
                    return GridToVector3(testGrid);
                }
            }

            // 极端情况：附近全无效，直接拉到 Y=0 层中心（至少不会 null）
            var fallback = new Vector3Int(originGrid.X, 0, originGrid.Z);
            if (IsValidGrid(fallback))
                return GridToVector3(fallback);

            // 还不成？直接返回原点 + voxelSize*0.5f
            return worldPos;
        }


        // 将世界坐标转换为网格坐标
        public Vector3Int Vector3ToGrid(Vector3 worldPos)
        {
            // 计算世界坐标相对于体素原点的偏移量
            Vector3 localPos = worldPos - volumeOrigin;

            // 计算网格坐标（体素索引），注意要向下取整
            int x = (int)Math.Floor(localPos.X / voxelSize);
            int y = (int)Math.Floor(localPos.Y / voxelSize);
            int z = (int)Math.Floor(localPos.Z / voxelSize);

            return new Vector3Int(x, y, z);
        }

        // 将网格坐标转换为世界坐标
        public Vector3 GridToVector3(Vector3Int gridPos)
        {
            // 将网格坐标转换回世界坐标
            return volumeOrigin + new Vector3(
                gridPos.X * voxelSize + voxelSize * 0.5f,
                gridPos.Y * voxelSize + voxelSize * 0.5f,
                gridPos.Z * voxelSize + voxelSize * 0.5f
            );
        }

        // 检查网格坐标是否有效
        public bool IsValidGrid(Vector3Int gridPos)
        {
            // 网格坐标必须在体素范围内
            return gridPos.X >= 0 && gridPos.X < volumeSize.X / voxelSize &&
                   gridPos.Y >= 0 && gridPos.Y < volumeSize.Y / voxelSize &&
                   gridPos.Z >= 0 && gridPos.Z < volumeSize.Z / voxelSize;
        }

        // 检查世界坐标是否有效
        public bool IsValidVector3(Vector3 worldPos)
        {
            // 将世界坐标转换为网格坐标，然后检查其有效性
            Vector3Int gridPos = Vector3ToGrid(worldPos);
            return IsValidGrid(gridPos);
        }


        // 根据体素数据判断某个体素是否有效
        public bool IsVoxelValid(Vector3Int gridPos)
        {
            // 确保网格坐标有效后，检查该体素是否被标记为有效
            if (!IsValidGrid(gridPos))
            {
                return false;
            }

            int index = (gridPos.X * (int)(volumeSize.Y / voxelSize) + gridPos.Y) * (int)(volumeSize.Z / voxelSize) + gridPos.Z;
            return voxelData[index];
        }
    }


    public struct Vector3Int
    {
        public int X, Y, Z;

        public Vector3Int(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static Vector3Int operator +(Vector3Int a, Vector3Int b)
        {
            Vector3Int vector3Int = new Vector3Int();
            vector3Int.X = a.X + b.X;
            vector3Int.Y = a.Y + b.Y;
            vector3Int.Z = a.Z + b.Z;
            return vector3Int;
        }

        public static bool operator ==(Vector3Int a, Vector3Int b)
        {

            return a.X == b.X &&
            a.Y == b.Y &&
            a.Z == b.Z;
        }

        public static bool operator !=(Vector3Int a, Vector3Int b)
        {

            return a.X != b.X &&
            a.Y != b.Y &&
            a.Z != b.Z;
        }

        public override string ToString() => $"({X}, {Y}, {Z})";
    }
}
