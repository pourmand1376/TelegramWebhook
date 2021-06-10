
using System.Data.Entity;
using System.Data.Entity.Migrations;
using TelegramWebHook.Models;

namespace TelegramWebHook.EF
{
    public class MyDbContext : DbContext
    {
        public MyDbContext() : base("Default")
        {
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<MyDbContext, TelegramWebHook.EF.Configuration>());
        }
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            
            base.OnModelCreating(modelBuilder);
        }

      //  public DbSet<Account> Accounts { get; set; }
        //public DbSet<ActivityFieldType> ActivityFieldTypes { get; set; }
        public DbSet<GroupMode> GroupModels { get; set; }

    }
    public class Configuration : DbMigrationsConfiguration<MyDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
            AutomaticMigrationDataLossAllowed = true;
            ContextKey = "DataContext";
        }
        protected override void Seed(MyDbContext context)
        {
            //context.Accounts.AddOrUpdate();
            //context.ActivityFieldTypes.AddOrUpdate(new ActivityFieldType
            //{
            //    Id = 1,
            //    Name = "صنعتي"
            //},
            //new ActivityFieldType
            //{
            //    Id = 2,
            //    Name = "فروشگاهي"
            //},
            //new ActivityFieldType
            //{
            //    Id = 3,
            //    Name = "هتداري"
            //},
            //new ActivityFieldType
            //{
            //    Id = 4,
            //    Name =
            //    "رستوران"
            //},
            //new ActivityFieldType
            //{
            //    Id = 5,
            //    Name = "توليدي"
            //}, new ActivityFieldType
            //{
            //    Id = 6,
            //    Name = "بازرگاني"
            //});
            base.Seed(context);
        }
    }
}