using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using Telegram.Bot.Types;

namespace TelegramWebHook.Controllers
{
    public class TelegramProductsController : ApiController
    {
        [System.Web.Mvc.Route("api/TelegramProducts")]
        [System.Web.Mvc.HttpPost]
        public async Task<HttpResponseMessage> Post(Update update)
        {
            await WebApiApplication.Product._bot_OnUpdate(update);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}