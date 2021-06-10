using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Http;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramProducts;
using TelegramWebHook.Models;

namespace TelegramWebHook.Controllers
{
    public class TelegramGroupController : ApiController
    {

        [HttpPost]
        [Route("api/TelegramGroup")]
        public async Task<HttpResponseMessage> Post(Update update)
        {
            try
            {

                await CheckMessage(update, update.Type != UpdateType.EditedMessage);
                return new HttpResponseMessage(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                SendAndSaveLog(ex.Message);
                return new HttpResponseMessage(HttpStatusCode.OK);
            }

        }
        [HttpGet]
        [Route("api/TelegramGroup")]
        public string Get()
        {
            WebApiApplication.Telegram.GetValue();
            return JsonConvert.SerializeObject(WebApiApplication.Telegram.GroupOptions);
        }


        public static async void SendText(Update e, string text)
        {
            WebApiApplication.Telegram.Bot.SendChatActionAsync(e.Message.Chat.Id, ChatAction.Typing);
            await WebApiApplication.Telegram.Bot.SendTextMessageAsync(e.Message.Chat.Id, text, ParseMode.Default, false, true);
            //await Telegram.Bot.PinChatMessageAsync(e.Message.Chat.Id, message.MessageId, true);
        }
        private async Task CheckMessage(Update e, bool newMessage)
        {
            try
            {
                if (DisableIfNotGroup(e))
                {
                    return;
                }

                var list = WebApiApplication.Telegram.GroupOptions.Where(m => m.GroupId == e.Message.Chat.Id);
                var groupModes = list as IList<GroupMode> ?? list.ToList();
                if (!groupModes.Any())
                {
                    SaveInfo(e);
                    SendAndSaveLog("New bot assigned" + e.Message.Chat.Title);
                    await WebApiApplication.Telegram.Bot.SendTextMessageAsync(e.Message.Chat.Id,
                       Properties.Settings.Default.MessageTempActivation, disableNotification: true);
                    await CheckMessage(e, false);
                    return;
                }

                IsTyping(e);
                GroupMode groupOptions = groupModes.First();
                //Telegram.Bot.SendTextMessageAsync(236200826, groupOptions.GroupName);
                if (groupOptions.Status == Status.Disabled)
                {
                    await WebApiApplication.Telegram.Bot.SendTextMessageAsync(e.Message.Chat.Id, Properties.Settings.Default.MessageDeactivated);
                    await WebApiApplication.Telegram.Bot.LeaveChatAsync(e.Message.Chat.Id);
                    return;
                }

                if (e.Message.From.Id == 236200826)
                {
                    int BotKick;
                    if (int.TryParse(e.Message.Text, out BotKick))
                    {
                        await WebApiApplication.Telegram.Bot.KickChatMemberAsync(e.Message.Chat.Id, BotKick);
                    }
                    return;
                }


                //if (!WebApiApplication.Telegram.Chats.ContainsKey(e.Message.Chat.Id))
                //{
                //    WebApiApplication.Telegram.Chats.Add(e.Message.Chat.Id, new ChatDetail()
                //    {
                //        ChatId = e.Message.Chat.Id,
                //    });
                //}
                var id = e.Message.From.Id;

                if (IsAdmin(e, id))
                {
                    return;
                }
                //TimerForMessage(e, groupOptions);

                //SendAndSaveLog("admin got");
                //Better Not to delete this kind of message
                if (e.Message.NewChatMembers != null)
                {

                    foreach (User messageNewChatMember in e.Message.NewChatMembers)
                    {

                        if (messageNewChatMember?.Username == null)
                        {
                            return;
                        }

                        if (messageNewChatMember.Username.EndsWith("bot") ||
                            messageNewChatMember.Username.EndsWith("Bot") ||
                            messageNewChatMember.IsBot)
                        {
                            if (groupOptions.KickBot)
                            {
                                WebApiApplication.Telegram.Bot.KickChatMemberAsync(e.Message.Chat.Id, messageNewChatMember.Id);
                            }

                            if (groupOptions.KickUserAddedBot)
                            {
                                WebApiApplication.Telegram.Bot.KickChatMemberAsync(e.Message.Chat.Id, e.Message.From.Id);
                            }
                        }

                    }
                    if (groupOptions.DeleteNewChatMember)
                    {
                        DeleteMessage(e);
                    }

                    return;
                }

                if (e.Message.ForwardFromChat?.Type == ChatType.Channel && groupOptions.RemoveChannelPostForward)
                {
                    DeleteMessage(e);
                    return;
                }


                if (groupOptions.RemoveChannelPostForward &&
                    e.Message.ForwardFrom.Username.ToLower().EndsWith("bot"))
                {
                    DeleteMessage(e);
                    return;
                }


                // SendAndSaveLog(groupOptions.NightTime.ToString() + groupOptions.GroupName);
                if (groupOptions.NightTime)
                {
                    if (NightTime(e, groupOptions))
                    {
                        return;
                    }
                }

                if (InappropriateMessage(e, groupOptions))
                {
                    return;
                }
                if (e.Message.LeftChatMember.IsBot)
                    DeleteMessage(e);
                if (newMessage && groupOptions.MessageExceed)
                {
                    if (MessageExceed(e, id, groupOptions))
                    {
                        return;
                    }
                }
                
            }
            catch (Exception ex)
            {
                await WebApiApplication.Telegram.Bot.SendTextMessageAsync(236200826, ex.Message + "\n" + ex.StackTrace.ToString());
            }
        }
        private bool MessageExceed(Update e, int id, GroupMode grp)
        {

            ChatDetail detail = WebApiApplication.Telegram.Chats[e.Message.Chat.Id];
            //SendAndSaveLog("Message exceed method"+e.Message.Chat.Title);
            if (detail.Messages.ContainsKey(id))
            {
                //SendAndSaveLog("Message Exceed id="+id+" "+grp.MessageCount+grp.MessageExceed+ " "+detail.Messages[id]);
                if (detail.Messages[id] >= grp.MessageCount)
                {
                    DeleteMessage(e);
                    return true;
                }
                detail.Messages[id] = detail.Messages[id] + 1;
            }
            else
            {
                detail.Messages.Add(id, 1);
            }
            return false;
        }
        public static void SendAndSaveLog(string text)
        {
            WebApiApplication.Telegram.Bot.SendTextMessageAsync(236200826, text);
        }
        private void IsTyping(Update e)
        {
            WebApiApplication.Telegram.Bot.SendChatActionAsync(e.Message.Chat.Id, ChatAction.Typing);
        }

        private bool InappropriateMessage(Update e, GroupMode grp)
        {
            if (grp.DeleteUrl)
            {
                if (e.Message.Entities != null && e.Message.Entities.Any(
                    messageEntity => messageEntity.Type == MessageEntityType.Url || messageEntity.Type == MessageEntityType.TextLink
                                     ))
                {
                    DeleteMessage(e);
                    return true;
                }
            }

            if (e.Message.ReplyToMessage != null && grp.DeleteReply)
            {
                DeleteMessage(e);
                return true;
            }
            if (grp.MessageTypes.All(m => m != e.Message.Type))
            {
                DeleteMessage(e);
                return true;
            }
            //delete links
            if (e.Message.Text != null)
            {
                if (CheckForText(e.Message.Text, e, grp))
                {
                    return true;
                }
            }
            if (e.Message.Caption != null)
            {
                if (CheckForText(e.Message.Caption, e, grp))
                {
                    return true;
                }
            }
            return false;
        }
        public static int CurrentHour => int.Parse(DateTime.Now.ToString("HH"));
        public static int CurrentMinute => DateTime.Now.Minute;

        private bool NightTime(Update e, GroupMode grp)
        {
            // SendAndSaveLog("night time");
            if (CurrentHour < grp.ToHour)
            {

                DeleteMessage(e);
                return true;
            }
            if (CurrentHour == grp.ToHour && CurrentMinute <= grp.ToMinute)
            {
                DeleteMessage(e);
                return true;
            }
            return false;
        }

        private bool IsAdmin(Update e, long id)
        {
            var chat = WebApiApplication.Telegram.Chats[e.Message.Chat.Id];
            //if (chat.ReceivingAdmins)
            //   return false;
            //if (chat.AdminList.Count == 0)
            //{
            //    SendAndSaveLog("getting admins for " + e.Message.Chat.Title);
            //    //chat.ReceivingAdmins = true;
            //    ChatMember[] chatMembers =await Telegram.Bot.GetChatAdministratorsAsync(e.Message.Chat.Id);
            //    //chat.ReceivingAdmins = false;
            //    SendAndSaveLog("got admins for " + e.Message.Chat.Title);
            //    foreach (ChatMember member in chatMembers)
            //        chat.AdminList.Add(member.User.Id);
            //}

            return chat.AdminList.Any(m => m == id);
        }

        private void TimerForMessage(Update e, GroupMode group)
        {

            //if (!Telegram.FirstTime.Contains(e.Message.Chat.Id))
            //{
            //    Telegram.FirstTime.Add(e.Message.Chat.Id);
            //    TextNight(e, group);
            //}
        }

        private bool DisableIfNotGroup(Update e)
        {
            if (e.Message?.Chat?.Type == null)
            {
                return true;
            }

            if (e.Message?.Chat?.Type == ChatType.Private)
            {
                WebApiApplication.Telegram.Bot.SendTextMessageAsync(e.Message.Chat.Id,
                    Properties.Settings.Default.Message);
            }

            return e.Message?.Chat?.Type == ChatType.Private || e.Message?.Chat?.Type == ChatType.Channel;
        }

        private void DeleteMessage(Update e)
        {
            WebApiApplication.Telegram.Bot.DeleteMessageAsync(e.Message.Chat.Id, e.Message.MessageId);
            //  WebApiApplication.Telegram.Bot.DeleteMessageAsync(e.Message.Chat.Id, e.Message.MessageId);   
        }

        /*   public async void TextNight(Update e, GroupMode group)
           {
               int minutesRemaining = (0 - CurrentHour) * 60 + (32 - CurrentMinute);
               if (minutesRemaining < 0)
                   minutesRemaining += (int)new TimeSpan(1, 0, 0, 0).TotalMinutes;
               SendAndSaveLog("night for "+e.Message.Chat.Title+" "+minutesRemaining);
               await Task.Delay((int)new TimeSpan(0, minutesRemaining, 0).TotalMilliseconds);
               var text = new StringBuilder();
               text.AppendLine(group.TextPin)
               .AppendLine(e.Message.Chat.Title);

                SendText(e, text.ToString());
               //SendConfig();
               WebApiApplication.Telegram.GetValue();
               if (group.Status == Status.Expired)
                    SendText(e, Properties.Settings.Default.MessageExpired);
               //SaveInfo(e);
               WebApiApplication.Telegram.Chats.Clear();
               WebApiApplication.Telegram.GetValue();
               await Task.Delay(5 * 60 * 1000);
               TextNight(e, group);
           }*/

        private async void SaveInfo(Update e)
        {
            string filepath = TelegramBotProduct.ResolvePath(Telegram.FileName);
            WebApiApplication.Telegram.GroupOptions.Add(new GroupMode()
            {
                GroupName = e.Message.Chat.Title,
                DeleteReply = true,
                DeleteUrl = true,
                GroupId = e.Message.Chat.Id,
                KickBot = true,
                Status = Status.Enabled,
                KickUserAddedBot = true,
                MaxLength = 800,
                MessageCount = 5,
                MessageExceed = false,
                MessageLength = true,
                MessageTypes = new[]
                        {
                            MessageType.Text,
                            MessageType.Photo,
//                            MessageType.Audio,
//                            MessageType.Document,
//                            MessageType.Sticker,
//                            MessageType.Video,
//                            MessageType.VideoNote,
//                            MessageType.Voice,
                        },
                ToDate = CurrentDate(DateTime.Now.AddDays(2))
            });
            Groups group = new Groups();
            group.GroupSettings = WebApiApplication.Telegram.GroupOptions;
            string output = JsonConvert.SerializeObject(group);
            using (StreamWriter writer = new StreamWriter(filepath, false))
            {
                writer.Write(output);
            }
            //System.IO.File.WriteAllText(filepath, output);/
            WebApiApplication.Telegram.GetValue();
            WebApiApplication.Telegram.AdminGet(WebApiApplication.Telegram.GroupOptions.First(m => m.GroupId == e.Message.Chat.Id));
        }
        public string CurrentDate(DateTime dt)
        {
            PersianCalendar pc = new PersianCalendar();
            return $"{pc.GetYear(dt)}/{pc.GetMonth(dt)}/{pc.GetDayOfMonth(dt)}";
        }
        private bool CheckForText(string text, Update e, GroupMode grp)
        {
            if (grp.DeleteUrl && (text.Contains("@") || IsUrlValid(text) ||
                text.ToLower().Contains("http")))

            {
                DeleteMessage(e);
                return true;
            }
            if (grp.MessageLength)
            {
                if (text.Length > grp.MaxLength || text.Length < grp.MinLength)
                {

                    DeleteMessage(e);
                    return true;
                }
            }

            return false;
        }

        private string pattern = @"/(https?:\/\/(?:www\.|(?!www))[a-zA-Z0-9][a-zA-Z0-9-]+[a-zA-Z0-9]\.[^\s]{2,}|www\.[a-zA-Z0-9][a-zA-Z0-9-]+[a-zA-Z0-9]\.[^\s]{2,}|https?:\/\/(?:www\.|(?!www))[a-zA-Z0-9]\.[^\s]{2,}|www\.[a-zA-Z0-9]\.[^\s]{2,})/g";
        private bool IsUrlValid(string url)
        {
            Regex reg = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            return reg.IsMatch(url);
        }

    }
}

