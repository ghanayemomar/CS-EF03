using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Transactions;

namespace EF003.DapperAndTransactions
{
    class Program
    {
        public static void Main()
        {
            DapterUsingDynamicAndTyped();
            Console.ReadKey();
        }
        public static void DapterUsingDynamicAndTyped()
        {
            var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

            IDbConnection db = new SqlConnection(configuration.GetSection("constr").Value);

            var sql = "SELECT * FROM WALLETS";

            Console.WriteLine("-------- using Dynamic Query-------");
            var resultAsDynamic = db.Query<dynamic>(sql);
            foreach (var item in resultAsDynamic)
                Console.WriteLine(item);

            Console.WriteLine("-------- using Typed Query -------");
            var wallets = db.Query<Wallet>(sql);
            foreach (var wallet in wallets)
            {
                Console.WriteLine(wallet);
            }
        }
        public static void SimpleInsert()
        {
            var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
            IDbConnection db = new SqlConnection(configuration.GetSection("constr").Value);

            var walletToInsert = new Wallet { Holder = "Sarah", Balance = 10000m };
            var sql = "INSERT INTO Wallets (Holder, Balance)" +
                "Values (@Holder,@Balance)";

            db.Execute(sql, new
            {
                Holder = walletToInsert.Holder,
                Balance = walletToInsert.Balance,
            });

        }
        public static void InsertStatmentReturnId()
        {
            var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
            IDbConnection db = new SqlConnection(configuration.GetSection("constr").Value);

            var walletToInsert = new Wallet { Holder = "Ayman", Balance = 170000m };
            var sql = "INSERT INTO Wallets (Holder, Balance)" +
                        "Values (@Holder,@Balance)" +
                        "SELECT CAST(SCOPE_IDENTITY() AS INT)";
            var parameters = new
            {
                Holder = walletToInsert.Holder,
                Balance = walletToInsert.Balance
            };
            walletToInsert.Id = db.Query<int>(sql, parameters).Single();

        }
        public static void ExcuteDeleteStatment()
        {
            var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
            IDbConnection db = new SqlConnection(configuration.GetSection("constr").Value);

            var walletToDelete = new Wallet { Id = 9 };
            var sql = "DELETE FROM Wallets WHERE Id = @Id";

            var parameters = new
            {
                Id = walletToDelete.Id,
            };
            db.Execute(sql, parameters);

        }

        public static void ExcuteMultipleQueryInOneBatch()
        {
            var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
            IDbConnection db = new SqlConnection(configuration.GetSection("constr").Value);

            var sql = "SELECT MIN(Balance) FROM Wallets;" +
                "SELECT MAX(Balance) FROM Wallets;";

            var multi = db.QueryMultiple(sql);

            Console.WriteLine(
                $"MIN = {multi.ReadSingle<decimal>()}" +
                $"\nMax = {multi.ReadSingle<decimal>()}");
            //or
            //multi = db.QueryMultiple(
            //   $"MIN = {multi.Read<decimal>().Single()}" +
            //   $"\nMax = {multi.Read<decimal>().Single()}");

        }

        public static void DapperWithTransaction()
        {
            var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
            IDbConnection db = new SqlConnection(configuration.GetSection("constr").Value);

            //Transfer 2000
            // from: id =8 to id = 4
            decimal amountToTranfer = 2000m;
            using (var transactionScope = new TransactionScope())
            {
                var walletFrom = db.QuerySingle<Wallet>("SELECT * FROM Wallets Where Id = @Id", new { Id = 8 });
                var walletTo = db.QuerySingle<Wallet>("SELECT * FROM Wallets Where Id = @Id", new { Id = 4 });


                db.Execute("UPDATE Wallets Set Balance = @Balance Where Id = @Id",
                    new
                    {
                        Id = walletFrom.Id,
                        Balance = walletFrom.Balance - amountToTranfer,
                    }
                    );
                db.Execute("UPDATE Wallets Set Balance = @Balance Where Id = @Id",
                   new
                   {
                       Id = walletTo.Id,
                       Balance = walletTo.Balance + amountToTranfer
                   }
                   );
                transactionScope.Complete();

            }





        }

    }
}