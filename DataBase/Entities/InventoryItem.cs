using Newtonsoft.Json;
using Server.Game.Actor.Domain.ACharacter;
using Server.Game.Contracts.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.DataBase.Entities
{
    [Table("inventory_items")]
    public class InventoryItem
    {
        [Key]
        [Column("dbId")]
        public long DbId { get; set; }

        [Column("characterId")]
        [Required]
        public string CharacterId { get; set; }

        [Column("instanceId")]
        public string InstanceId { get; set; }


        [Column("itemType")]
        [Required]
        public ItemType ItemType { get; set; }

        [Column("count")]
        [Required]
        public int Count { get; set; }

        [Column("templateId")]
        [Required]
        public string TemplateId { get; set; }

        [Column("slotContainer")]
        [Required]
        public SlotContainerType SlotContainer;

        [Column("slotIndex")]
        [Required]
        public int SlotIndex { get; set; }

        [Column("forgeLevel")]
        public int ForgeLevel { get; set; }

        public EquipDynamicData DynamicData { get; set; } = new EquipDynamicData();


        [ForeignKey("CharacterId")]
        public virtual Character Character { get; set; }


    }


    public class EquipDynamicData
    {
        // 随机属性词条
        public Dictionary<int, float> Affixes { get; set; } = new Dictionary<int, float>();
        // 宝石孔ID
        public List<int> Gems { get; set; } = new List<int>();
    }
}
