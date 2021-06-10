using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Timers;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramWebHook.EF;
using TelegramWebHook.Models;
using TelegramWebHook.Properties;

namespace TelegramWebHook.Controllers
{
    public class ChatDetail
    {
        public long ChatId { get; set; }
        public List<long> AdminList
        {
            get; set;
        }
        public Dictionary<long, int> Messages { get; set; }
        // public bool ReceivingAdmins { get; set; }
        public ChatDetail()
        {
            AdminList = new List<long>();
            Messages = new Dictionary<long, int>();
        }
    }

    public class Telegram
    {
        public TelegramBotClient Bot { get; set; }
        public Dictionary<long, ChatDetail> Chats { get; set; }
        public List<GroupMode> GroupOptions { get; set; }
        public List<long> FirstTime { get; set; }

        public Timer Timer { get; set; }
        public const string FileName = "Groups.json";
        public Telegram()
        {
            if (GroupOptions == null)
            {
                GroupOptions = new List<GroupMode>();
            }

            if (Chats == null)
            {
                Chats = new Dictionary<long, ChatDetail>();
            }

            if (FirstTime == null)
            {
                FirstTime = new List<long>();
            }

            if (Bot == null)
            {
                Bot = new TelegramBotClient("your Bot key");
                Bot.SetWebhookAsync("your webhook!");
            }
            if (Timer == null)
            {
                Timer = new Timer();
            }

            //Bot.StartReceiving();
            //Bot.OnMessage += Bot_OnMessage;
        }

