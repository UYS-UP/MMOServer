using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;

namespace Server.Game.World.Skill
{
    public static class SkillTimelineJsonSerializer
    {
        public static Dictionary<int, SkillTimelineConfig> SkillConfigs { get; private set; }

        private static JsonSerializerSettings CreateSerializerSettings()
        {
            var settings = new JsonSerializerSettings();

            settings.Formatting = Formatting.Indented;
            settings.TypeNameHandling = TypeNameHandling.Auto;
            settings.NullValueHandling = NullValueHandling.Ignore;
            settings.Converters.Add(new Vector3Converter());
            settings.Converters.Add(new Vector2Converter());
            settings.SerializationBinder = new AliasBinder(new[]
            {
                typeof(CircleDamageEvent),
                typeof(ServerMoveStepPhase)
                
            });
            return settings;
        }

        public static void Deserializer(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("反序列化失败:" + filePath + "文件不存在");
            }

            try
            {
                string json = File.ReadAllText(filePath);
                var settings = CreateSerializerSettings();

                var asset = JsonConvert.DeserializeObject<List<SkillTimelineConfig>>(json, settings);
                SkillConfigs = new Dictionary<int, SkillTimelineConfig>();
                foreach (var skill in asset)
                {
                    SkillConfigs[skill.Id] = skill;
                }
            }
            catch (Exception ex)
            {
                throw new JsonSerializationException("反序列化失败:" + ex);
            }
        }


        public static void Serializer(List<SkillTimelineConfig> skillTimelineConfigs, string filePath)
        {
            try
            {
                var settings = CreateSerializerSettings();
                string json = JsonConvert.SerializeObject(skillTimelineConfigs, settings);

                // 确保目录存在
                string directory = Path.GetDirectoryName(filePath);
                if (Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                throw new JsonSerializationException("序列化失败:" + ex);
            }
        }

    }


    public sealed class AliasBinder : ISerializationBinder
    {
        private readonly Dictionary<string, Type> _aliasToType;
        private readonly Dictionary<Type, string> _typeToAlias;

        public AliasBinder(IEnumerable<Type> knownTypes)
        {
            _aliasToType = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
            _typeToAlias = new Dictionary<Type, string>();

            foreach (var t in knownTypes)
            {
                var alias = t.GetCustomAttribute<JsonTypeAliasAttribute>()?.Alias ?? t.Name;
                _aliasToType[alias] = t;
                _typeToAlias[t] = alias;
            }
        }

 
        public Type BindToType(string assemblyName, string typeName)
            => _aliasToType.TryGetValue(typeName, out var t) ? t
                : throw new JsonSerializationException($"Unknown type alias: {typeName}");


        public void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            assemblyName = null;
            if (!_typeToAlias.TryGetValue(serializedType, out typeName))
                typeName = serializedType.Name;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class JsonTypeAliasAttribute : Attribute
    {
        public string Alias { get; }
        public JsonTypeAliasAttribute(string alias) => Alias = alias;
    }

    public class SkillEventConverter : JsonConverter<SkillEvent>
    {

        public override SkillEvent ReadJson(JsonReader reader, Type objectType, SkillEvent? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);

            // 读取 Type 字段来确定具体类型
            if (!jo.TryGetValue("Type", StringComparison.OrdinalIgnoreCase, out var typeToken))
            {
                throw new JsonSerializationException("缺少 'Type' 字段，无法确定 SkillEvent 的具体类型");
            }

            string type = typeToken.Value<string>();

            // 根据 Type 字段创建对应的具体类型实例
            SkillEvent skillEvent = type switch
            {
                nameof(CircleDamageEvent) => new CircleDamageEvent(),
                nameof(RectangleDamageEvent) => new RectangleDamageEvent(),
                nameof(DashEvent) => new DashEvent(),
                nameof(ApplyBuffEvent) => new ApplyBuffEvent(), 
                nameof(SingletonDamageEvent) => new SingletonDamageEvent(),
                _ => throw new JsonSerializationException($"未知的 SkillEvent 类型: {type}")
            };

            // 使用默认的序列化器填充对象属性
            serializer.Populate(jo.CreateReader(), skillEvent);

            return skillEvent;
        }


        public override void WriteJson(JsonWriter writer, SkillEvent? value, JsonSerializer serializer)
        {
            
        }
    }


    public class Vector3Converter : JsonConverter<Vector3>
    {
        public override void WriteJson(JsonWriter writer, Vector3 value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(value.X);
            writer.WritePropertyName("y");
            writer.WriteValue(value.Y);
            writer.WritePropertyName("z");
            writer.WriteValue(value.Z);
            writer.WriteEndObject();
        }

        public override Vector3 ReadJson(JsonReader reader, Type objectType, Vector3 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var obj = JObject.Load(reader);
            return new Vector3(
                obj.Value<float>("x"),
                obj.Value<float>("y"),
                obj.Value<float>("z")
            );
        }
    }


    public class Vector2Converter : JsonConverter<Vector2>
    {
        public override void WriteJson(JsonWriter writer, Vector2 value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(value.X);
            writer.WritePropertyName("y");
            writer.WriteValue(value.Y);
            writer.WriteEndObject();
        }

        public override Vector2 ReadJson(JsonReader reader, Type objectType, Vector2 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var obj = JObject.Load(reader);
            return new Vector2(
                obj.Value<float>("x"),
                obj.Value<float>("y")
            );
        }
    }
}
