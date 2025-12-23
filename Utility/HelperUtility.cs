using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Server.Utility
{
    public static class HelperUtility
    {
        
        public static List<int> ShuffleRollPool()
        {
            var list = Enumerable.Range(1, 100).ToList();
            var rng = Random.Shared;

            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
            return list;
        }

        public static Vector3 FloatArrayToVector3(float[] value)
        {
            return new Vector3(value[0], value[1], value[2]);
        }

        public static byte[] MessageSerializer<T>(T data)
        {
            return MessagePackSerializer.Serialize(data);
        }

        public static short[] Vector3ToShortArray(Vector3 vec)
        {
            return new short[] { (short)(vec.X * 100), (short)(vec.Y * 100), (short)(vec.Z * 100) };
        }

        public static Vector3 ShortArrayToVector3(short[] arr)
        {
            return new Vector3(arr[0] / 100f, arr[1] / 100f, arr[2] / 100f);
        }

        public static sbyte[] Vector3ToSbyteArray(Vector3 vec)
        {
            return new sbyte[] { (sbyte)(vec.X * 127), (sbyte)(vec.Y * 127), (sbyte)(vec.Z * 127) };
        }

        public static Vector3 SbyteArrayToVector3(sbyte[] arr)
        {
            return new Vector3(arr[0] / 127f, arr[1] / 127f, arr[2] / 127f);
        }

        public static short YawToShort(float yaw)
        {
            float wrappedYaw = (float)((yaw % 360 + 360) % 360);
            return (short)(wrappedYaw * 10);
        }

        public static float ShortToYaw(short yaw)
        {
            return yaw / 10f;
        }

        /// <summary>
        /// 根据 Yaw 角（弧度）计算朝向向量（XZ 平面上）。
        /// </summary>
        public static Vector3 YawToForward(float yaw)
        {
            float rad = yaw * MathF.PI / 180f;
            float x = MathF.Sin(rad);
            float z = MathF.Cos(rad);
            return new Vector3(x, 0f, z);
        }


        public static Vector3 RotateVector(Vector3 v, float yawDegrees)
        {
            // 将角度转弧度
            float radians = yawDegrees * (MathF.PI / 180f);

            float cos = MathF.Cos(radians);
            float sin = MathF.Sin(radians);

            // 绕 Y 轴旋转矩阵公式
            float newX = v.X * cos + v.Z * sin;
            float newZ = v.X * -sin + v.Z * cos;

            return new Vector3(newX, v.Y, newZ);
        }

        /// <summary>
        /// 如果你手里的是角度（度），可先转弧度。
        /// </summary>
        public static Vector3 YawDegToForward(float yawDeg)
        {
            float rad = yawDeg * MathF.PI / 180f;
            return YawToForward(rad);
        }


        public static Vector3 GetForwardDirection(Quaternion rotation)
        {
            return Vector3.Transform(Vector3.UnitZ, rotation);
        }

        public static Vector3 GetBackwardDirection(Quaternion rotation)
        {
            return Vector3.Transform(-Vector3.UnitZ, rotation);
        }

        public static Vector3 GetRightDirection(Quaternion rotation)
        {
            return Vector3.Transform(Vector3.UnitX, rotation);
        }

        public static Vector3 GetLeftDirection(Quaternion rotation)
        {
            return Vector3.Transform(-Vector3.UnitX, rotation);
        }

        public static Vector3 GetUpDirection(Quaternion rotation)
        {
            return Vector3.Transform(Vector3.UnitY, rotation);
        }

        public static Vector3 GetDownDirection(Quaternion rotation)
        {
            return Vector3.Transform(-Vector3.UnitY, rotation);
        }

        public static Vector3 GetForwardDirection(float yaw)
        {
            float yawRad = yaw * (MathF.PI / 180f);
            return new Vector3(MathF.Sin(yawRad), 0, MathF.Cos(yawRad));
        }

        public static Vector3 GetRightDirection(float yaw)
        {
            float yawRad = yaw * (MathF.PI / 180f);
            return new Vector3(MathF.Cos(yawRad), 0, -MathF.Sin(yawRad));
        }

        public static Vector3 GetMovementDirection(Vector3 delta)
        {
            var horizontalDelta = new Vector3(delta.X, 0, delta.Z);

            // 检查输入是否接近零向量
            if (horizontalDelta.LengthSquared() < 0.0001f) // 使用平方长度避免开方
            {
                return Vector3.Zero;
            }

            return Vector3.Normalize(horizontalDelta);
        }

        public static Vector3 GetDirection(float yaw, float pitch)
        {
            Quaternion rotation = Quaternion.CreateFromYawPitchRoll(
                yaw * (MathF.PI / 180f),
                pitch * (MathF.PI / 180f),
                0);
            return Vector3.Transform(Vector3.UnitZ, rotation);
        }

        public static float GetYawDegrees(Quaternion q)
        {
            float yawRadians = GetYaw(q);
            return yawRadians * (180f / MathF.PI); // 弧度转角度
        }

        public static float GetYaw(Quaternion q)
        {
            float yaw = MathF.Atan2(
                2f * q.Y * q.W - 2f * q.X * q.Z,
                1f - 2f * q.Y * q.Y - 2f * q.Z * q.Z
            );
            return yaw;
        }


        public static float SqrMagnitude(this Vector3 value)
        {
            return (float)((double)value.X * (double)value.X + (double)value.Y * (double)value.Y + (double)value.Z * (double)value.Z);
        }


        public static float GetYawFromDirection(Vector3 direction)
        {
            float angle = (float)Math.Atan2(direction.X, direction.Z) * (float)(180.0 / Math.PI);
            return angle < 0 ? angle + 360 : angle;
        }


        public static int SecondsToTicks(float seconds, int tickMs) => (int)MathF.Ceiling(seconds * 1000f / tickMs);

        public static float PointToSegmentDistance(Vector3 point, Vector3 segmentStart, Vector3 segmentEnd)
        {
            Vector3 v = segmentEnd - segmentStart; // 向量AB
            Vector3 w = point - segmentStart;      // 向量AP

            float c1 = Vector3.Dot(w, v);
            if (c1 <= 0) // 点P在线段AB起点A的外面
            {
                return Vector3.Distance(point, segmentStart);
            }

            float c2 = Vector3.Dot(v, v);
            if (c2 <= c1) // 点P在线段AB终点B的外面
            {
                return Vector3.Distance(point, segmentEnd);
            }

            float b = c1 / c2;
            Vector3 pb = segmentStart + b * v; // 点P在线段AB上的投影点
            return Vector3.Distance(point, pb);
        }

        public static string GetKey()
        {
            return Guid.NewGuid().ToString();
        }


        public const float PI = (float)Math.PI;
        public const float TwoPI = 2f * PI;
        public const float Deg2Rad = PI / 180f;
        public const float Rad2Deg = 180f / PI;
        public const float Epsilon = 1e-5f;

        #region Vector3 插值方法

        /// <summary>
        /// Vector3线性插值
        /// </summary>
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t)
        {
            t = Clamp01(t);
            return new Vector3(
                a.X + (b.X - a.X) * t,
                a.Y + (b.Y - a.Y) * t,
                a.Z + (b.Z - a.Z) * t
            );
        }

        /// <summary>
        /// Vector3球形插值
        /// </summary>
        public static Vector3 Slerp(Vector3 a, Vector3 b, float t)
        {
            t = Clamp01(t);

            float magnitudeA = a.Length();
            float magnitudeB = b.Length();

            if (magnitudeA < Epsilon || magnitudeB < Epsilon)
                return Lerp(a, b, t);

            Vector3 directionA = a / magnitudeA;
            Vector3 directionB = b / magnitudeB;

            float dot = Vector3.Dot(directionA, directionB);
            dot = Clamp(dot, -1f, 1f);

            float angle = (float)Math.Acos(dot) * t;
            Vector3 direction = Vector3.Normalize(directionB - directionA * dot);

            Vector3 slerpDirection = directionA * (float)Math.Cos(angle) + direction * (float)Math.Sin(angle);
            float slerpMagnitude = magnitudeA + (magnitudeB - magnitudeA) * t;

            return slerpDirection * slerpMagnitude;
        }

        /// <summary>
        /// Vector3平滑阻尼
        /// </summary>
        public static Vector3 SmoothDamp(Vector3 current, Vector3 target, ref Vector3 currentVelocity,
                                       float smoothTime, float maxSpeed = float.MaxValue, float deltaTime = 0.016f)
        {
            smoothTime = Max(0.0001f, smoothTime);
            float omega = 2f / smoothTime;

            float x = omega * deltaTime;
            float exp = 1f / (1f + x + 0.48f * x * x + 0.235f * x * x * x);

            Vector3 change = current - target;
            Vector3 originalTo = target;

            // 限制最大变化量
            float maxChange = maxSpeed * smoothTime;
            float changeMagnitude = change.Length();
            if (changeMagnitude > maxChange)
                change = change / changeMagnitude * maxChange;

            target = current - change;

            Vector3 temp = (currentVelocity + omega * change) * deltaTime;
            currentVelocity = (currentVelocity - omega * temp) * exp;

            Vector3 output = target + (change + temp) * exp;

            // 防止过冲
            if (Vector3.Dot(originalTo - current, output - originalTo) > 0)
            {
                output = originalTo;
                currentVelocity = (output - originalTo) / deltaTime;
            }

            return output;
        }

        /// <summary>
        /// Vector3朝向目标移动
        /// </summary>
        public static Vector3 MoveTowards(Vector3 current, Vector3 target, float maxDistanceDelta)
        {
            Vector3 toVector = target - current;
            float distance = toVector.Length();

            if (distance <= maxDistanceDelta || distance < Epsilon)
                return target;

            return current + toVector / distance * maxDistanceDelta;
        }

        #endregion

        #region Yaw 插值方法

        /// <summary>
        /// Yaw线性插值（处理角度环绕）
        /// </summary>
        public static float LerpYaw(float a, float b, float t)
        {
            t = Clamp01(t);

            // 规范化角度到[0, 2π]
            a = NormalizeAngle(a);
            b = NormalizeAngle(b);

            // 计算最短路径
            float diff = b - a;
            if (diff > PI)
                diff -= TwoPI;
            else if (diff < -PI)
                diff += TwoPI;

            float result = a + diff * t;
            return NormalizeAngle(result);
        }

        /// <summary>
        /// Yaw球形插值
        /// </summary>
        public static float SlerpYaw(float a, float b, float t)
        {
            t = Clamp01(t);

            a = NormalizeAngle(a);
            b = NormalizeAngle(b);

            float diff = b - a;
            if (diff > PI)
                diff -= TwoPI;
            else if (diff < -PI)
                diff += TwoPI;

            // 小角度时使用线性插值
            float angle = Math.Abs(diff);
            if (angle < 0.1f)
                return a + diff * t;

            // 球形插值
            float sinAngle = (float)Math.Sin(angle);
            float weightA = (float)Math.Sin((1f - t) * angle) / sinAngle;
            float weightB = (float)Math.Sin(t * angle) / sinAngle;

            float result = a * weightA + (a + diff) * weightB;
            return NormalizeAngle(result);
        }

        /// <summary>
        /// Yaw平滑阻尼
        /// </summary>
        public static float SmoothDampYaw(float current, float target, ref float currentVelocity,
                                         float smoothTime, float maxSpeed = float.MaxValue, float deltaTime = 0.016f)
        {
            // 处理角度环绕
            target = current + DeltaAngle(current, target);

            smoothTime = Max(0.0001f, smoothTime);
            float omega = 2f / smoothTime;

            float x = omega * deltaTime;
            float exp = 1f / (1f + x + 0.48f * x * x + 0.235f * x * x * x);

            float change = current - target;

            // 限制最大变化量
            float maxChange = maxSpeed * smoothTime;
            change = Clamp(change, -maxChange, maxChange);

            float temp = (currentVelocity + omega * change) * deltaTime;
            currentVelocity = (currentVelocity - omega * temp) * exp;

            float output = current - (change + temp) * exp;

            // 防止过冲
            if ((target - current) * (output - target) > 0)
            {
                output = target;
                currentVelocity = (output - target) / deltaTime;
            }

            return output;
        }

        /// <summary>
        /// Yaw朝向目标移动
        /// </summary>
        public static float MoveTowardsYaw(float current, float target, float maxDelta)
        {
            float delta = DeltaAngle(current, target);
            if (-maxDelta < delta && delta < maxDelta)
                return target;

            return current + Clamp(delta, -maxDelta, maxDelta);
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 规范化角度到[0, 2π]
        /// </summary>
        public static float NormalizeAngle(float angle)
        {
            angle %= TwoPI;
            if (angle < 0)
                angle += TwoPI;
            return angle;
        }

        /// <summary>
        /// 规范化角度到[-π, π]
        /// </summary>
        public static float NormalizeAngleSigned(float angle)
        {
            angle = NormalizeAngle(angle);
            if (angle > PI)
                angle -= TwoPI;
            return angle;
        }

        /// <summary>
        /// 计算两个角度之间的最小差值（考虑角度环绕）
        /// </summary>
        public static float DeltaAngle(float current, float target)
        {
            float delta = (target - current) % TwoPI;
            if (delta > PI)
                delta -= TwoPI;
            else if (delta < -PI)
                delta += TwoPI;
            return delta;
        }

        /// <summary>
        /// 限制数值在[0, 1]范围内
        /// </summary>
        public static float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            if (value > 1f) return 1f;
            return value;
        }

        /// <summary>
        /// 限制数值在min-max范围内
        /// </summary>
        public static float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        /// <summary>
        /// 返回两个值中的较大值
        /// </summary>
        public static float Max(float a, float b)
        {
            return a > b ? a : b;
        }

        private static Random random = new Random();

        public static Vector2 RandomPointInCircle(float radius)
        {
            float angle = (float)(random.NextDouble() * 2 * PI);
            float r = radius * (float)Math.Sqrt(random.NextDouble());
            float x = r * (float)Math.Cos(angle);
            float y = r * (float)Math.Sin(angle);
            return new Vector2(x, y);
        }

        #endregion

    }



}
