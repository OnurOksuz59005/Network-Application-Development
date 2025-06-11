using System;
using System.Data.Entity;
using System.Data;

namespace CurrencyExchangeService.Models
{
    public class CurrencyExchangeContext : DbContext
    {
        public CurrencyExchangeContext() : base("name=CurrencyExchangeDb")
        {
            // Enable migrations and create database if it doesn't exist
            Database.SetInitializer(new CreateDatabaseIfNotExists<CurrencyExchangeContext>());
            
            // Force database initialization
            try
            {
                // This will ensure the database is created
                this.Database.Initialize(force: true);
                Console.WriteLine($"Database initialized at: {AppDomain.CurrentDomain.GetData("DataDirectory")}");
                
                // Check if we can access the database
                var connectionState = this.Database.Connection.State.ToString();
                Console.WriteLine($"Database connection state: {connectionState}");
                
                // Try to open the connection if it's not already open
                if (this.Database.Connection.State != System.Data.ConnectionState.Open)
                {
                    this.Database.Connection.Open();
                    Console.WriteLine("Database connection opened successfully");
                    this.Database.Connection.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR initializing database: {ex.Message}");
                Console.WriteLine($"Connection string: {this.Database.Connection.ConnectionString}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
            }
        }

        public DbSet<ExchangeRate> ExchangeRates { get; set; }
        public DbSet<CurrencyInfo> CurrencyInfo { get; set; }
        public DbSet<QueryLog> QueryLogs { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // Configure relationships and constraints here if needed
            base.OnModelCreating(modelBuilder);
        }
    }
}
