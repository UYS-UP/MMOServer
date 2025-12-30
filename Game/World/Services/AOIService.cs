using System;
using System.Collections.Generic;
using System.Numerics;

namespace Server.Game.World.Services
{
    public class AOIService
    {
        private readonly struct Cell : IEquatable<Cell>
        {
            public readonly int X;
            public readonly int Z;
            public Cell(int x, int z) { X = x; Z = z; }
            public bool Equals(Cell other) => X == other.X && Z == other.Z;
            public override bool Equals(object obj) => obj is Cell c && Equals(c);
            public override int GetHashCode() => HashCode.Combine(X, Z);
            public override string ToString() => $"({X},{Z})";
        }

        private readonly float cellSize;
        private readonly float viewRange;
        private readonly float heightTolerance;
        private readonly int neigh;

        // ===== 动态实体数据结构 =====
        private readonly Dictionary<int, Vector3> pos = new Dictionary<int, Vector3>();   // entityId -> 世界坐标
        private readonly Dictionary<int, Cell> cellOf = new Dictionary<int, Cell>();      // entityId -> AOI网格
        private readonly Dictionary<Cell, HashSet<int>> grid = new Dictionary<Cell, HashSet<int>>();  // AOI网格 -> 实体集合
        private readonly Dictionary<int, HashSet<int>> visibleCache = new Dictionary<int, HashSet<int>>();  // entityId -> 上次可见集合

        public AOIService(float viewRange, float cellSize, float heightTolerance)
        {
            this.viewRange = viewRange;
            this.cellSize = cellSize;
            this.heightTolerance = heightTolerance;
            neigh = Math.Max(1, (int)Math.Ceiling(viewRange / cellSize));
        }


        public void Add(int entityId, Vector3 position)
        {
            if (pos.ContainsKey(entityId)) return;

            pos[entityId] = position;
            var cell = ToCell(position);
            cellOf[entityId] = cell;
            GetOrCreate(grid, cell).Add(entityId);

            visibleCache[entityId] = new HashSet<int>();
        }

        public HashSet<int> GetVisibleSet(int entityId)
        {
            if (entityId == -1) return new HashSet<int>();
            if (!pos.TryGetValue(entityId, out var position)) return new HashSet<int>();
            var set = CollectVisibleSet(position, entityId);
            return set;
        }

        public void Remove(int entityId)
        {
            if (entityId == -1) return;

            if (cellOf.TryGetValue(entityId, out var c))
            {
                if (grid.TryGetValue(c, out var bucket))
                {
                    bucket.Remove(entityId);
                    if (bucket.Count == 0) grid.Remove(c);
                }
                cellOf.Remove(entityId);
            }
            pos.Remove(entityId);
            visibleCache.Remove(entityId);
        }

        public (HashSet<int> enterWatchers, HashSet<int> leaveWatchers) Update(int entityId, Vector3 newPosition)
        {
            if (!pos.ContainsKey(entityId)) Add(entityId, newPosition);

            var oldCell = cellOf[entityId];
            var newCell = ToCell(newPosition);

            if (!oldCell.Equals(newCell))
            {
                if (grid.TryGetValue(oldCell, out var setOld))
                {
                    setOld.Remove(entityId);
                    if (setOld.Count == 0) grid.Remove(oldCell);
                }

                GetOrCreate(grid, newCell).Add(entityId);
                cellOf[entityId] = newCell;
            }

            pos[entityId] = newPosition;

            var now = CollectVisibleSet(newPosition, entityId);
            if (!visibleCache.TryGetValue(entityId, out var cache)) cache = new HashSet<int>();

            var enter = new HashSet<int>(now);
            enter.ExceptWith(cache);
            var leave = new HashSet<int>(cache);
            leave.ExceptWith(now);

            visibleCache[entityId] = now;
            return (enter, leave);
        }


        public HashSet<int> QueryCircle(Vector3 centerPos, float radius)
        {
            var res = new HashSet<int>();
            var c = ToCell(centerPos);

            // 针对这个半径，计算需要遍历多少格子
            int localNeigh = Math.Max(1, (int)Math.Ceiling(radius / cellSize));
            float radius2 = radius * radius;

            for (int dz = -localNeigh; dz <= localNeigh; dz++)
            {
                for (int dx = -localNeigh; dx <= localNeigh; dx++)
                {
                    var nc = new Cell(c.X + dx, c.Z + dz);
                    if (!grid.TryGetValue(nc, out var bucket) || bucket.Count == 0)
                        continue;

                    foreach (var id in bucket)
                    {
                        // 水平距离判断
                        var otherPos = pos[id];
                        float dxh = otherPos.X - centerPos.X;
                        float dzh = otherPos.Z - centerPos.Z;
                        float dist2 = dxh * dxh + dzh * dzh;
                        if (dist2 > radius2)
                            continue;

                        if (MathF.Abs(otherPos.Y - centerPos.Y) > heightTolerance)
                            continue;

                        res.Add(id);
                    }
                }
            }

            return res;
        }


        private Cell ToCell(in Vector3 p)
        {
            int cx = (int)MathF.Floor(p.X / cellSize);
            int cz = (int)MathF.Floor(p.Z / cellSize);
            return new Cell(cx, cz);
        }

        private static HashSet<TValue> GetOrCreate<TKey, TValue>(Dictionary<TKey, HashSet<TValue>> map, TKey key)
            where TKey : notnull
        {
            if (!map.TryGetValue(key, out var set))
            {
                set = new HashSet<TValue>();
                map[key] = set;
            }
            return set;
        }

        private HashSet<int> CollectVisibleSet(in Vector3 centerPos, int selfId)
        {
            var res = new HashSet<int>();
            var c = ToCell(centerPos);

            for (int dz = -neigh; dz <= neigh; dz++)
            {
                for (int dx = -neigh; dx <= neigh; dx++)
                {
                    var nc = new Cell(c.X + dx, c.Z + dz);
                    if (!grid.TryGetValue(nc, out var bucket) || bucket.Count == 0) continue;

                    foreach (var id in bucket)
                    {
                        if (id == selfId) continue;

                        var otherPos = pos[id];

                        // 水平距离
                        float dxh = otherPos.X - centerPos.X;
                        float dzh = otherPos.Z - centerPos.Z;
                        float dist2 = dxh * dxh + dzh * dzh;
                        if (dist2 > viewRange * viewRange) continue;

                        // 高度容忍
                        if (MathF.Abs(otherPos.Y - centerPos.Y) > heightTolerance) continue;

                        res.Add(id);
                    }
                }
            }
            return res;
        }
    }
}
