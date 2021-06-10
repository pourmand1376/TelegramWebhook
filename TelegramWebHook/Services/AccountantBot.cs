using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramWebHook.EF;

namespace TelegramWebHook.Services
{
    public class AccountantBot
    {
        private readonly ITelegramBotClient _botSepand;
        private readonly MyDbContext _myDbContext;
        
        private List<PropertyInfo> propertyInfos = new List<PropertyInfo>();
        
        public AccountantBot()
        {
            _myDbContext = new MyDbContext();
            _botSepand = new TelegramBotClient("735965978:AAHbh0o6QGepto_JfQZlI_js9eBMj0aICDw");
            _botSepand.SetWebhookAsync("https://amirpourmand.ml/api/Accountant");
        }
        
        public async Task Bot_OnUpdate(Update update)
        {
            if (update.Type == Telegram.Bot.Types.Enums.UpdateType.Message)
            {
                if (update.Message.ReplyToMessage != null)
                {
                    UpdateProperties(update);
                }
                else
                {
                    var reply = GetReplyMarkUp();
                    string message = BuildMessageText(GetId(update));

                    await _botSepand.SendTextMessageAsync(GetId(update), message,
                        replyMarkup: reply, replyToMessageId: update.Message.MessageId);
                }
            }
            else if (update.Type == Telegram.Bot.Types.Enums.UpdateType.CallbackQuery)
            {
                //if (!_myDbContext.Accounts.Any(a => a.Id == GetId(update)))
                //{
                //    Account newa = new Account() { Id = GetId(update), Name = "", FamilyName = "" };
                //    _myDbContext.Accounts.Add(newa);
                //    _myDbContext.SaveChanges();
                //}
                
                switch (update.InlineQuery.Query)
                {
                    case "Name":
                        await SendTextAfterQuery(update,"نام");
                        break;
                    case "FamilyName":
                        await SendTextAfterQuery(update, "نام خانوادگی");
                        break;
                    case "CompanyName":
                        await SendTextAfterQuery(update, "نام شرکت");
                        break;
                    case "CellPhone":
                        await SendTextAfterQuery(update, "شماره تلفن");
                        break;
                    case "Email":
                        await SendTextAfterQuery(update, "ایمیل");
                        break;
                    case "JobTitle":
                        await SendTextAfterQuery(update, "سمت");
                        break;
                    //case "CompanyName":
                    //    _myDbContext.Entry(account).Property(x => x.CompanyName).IsModified = true;
                    //   break;
                    case "Submit":
                        // _myDbContext.Entry(account).Property(x => x.CompanyName).IsModified = true;
                        break;
                    default:
                        break;
                }
                //UpdateValues(update);
            }
        }

        private void UpdateProperties(Update update)
        {
            
        }

        private async Task SendTextAfterQuery(Update update,string val)
        {
            await _botSepand.SendTextMessageAsync(GetId(update), PleaseEnter(val), replyMarkup: new ForceReplyMarkup() { Selective = false });
        }
        
        public string PleaseEnter(string val) => $"لطفا {val} خود را وارد نمایید.";
        private void UpdateValues(Update update)
        {
            Account account = new Account() { Id = GetId(update) };
            //_myDbContext.Accounts.Attach(account);

            switch (update.InlineQuery.Query)
            {
                case "Name":
                    _myDbContext.Entry(account).Property(x => x.Name).IsModified = true;
                    break;
                case "FamilyName":
                    _myDbContext.Entry(account).Property(x => x.FamilyName).IsModified = true;
                    break;
                case "CompanyName":
                    _myDbContext.Entry(account).Property(x => x.CompanyName).IsModified = true;
                    break;
                case "CellPhone":
                    _myDbContext.Entry(account).Property(x => x.CellPhone).IsModified = true;
                    break;
                case "Email":
                    _myDbContext.Entry(account).Property(x => x.Email).IsModified = true;
                    break;
                case "JobTitle":
                    _myDbContext.Entry(account).Property(x => x.JobTitle).IsModified = true;
                    break;
                //case "CompanyName":
                //    _myDbContext.Entry(account).Property(x => x.CompanyName).IsModified = true;
                //   break;
                case "Submit":
                    // _myDbContext.Entry(account).Property(x => x.CompanyName).IsModified = true;
                    break;
                default:
                    break;
            }
            _myDbContext.SaveChanges();
        }

        private long GetId(Update update)
        {
            if (update.Type == Telegram.Bot.Types.Enums.UpdateType.Message)
            {
                return update.Message.Chat.Id;
            }
            else 
            {
                return update.CallbackQuery.From.Id;
            }
        }

        private string BuildMessageText(long id)
        {
            
            //Account account= _myDbContext.Accounts.Find(id);
            Account account = null;
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("لطفا اطلاعات خود را وارد نمایید.");
            stringBuilder.AppendLine($"نام *: "+account?.Name??"");
            stringBuilder.AppendLine($"نام خانوادگی *: {account?.FamilyName??""}" );
            stringBuilder.AppendLine($"شماره موبایل: " + (account?.CellPhone??""));
            stringBuilder.AppendLine($"نام شرکت: " + (account?.CompanyName??""));
            stringBuilder.AppendLine($"سمت: " +( account?.JobTitle??""));
            stringBuilder.AppendLine($"ایمیل: " + (account?.Email??""));
            
            return stringBuilder.ToString();

        }

        public IReplyMarkup GetReplyMarkUp()
        {
            
            List<InlineKeyboardButton[]> inlineKeyboardButtons = new List<InlineKeyboardButton[]>();

            List<InlineKeyboardButton> firstRow = new List<InlineKeyboardButton>();

            firstRow.Add(
                InlineKeyboardButton.WithCallbackData("تغییر نام خانوادگی", "FamilyName"));

            firstRow.Add(
                InlineKeyboardButton.WithCallbackData("تغییر نام", "Name")
                );
            
            List<InlineKeyboardButton> secondrow = new List<InlineKeyboardButton>();
            
            secondrow.Add(
                InlineKeyboardButton.WithCallbackData("تغییر اسم شرکت", "CompanyName"));
            secondrow.Add(
                InlineKeyboardButton.WithCallbackData("تغییر شماره موبایل", "CellPhone")
                );


            List<InlineKeyboardButton> thirdrow = new List<InlineKeyboardButton>();
            
            thirdrow.Add(
                InlineKeyboardButton.WithCallbackData("تغییر ایمیل", "Email"));
            thirdrow.Add(
                InlineKeyboardButton.WithCallbackData("تغییر سمت", "JobTitle")
                );
            List<InlineKeyboardButton> forthrow = new List<InlineKeyboardButton>();

            forthrow.Add(
                InlineKeyboardButton.WithCallbackData("ثبت نهایی", "Submit"));

            inlineKeyboardButtons.Add(firstRow.ToArray());
            inlineKeyboardButtons.Add(secondrow.ToArray());
            inlineKeyboardButtons.Add(thirdrow.ToArray());
            inlineKeyboardButtons.Add(forthrow.ToArray());

            InlineKeyboardMarkup replyMarkup = new InlineKeyboardMarkup(inlineKeyboardButtons.ToArray());
            return replyMarkup;
        }
    }
}