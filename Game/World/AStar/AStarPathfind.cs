
using Server.Game.World.Services;
using System.Numerics;

namespace Server.Game.World.AStar
{
    public class AStarPathfind
    {
        public enum NeighborMode
        { N6 = 6, N18 = 18, N26 = 26 }

        private readonly NavVolumeService navVolumeService;
        private readonly float sampleStep;
        private readonly NeighborMode neighborMode;
        private readonly float goalTolerance;
        private readonly float heuristicScale;

        public AStarPathfind(NavVolumeService navVolumeService, float sampleStep = 0.5f,
            NeighborMode neighborMode = NeighborMode.N26, float goalTolerance = 0.2f,
            float heuristicScale = 1.0f)
        {
            this.navVolumeService = navVolumeService;
            this.sampleStep = MathF.Max(0.1f, sampleStep);
            this.neighborMode = neighborMode;
            this.goalTolerance = MathF.Max(0.05f, goalTolerance);
            this.heuristicScale = MathF.Max(0.1f, heuristicScale);
        }

        public List<Vector3> FindPath(Vector3 goal, Vector3 start, int maxExpand = 100_000)
        {
            start = navVolumeService.ProjectToValidHeight(start);
            goal = navVolumeService.ProjectToValidHeight(goal);


            var startGrid = navVolumeService.Vector3ToGrid(start);
            var goalGrid = navVolumeService.Vector3ToGrid(goal);

            if (!navVolumeService.IsValidGrid(startGrid) || !navVolumeService.IsVoxelValid(startGrid)) return null;
            if (!navVolumeService.IsValidGrid(goalGrid) || !navVolumeService.IsVoxelValid(goalGrid)) return null;

            if (Vector3.Distance(start, goal) <= goalTolerance) return new List<Vector3> { start, goal };
            if (HasLineOfSight(start, goal)) return new List<Vector3> { start, goal };

            var open = new MinHeap<Node>((a, b) =>
            {
                int c = a.F.CompareTo(b.F);
                return c != 0 ? c : a.F.CompareTo(b.H);
            });

            var gScore = new Dictionary<Vector3Int, float>();
            var cameFrom = new Dictionary<Vector3Int, Vector3Int>();
            var bestF = new Dictionary<Vector3Int, float>();

            float h0 = Heuristic(startGrid, goalGrid);
            var startNode = new Node(startGrid, 0f, h0);
            open.Push(startNode);
            gScore[startGrid] = 0f;
            bestF[startGrid] = startNode.F;

            var neighborOffsets = BuildNeighborOffsets(neighborMode);
            int expanded = 0;

            while (open.Count > 0)
            {
                var cur = open.Pop();
                if (bestF.TryGetValue(cur.Pos, out float bf) && cur.F > bf)
                    continue;

                if (cur.Pos.Equals(goalGrid))
                {
                    var raw = ReconstructPath(cameFrom, cur.Pos, startGrid);
                    return SimplifyPathByLineOfSight(raw);
                }

                var curWorld = navVolumeService.GridToVector3(cur.Pos);
                if (Vector3.Distance(curWorld, goal) <= goalTolerance)
                {
                    var raw = ReconstructPath(cameFrom, cur.Pos, startGrid, overrideGoalWorld: goal);
                    return SimplifyPathByLineOfSight(raw);
                }

                if (++expanded > maxExpand)
                {
                    // Console.WriteLine("超过最大迭代上限");
                    break;
                }

                foreach (var (dx, dy, dz, stepCost) in neighborOffsets)
                {
                    var ngrid = new Vector3Int(cur.Pos.X + dx, cur.Pos.Y + dy, cur.Pos.Z + dz);

                    if (!navVolumeService.IsValidGrid(ngrid)) continue;
                    if (!navVolumeService.IsVoxelValid(ngrid)) continue;

                    float tentativeG = gScore[cur.Pos] + stepCost;
                    if (!gScore.TryGetValue(ngrid, out float oldG) || tentativeG < oldG)
                    {
                        gScore[ngrid] = tentativeG;
                        cameFrom[ngrid] = cur.Pos;

                        float h = Heuristic(ngrid, goalGrid);
                        var node = new Node(ngrid, tentativeG, h);
                        bestF[ngrid] = node.F;
                        open.Push(node);
                    }
                }
            }

            return null;
        }

