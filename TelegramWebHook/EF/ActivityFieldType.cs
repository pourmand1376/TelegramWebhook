using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace TelegramWebHook.EF
{
    public class ActivityFieldType
    {
        public int Id { get; set; }
        [MaxLength(50)]
        public string Name { get; set; }
        public IList<Account> Accounts { get; set; }
    }
}