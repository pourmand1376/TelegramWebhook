#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

using TelegramWebHook.Properties;
using File = System.IO.File;

#endregion

namespace TelegramProducts
{
    public class TelegramBotProduct
    {
        private readonly ITelegramBotClient _bot;
        private const string HandsDown = "👇";
        private const string FolderName = "Folders.json";
        public const string ManagerName = "Managers.json";
        private const string Create = "/Create";
        private const string DeleteLink = "/DeleteLink";
        private const string CreateLink = "/CreateLink";
        private const string Edit = "/Edit";
        private const string Delete = "/Delete";
        private const string AddMessage = "/AddMessage";
        public const string DeleteMessage = "/DeleteMessage";
        private const char DeleteSeparator = '$';
        private Folder _folders;
        private ManagerList _managers;
        private bool ReceivingMessage = false;
        public string Enable = "ReceivingMessage";
        private Message Message;
        public TelegramBotProduct()
        {
            _bot = new TelegramBotClient("your bot key!");
            _bot.SetWebhookAsync("web hookaddress");
            //_bot.DeleteWebhookAsync();
            //_bot.StartReceiving();
            //_bot.OnUpdate += (sender, args) =>
            //{
            //    _bot_OnUpdate(args.Update);
            //};
        }
        public void Start()
        {
            //_bot.StartReceiving();
            //_bot.OnUpdate += _bot_OnUpdate;
            _folders = LoadJson<Folder>((FolderName));
            _managers = LoadJson<ManagerList>((ManagerName));
        }
        public long ChatId(Update update) =>
             update.Message?.Chat.Id ?? update.CallbackQuery.Message.Chat.Id;
        public async Task _bot_OnUpdate(Update update)
        {
            try
            {
                switch (update.Type)
                {
                    case UpdateType.Message:
                        if (IsMananger(update) && ReceivingMessage)
                            Message = update.Message;
                        var reply=CheckReply(update);
                        CreateButton(update);
                        break;
                    case UpdateType.CallbackQuery:
                        var tuple = CheckQueries(update);
                        if (!tuple.Item1)
                           tuple = CreateButton(update);
                        if (!ReceivingMessage || !IsMananger(update))await LoadSubItems(update);
                       await _bot.AnswerCallbackQueryAsync(update.CallbackQuery.Id,tuple.Item2);
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        private Tuple<bool,string> CheckReply(Update update)
        {
            
            if (update.Message?.ReplyToMessage != null)
            {
                string text = update.Message.ReplyToMessage.Text ?? update.Message.ReplyToMessage.Caption;
                if (text == null) return new Tuple<bool, string>(false,null);
                string[] array = text.Split('\n');
                if (text.Contains(DeleteSeparator))
                {
                    string[] separated = text.Split(DeleteSeparator);
                    if (separated.Length == 3)
                    {
                        int subItemId = int.Parse(separated[1]);
                        string subItemPath = separated[2];
                        SubItemList subItemList = LoadJson<SubItemList>(AppendExtension(subItemPath));
                        var subItem = subItemList?.SubItems?.Find(p => p.Id == subItemId);
                        if (subItem != null)
                        {
                            if (subItemList.SubItems.Remove(subItem))
                            {
                                SaveJson(subItemList,AppendExtension(subItemPath));
                            }
                        }
                    }
                    return new Tuple<bool, string>(true, "پیغام حذف شد");
                }
                
                if (array[0] == Settings.Default.CreateNewButton)
                {
                    var path = GetPathWithoutCommand(array[1]);
                    var tuple = GetFolderByPath(path);
                    var folder = tuple.Item1;
                    int id = 0;
                    if (folder.SubFolders != null && folder.SubFolders?.Count != 0)
                    {
                        id = folder.SubFolders.Max(f => f.Id);
                        folder.SubFolders.Add(new Folder
                        {
                            Id = id + 1,
                            Name = update.Message.Text,
                        });
                    }
                    else
                    {
                        folder.SubFolders = new List<Folder>()
                        {
                            new Folder
                            {
                                Id = 0,
                                Name = update.Message.Text
                            }
                        };
                    }
                    SaveJson(_folders, FolderName);
                    return new Tuple<bool, string>(true, "کلید ساخته شد");
                }
                if (array[0] == Settings.Default.EditButton)
                {
                    var path = GetPathWithoutCommand(array[1]);
                    var tuple = GetFolderByPath(path);
                    var folder = tuple.Item1;
                    folder.Name = update.Message.Text;
                    SaveJson(_folders, FolderName);
                    return new Tuple<bool, string>(true, "کلید ویرایش شد.");
                }
                if (array[0] == Settings.Default.CreateNewLinkButton)
                {
                    var path = GetPathWithoutCommand(array[1]);
                    var tuple = GetFolderByPath(path);
                    var folder = tuple.Item1;
                    folder.Url = update.Message.Text;
                    SaveJson(_folders, FolderName);
                    return new Tuple<bool, string>(true, "ادرس کلید اضافه شد.");
                }
            }
           return new Tuple<bool, string>(false,null);
        }
        private Tuple<bool,string> CheckQueries(Update update)
        {
            
            if (update.CallbackQuery.Data == Enable)
            {
                update.CallbackQuery.Data = "0";
                ReceivingMessage = !ReceivingMessage;
                string name = ReceivingMessage ? "حالت مدیر فعال شد" : "حالت مدیر غیرفعال شد";
                return new Tuple<bool, string>(false,name);
            }
            if (update.CallbackQuery.Data.EndsWith(Create))
            {
                _bot.SendTextMessageAsync(ChatId(update), Settings.Default.CreateNewButton + "\n" + update.CallbackQuery.Data, replyMarkup: new ForceReplyMarkup() {Selective = false});
                return new Tuple<bool, string>(true, "ایجاد کلید");
            }
            if (update.CallbackQuery.Data.EndsWith(CreateLink))
            {
                _bot.SendTextMessageAsync(ChatId(update), Settings.Default.CreateNewLinkButton + "\n" + update.CallbackQuery.Data, replyMarkup: new ForceReplyMarkup() {Selective = false});
                return new Tuple<bool, string>(true,"ایجاد کلید لینک دار");
            }
            if (update.CallbackQuery.Data.EndsWith(Edit))
            {
                _bot.SendTextMessageAsync(ChatId(update), Settings.Default.EditButton + "\n" + update.CallbackQuery.Data, replyMarkup: new ForceReplyMarkup() {Selective = false});
                return new Tuple<bool, string>(true,"ویرایش کلید");
            }
            if (update.CallbackQuery.Data.EndsWith(DeleteLink))
            {
                var path = GetPathWithoutCommand(update.CallbackQuery.Data);
                var tuple = GetFolderByPath(path);
                var folder = tuple.Item1;
                Folder f = folder?.SubFolders?.First(fo => fo.Url != null);
                if(f==null)return new Tuple<bool, string>(true,"ایتمی وجود ندارد.");
                 folder.SubFolders.Remove(f);
                SaveJson(_folders, FolderName);
                return new Tuple<bool, string>(true,"حذف شد");
            }
            if (update.CallbackQuery.Data.EndsWith(Delete))
            {
                string name="";
                try
                {
                    var path = GetPathWithoutCommand(update.CallbackQuery.Data);
                    SubItemList sub = LoadJson<SubItemList>(AppendExtension(path));
                    if (sub?.SubItems?.Count > 0)
                    {
                        return new Tuple<bool, string>(true,"ابتدا پیغام های این کلید را پاک کنید.");
                    }
                    var fatherpath = FatherPath(path);
                    var tuple = GetFolderByPath(fatherpath.Item1);
                    var folder = tuple.Item1;
                    Folder f = folder.SubFolders.First(temp => temp.Id == fatherpath.Item2);
                    if (f != null)
                    {
                        folder.SubFolders.Remove(f);
                        name = "کلید حذف شد";
                        SaveJson(_folders, FolderName);

                    }
                    update.CallbackQuery.Data = "0";
                    //_bot.DeleteMessageAsync(ChatId(update), update.CallbackQuery.Message.MessageId);
                }
                catch
                {
                    name = "خطا";
                }
                return new Tuple<bool, string>(false,name);
            }
            #region AddMessage
            if (update.CallbackQuery.Data.EndsWith(AddMessage) && ReceivingMessage && Message != null)
            {
                try
                {
                    var path = GetPathWithoutCommand(update.CallbackQuery.Data);
                    SubItemList sub = LoadJson<SubItemList>(AppendExtension(path)) ?? new SubItemList();
                    if (sub.SubItems == null) sub.SubItems = new List<SubItem>();
                    int id = 0;
                    if (sub.SubItems.Count > 0)
                        id = sub.SubItems.Max(subitem => subitem.Id);
                    sub.SubItems.Add(new SubItem
                    {
                        Id = id + 1,
                        Audio = Message.Audio?.FileId,
                        Document = Message.Document?.FileId,
                        Latitude = Message.Location?.Latitude,
                        Longitude = Message.Location?.Longitude,
                        PhotoSize = Message.Photo?[Message.Photo.Length-1]?.FileId,
                        Sticker = Message.Sticker?.FileId,
                        Text = Message.Text ?? Message.Caption ?? "",
                        Voice = Message.Voice?.FileId,
                        Video = Message.Video?.FileId,
                        VideoNote = Message.VideoNote?.FileId,

                    });
                    SaveJson(sub, AppendExtension(path));
                    _bot.DeleteMessageAsync(ChatId(update), update.CallbackQuery.Message.MessageId);
                    return new Tuple<bool, string>(true,"فایل ذخیره شد.");
                }
                catch
                {
                    return new Tuple<bool, string>(false,"خطا");
                }
            }
            #endregion addmessage

            if (update.CallbackQuery.Data.EndsWith(DeleteMessage))
            {
                var path = GetPathWithoutCommand(update.CallbackQuery.Data);
                update.CallbackQuery.Data = path;
                LoadSubItems(update, true);
            }
            return new Tuple<bool, string>(false,null);
        }
        private string GetPathWithoutCommand(string data)
        {
            var split = data.Split('/');
            return split[0];
        }
        public bool IsMananger(Update update)
        {
            var from = 0;
            if (update.Type == UpdateType.CallbackQuery)
                from = update.CallbackQuery.From.Id;
            else if (update.Type == UpdateType.Message)
                from = update.Message.From.Id;
            return _managers.Managers.Any(manager => manager.Id == from);
        }
        public string AppendExtension(string data) => data + ".prd";
        private async Task<Tuple<bool,string>> LoadSubItems(Update eUpdate,bool deleteMode=false)
        {
            var data = eUpdate.CallbackQuery.Data;
            var item = LoadJson<SubItemList>(AppendExtension(data));
            if (item == null)
            {
                return new Tuple<bool, string>(false,null);
            }
            string folder = GetFolderByPath(data).Item2;
            string text = $"{HandsDown} {folder} {HandsDown}";
            await _bot.SendTextMessageAsync(ChatId(eUpdate),text,ParseMode.Markdown);
            foreach (SubItem subItem in item.SubItems)
            {
//                if (deleteMode) subItem.Text += +'\n'+ DeleteSeparator.ToString() + subItem.Id.ToString() + DeleteSeparator.ToString() + data;
//                    if (subItem.Audio != null)
//                     await _bot.SendAudioAsync(ChatId(eUpdate), new FileToSend
//                    {
//                        FileId = subItem.Audio
//                    }, subItem.Text, 0, "", "");
//                else if (subItem.Document != null)
//                   await _bot.SendDocumentAsync(ChatId(eUpdate), new FileToSend
//                    {
//                        FileId = subItem.Document
//                    }, subItem.Text);
//                else if (subItem.Latitude != null && subItem.Longitude != null)
//                   await _bot.SendLocationAsync(ChatId(eUpdate), (float)subItem.Latitude, (float)subItem.Longitude);
//                else if (subItem.PhotoSize != null)
//                   await _bot.SendPhotoAsync(ChatId(eUpdate), new FileToSend
//                    {
//                        FileId = subItem.PhotoSize
//                    }, subItem.Text);
//                else if (subItem.Sticker != null)
//                   await _bot.SendStickerAsync(ChatId(eUpdate), new FileToSend { FileId = subItem.Sticker });
//                else if (subItem.Video != null)
//                   await _bot.SendVideoAsync(ChatId(eUpdate), new FileToSend { FileId = subItem.Video },
//                        0, 0,
//                        0, subItem.Text);
//                else if (subItem.VideoNote != null)
//                   await _bot.SendVideoNoteAsync(ChatId(eUpdate), new FileToSend { FileId = subItem.VideoNote }
//                        );
//                else if (subItem.Voice != null)
//                   await _bot.SendVoiceAsync(ChatId(eUpdate), new FileToSend { FileId = subItem.Voice }, subItem.Text
//                        
//                    );
//                else if (!string.IsNullOrWhiteSpace(subItem.Text)) await  _bot.SendTextMessageAsync(ChatId(eUpdate), subItem.Text, ParseMode.Markdown);
            }
            string myData = FatherPath(eUpdate.CallbackQuery.Data).Item1;
            eUpdate.CallbackQuery.Data = myData;
            await _bot.DeleteMessageAsync(ChatId(eUpdate), eUpdate.CallbackQuery.Message.MessageId);
            CreateButton(eUpdate, true);
            return new Tuple<bool, string>(true,null);
        }
        private Tuple<bool,string> CreateButton(Update update, bool newMessage = false)
        {
            var value = update.CallbackQuery != null
                ? update.CallbackQuery.Data
                : "0";
            var tuple = LoadButtons(value, IsMananger(update));
            var replyMarkup = tuple.Item1;
            if (replyMarkup == null) return new Tuple<bool, string>(false,null);
//            if (update.CallbackQuery == null || newMessage)
//                _bot.SendTextMessageAsync(ChatId(update), Settings.Default.WelcomeMessage,
//                    replyMarkup: replyMarkup);
//            else
//                _bot.EditMessageTextAsync(ChatId(update), update.CallbackQuery.Message.MessageId,
//                    Settings.Default.WelcomeMessage, replyMarkup: replyMarkup);
            return new Tuple<bool, string>(true,tuple.Item2);
        }
        public Tuple<IReplyMarkup,string> LoadButtons(string path, bool manager)
        {
            var buttons = new List<InlineKeyboardButton[]>();
            var tuple = GetFolderByPath(path);
            var current =tuple.Item1;
            if ((current?.SubFolders == null) && !manager)
                return null;
            if (current?.SubFolders != null)
                foreach (var currentSubFolder in current.SubFolders)
                    if(currentSubFolder.Url==null)
                    AddButtonToList(buttons,currentSubFolder.Name,path+"-"+currentSubFolder.Id);
                    else  AddButtonToList(buttons,currentSubFolder.Name,currentSubFolder.Url,true);
            
            if (path != "0")
            {
                var temp = FatherPath(path);
                AddButtonToList(buttons,"برگشت",temp.Item1);
            }
            if (manager)
            {
                if (path == "0")
                    AddButtonToList(buttons, "حالت مدیر",Enable);
            }
            if (manager&&ReceivingMessage)
            {
                AddButtonToList(buttons,"اضافه کردن کلید", path + Create);
                AddButtonToList(buttons,"حذف کلید دارای لینک",path+DeleteLink);
                if (path != "0")
                {
                    AddButtonToList(buttons, "ویرایش کردن کلید", path + Edit);
                    AddButtonToList(buttons, "اضافه کردن لینک", path + CreateLink);
                }
                if ((buttons.Count == 5) && (path != "0"))
                {
                    AddButtonToList(buttons,"حذف کردن کلید", path + Delete);
                    AddButtonToList(buttons, "قرار دادن پیغام", path + AddMessage);
                    AddButtonToList(buttons, "حذف پیغام", path + DeleteMessage);
                }
                
            }
            return 
                new Tuple<IReplyMarkup, string>
                (new InlineKeyboardMarkup(buttons.ToArray()),
                tuple.Item2);
        }

        public void AddButtonToList(List<InlineKeyboardButton[]> buttons, string name,string path,bool url=false)
        {
            buttons.Add(!url
                ? new[] {InlineKeyboardButton.WithCallbackData(StringBeautifier(name), path),}
                : new[] {InlineKeyboardButton.WithUrl(StringBeautifier(name), path),});
        }
        private static Tuple<string,int> FatherPath(string path)
        {
            var index = path.LastIndexOf('-');
            return new Tuple<string, int>(path.Substring(0, index)
                , int.Parse(path.Substring(index + 1, path.Length - index - 1))); 
        }
        public string StringBeautifier(string value)
        {
            return $"« {value} »";
        }
        private Tuple<Folder,string> GetFolderByPath(string path)
        {
            StringBuilder st = new StringBuilder();
            if (path.EndsWith(Create) || path.EndsWith(Edit) || path.EndsWith(Delete))
                return null;
            var pathes = path.Split('-');
            var current = _folders;
            for (var index = 1; index < pathes.Length; index++)
            {
                var value = pathes[index];
                try
                {
                    current = current.SubFolders.First(m => m.Id == int.Parse(value));
                    st.Append(current.Name);
                    if (index + 1 < pathes.Length) st.Append(" « ");
                }
                catch(Exception ex)
                {
                    return new Tuple<Folder, string>(current,"خطایی رخ داد");
                }
            }
            return new Tuple<Folder, string>(current,st.ToString());
        }
        public static string ResolvePath(string fileName)
        {
            string path = "~/App_Data/" + fileName;
            return HttpContext.Current?.Server.MapPath(path) ??
                   HostingEnvironment.MapPath(path);
            // return Path.Combine("App_Data", fileName);
        }
        public const long AmirPourmandId = 236200826;
        public void SendDataToMe(string data)
        {
            try
            {
                _bot.SendTextMessageAsync(AmirPourmandId, data);
            }
            catch
            {
            }
        }
        public  static  void SaveJson<T>(T json, string fileName) where T : class
        {
            try
            {
                string output = JsonConvert.SerializeObject(json);
                using (StreamWriter writer = new StreamWriter(ResolvePath(fileName),false))
                {
                    writer.Write(output);
                }
            }
            catch 
            {
                
            }
        }
        public static T  LoadJson<T>(string fileName) where T : class
        {
            try
            {
                using (var reader = new StreamReader(ResolvePath(fileName)))
                {
                    var output = reader.ReadToEnd();
                    return JsonConvert.DeserializeObject<T>(output);
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}