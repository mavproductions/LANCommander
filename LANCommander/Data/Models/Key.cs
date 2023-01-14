﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace LANCommander.Data.Models
{
    [Table("Keys")]
    public class Key : BaseModel
    {
        [MaxLength(255)]
        public string Value { get; set; }
        [JsonIgnore]
        public virtual Game Game { get; set; }
        public KeyAllocationMethod AllocationMethod { get; set; }
        [MaxLength(17)]
        public string? ClaimedByMacAddress { get; set; }
        [MaxLength(15)]
        public string? ClaimedByIpv4Address { get; set; }
        [MaxLength(255)]
        public string? ClaimedByComputerName { get; set; }
        public virtual User? ClaimedByUser { get; set; }
        public DateTime? ClaimedOn { get; set; }
    }

    public enum KeyAllocationMethod
    {
        UserAccount,
        MacAddress
    }
}