        public void AdminGet(GroupMode groupOption)
        {
            try
            {
                if (Chats[groupOption.GroupId].AdminList.Count == 0)
                {
                    TelegramGroupController.SendAndSaveLog("getting admins for " + groupOption.GroupName);
                    ChatMember[] chatMembers = Bot.GetChatAdministratorsAsync(groupOption.GroupId).Result;
                    TelegramGroupController.SendAndSaveLog("got admins for " + groupOption.GroupName);
                    foreach (ChatMember member in chatMembers)
                    {
                        Chats[groupOption.GroupId].AdminList.Add(member.User.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                Bot.SendTextMessageAsync(236200826, ex.Message);
            }
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs elapsed)
        {
            try
            {
                WebApiApplication.Telegram.Timer.Stop();
                TelegramGroupController.SendAndSaveLog("timer elapsed");
                WebApiApplication.Telegram.Timer.Interval = new TimeSpan(1, 0, 0, 0).TotalMilliseconds;
                WebApiApplication.Telegram.Timer.Start();
                GetValue();
                CalculateDate();
                TelegramGroupController.SendAndSaveLog(WebApiApplication.Telegram.GroupOptions.Count.ToString());
                foreach (GroupMode group in WebApiApplication.Telegram.GroupOptions)
                {
                    try
                    {
                        TelegramGroupController.SendAndSaveLog(group.GroupName);
                        if (group.Status == Status.Disabled)
                        {
                            continue;
                        }

                        var text = new StringBuilder();
                        if (group.TextPin != null)
                        {
                            text.AppendLine(group.TextPin)
                                .AppendLine(group.GroupName);
                            WebApiApplication.Telegram.Bot.SendTextMessageAsync(group.GroupId, text.ToString(), disableNotification: true);
                            //WebApiApplication.Telegram.Bot.PinChatMessageAsync(group.GroupId, message.MessageId, true);
                        }
                        //TelegramGroupController.SendText(e, text.ToString());
                        if (group.Status == Status.Expired)
                        {
                            WebApiApplication.Telegram.Bot.SendTextMessageAsync(group.GroupId, Properties.Settings.Default.MessageExpired, disableNotification: true);
                        }
                        //TelegramGroupController.SendText(e, Properties.Settings.Default.MessageExpired);
                    }
                    catch (Exception ex)
                    {
                        TelegramGroupController.SendAndSaveLog(ex.Message + "\n" + ex);
                    }

                }
                foreach (KeyValuePair<long, ChatDetail> telegramChat in WebApiApplication.Telegram.Chats)
                {
                    telegramChat.Value.Messages.Clear();
                }

            }
            catch (Exception ex) { TelegramGroupController.SendAndSaveLog(ex.Message + "\n" + ex.InnerException?.Message); }
        }

        private void Bot_OnMessage(object sender, global::Telegram.Bot.Args.MessageEventArgs e)
        {
            TelegramGroupController t = new TelegramGroupController();
            Update update = new Update();
            update.Message = e.Message;
            t.Post(update);
        }

        public void GetValue()
        {
            try
            {
                var groups = LoadJson();
                if (groups == null)
                {
                    TelegramGroupController.SendAndSaveLog("Group is null");
                    return;
                }
                try
                {
                    using (MyDbContext myDbContext = new MyDbContext())
                    {
                        if (!myDbContext.GroupModels.Any())
                        {
                            TelegramGroupController.SendAndSaveLog("Saving into database");
                                myDbContext.GroupModels.AddRange(groups.GroupSettings);
                            myDbContext.SaveChanges();
                            // myDbContext.GroupModels.AddRange(groups.GroupSettings);
                        }
                    }
                }
                catch(Exception ex)
                {
                    TelegramGroupController.SendAndSaveLog(ex.Message+"data");
                }
                WebApiApplication.Telegram.GroupOptions.Clear();
                WebApiApplication.Telegram.GroupOptions.AddRange(groups.GroupSettings);
                CalculateDate();
                TelegramGroupController.SendAndSaveLog("GetValuesMethod finished");
            }
            catch (Exception ex)
            {
                TelegramGroupController.SendAndSaveLog(ex.Message);
            }
        }

        private static void CalculateDate()
        {
            try
            {
                PersianCalendar pc = new PersianCalendar();
                foreach (GroupMode grp in WebApiApplication.Telegram.GroupOptions)
                {
                    string[] date = grp.ToDate.Split('/');
                    var dt = new DateTime(int.Parse(date[0]),
                        int.Parse(date[1]),
                        int.Parse(date[2]),
                        pc);
                    TimeSpan t = dt - DateTime.Now;
                    if (t.Days <= -2)
                    {
                        grp.Status = Status.Disabled;
                    }
                    else if (t.Days <= 1)
                    {
                        grp.Status = Status.Expired;
                    }
                    else
                    {
                        grp.Status = Status.Enabled;
                    }
                }
            }
            catch (Exception ex)
            {
                TelegramGroupController.SendAndSaveLog(ex.Message);
            }
        }

        public static Groups LoadJson()
        {
            using (StreamReader r = new StreamReader(TelegramProducts.TelegramBotProduct.ResolvePath(FileName)))
            {
                string json = r.ReadToEnd();
                return JsonConvert.DeserializeObject<Groups>(json);
            }
        }

        public void Start()
        {
            GetValue();
            foreach (GroupMode groupOption in GroupOptions)
            {
                if (!Chats.ContainsKey(groupOption.GroupId))
                {
                    Chats.Add(groupOption.GroupId, new ChatDetail()
                    {
                        ChatId = groupOption.GroupId,
                    });
                }
                AdminGet(groupOption);
            }
            int minutesRemaining = (Properties.Settings.Default.Hour - TelegramGroupController.CurrentHour) * 60 + (Settings.Default.Minute - TelegramGroupController.CurrentMinute);
            if (minutesRemaining < 0)
            {
                minutesRemaining += (int)new TimeSpan(1, 0, 0, 0).TotalMinutes;
            }

            Timer.Interval = minutesRemaining * 60 * 1000;
            TelegramGroupController.SendAndSaveLog("interval in " + Timer.Interval);
            Timer.Start();
            Timer.Elapsed += Timer_Elapsed;
        }
    }
}