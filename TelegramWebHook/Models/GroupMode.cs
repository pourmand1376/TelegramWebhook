using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Security.Policy;
using System.Web;
using Telegram.Bot.Types.Enums;

namespace TelegramWebHook.Models
{
    
    public class Groups
    {
        public IList<GroupMode> GroupSettings { get; set; }
    }
    public class GroupMode
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long GroupId { get; set; }
        [MaxLength(100)]
        public string GroupName { get; set; }
        public bool KickBot { get; set; }
        public bool KickUserAddedBot { get; set; }
        public bool RemoveChannelPostForward { get; set; }
        public bool NightTime { get; set; }
        public int ToHour { get; set; } 
        public int ToMinute { get; set; } 
        public bool DeleteUrl { get; set; } 
        public bool DeleteReply { get; set; } 
        public IList<MessageType> MessageTypes { get; set; }
        public bool MessageLength { get; set; } 
        public int MinLength { get; set; } 
        public int MaxLength { get; set; } 
        public bool MessageExceed { get; set; } 
        public int MessageCount { get; set; }
        [MaxLength(50)]
        public string ToDate { get; set; } 
        public Status Status { get; set; }
        public bool DeleteNewChatMember { get; set; }
        [MaxLength(500)]
        public string TextPin { get; set; }
    }
   
    public enum Status:byte
    {
        Enabled = 0,
        Expired = 1,
        Disabled = 2
    }
}