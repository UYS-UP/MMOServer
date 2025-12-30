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

        [Column("itemId")]
        public int ItemId { get; set; }

        [Column("count")]
        public int Count { get; set; }

        [Column("slotIndex")]
        public int SlotIndex { get; set; }

        [Column("metaData")]
        public string MetaData { get; set; }

        [ForeignKey("CharacterId")]
        public virtual Character Character { get; set; }
    }
}
