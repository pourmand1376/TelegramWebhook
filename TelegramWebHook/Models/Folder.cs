using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace TelegramProducts
{
    class Folder
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public List<Folder> SubFolders { get; set; }
    }

    class SubItemList
    {
        public List<SubItem> SubItems { get; set; }
    }
    class SubItem
    {
        public int Id { get; set; }
        public string Text { get; set; }
       
        public string Url { get; set; }
        public string Audio { get; set; }
        public string Document { get; set; }
        public string PhotoSize { get; set; }
        public float? Latitude { get; set; }
        public float? Longitude { get; set; }
        public string Video { get; set; }
        public string VideoNote { get; set; }
        public string Voice { get; set; }
        public string Sticker { get; set; }
       
    }
}