        private List<Vector3> ReconstructPath(Dictionary<Vector3Int, Vector3Int> cameFrom,
           Vector3Int cur, Vector3Int start, Vector3? overrideGoalWorld = null)
        {
            var rev = new List<Vector3Int> { cur };
            while (!cur.Equals(start) && cameFrom.TryGetValue(cur, out var p))
            {
                cur = p;
                rev.Add(cur);
            }
            rev.Reverse();

            var path = new List<Vector3>(rev.Count);
            for (int i = 0; i < rev.Count; i++)
            {
                if (i == rev.Count - 1 && overrideGoalWorld.HasValue)
                    path.Add(overrideGoalWorld.Value);
                else
                    path.Add(navVolumeService.GridToVector3(rev[i]));
            }
            return path;
        }

        private List<Vector3> SimplifyPathByLineOfSight(List<Vector3> path)
        {
            if (path == null || path.Count <= 2) return path;

            var simplified = new List<Vector3>(path.Count);
            int anchor = 0;
            simplified.Add(path[anchor]);

            int probe = anchor + 2;
            while (probe < path.Count)
            {
                if (HasLineOfSight(path[anchor], path[probe]))
                {
                    probe++;
                    continue;
                }

                int keep = probe - 1;
                if (Vector3.Distance(simplified[^1], path[keep]) > goalTolerance * 0.5f)
                    simplified.Add(path[keep]);

                anchor = keep;
                probe = anchor + 2;
            }

            if (Vector3.Distance(simplified[^1], path[^1]) > goalTolerance * 0.0f)
                simplified.Add(path[^1]);

            return simplified;
        }

        private static (int dx, int dy, int dz, float cost)[] BuildNeighborOffsets(NeighborMode mode)
        {
            var list = new List<(int, int, int, float)>(26);

            int[][] n6 = new[]
            {
                new[] {+1,0,0}, new[] {-1,0,0},
                new[] {0,+1,0}, new[] {0,-1,0},
                new[] {0,0,+1}, new[] {0,0,-1},
            };
            foreach (var v in n6) list.Add((v[0], v[1], v[2], 1f));

            if (mode == NeighborMode.N6) return list.ToArray();

            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        int md = Math.Abs(dx) + Math.Abs(dy) + Math.Abs(dz);
                        if (md == 2) list.Add((dx, dy, dz, MathF.Sqrt(2f)));
                    }

            if (mode == NeighborMode.N18) return list.ToArray();

            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        if (Math.Abs(dx) + Math.Abs(dy) + Math.Abs(dz) == 3)
                            list.Add((dx, dy, dz, MathF.Sqrt(3f)));
                    }

            return list.ToArray();
        }

        public bool HasLineOfSight(Vector3 a, Vector3 b)
        {
            Vector3 dir = b - a;
            float len = dir.Length();
            if (len <= float.Epsilon) return true;
            dir /= len;

            int steps = Math.Max(1, (int)MathF.Ceiling(len / sampleStep));
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                Vector3 p = a + dir * (t * len);

                var g = navVolumeService.Vector3ToGrid(p);
                if (!navVolumeService.IsValidGrid(g) || !navVolumeService.IsVoxelValid(g))
                    return false;
            }
            return true;
        }

        private float Heuristic(Vector3Int a, Vector3Int b)
        {
            int dx = a.X - b.X, dy = a.Y - b.Y, dz = a.Z - b.Z;
            float dist = MathF.Sqrt(dx * dx + dy * dy + dz * dz);
            return heuristicScale * dist;
        }

        private readonly struct Node
        {
            public readonly Vector3Int Pos;
            public readonly float G;
            public readonly float H;
            public float F => G + H;

            public Node(Vector3Int pos, float g, float h)
            { Pos = pos; G = g; H = h; }
        }
    }
}