using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Telegram.Bot;
using TelegramProducts;
using TelegramWebHook.Controllers;
using TelegramWebHook.Services;
using Telegram = TelegramWebHook.Controllers.Telegram;

namespace TelegramWebHook
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        public static TelegramWebHook.Controllers.Telegram Telegram;
        public static TelegramBotProduct Product;
        public static SepandLock SepandLock;
        public static DataSenderClass DataSender1Controller;
        public static AccountantBot AccountantBot;
        
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            
            Telegram = new Controllers.Telegram();
            Telegram.Start();


            SepandLock = new SepandLock();
            SepandLock.Start();
            // Product = new TelegramBotProduct();
            // Product.Start();



            //DataSender1Controller = new DataSenderClass();

            // AccountantBot = new AccountantBot();
        }

        
        
    }
}
