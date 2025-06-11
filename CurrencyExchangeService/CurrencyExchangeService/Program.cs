using System;
using System.IO;
using System.ServiceModel;
using System.ServiceModel.Description;

namespace CurrencyExchangeService
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔════════════════════════════════════╗");
            Console.WriteLine("║    CURRENCY EXCHANGE WCF SERVICE   ║");
            Console.WriteLine("╚════════════════════════════════════╝");
            Console.ResetColor();
            
            Console.WriteLine("Initializing service...");
            
            try
            {
                // Set the database path for Entity Framework
                string appDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data");
                if (!Directory.Exists(appDataPath))
                {
                    Directory.CreateDirectory(appDataPath);
                }
                AppDomain.CurrentDomain.SetData("DataDirectory", appDataPath);
                
                Console.WriteLine($"Database will be created at: {Path.Combine(appDataPath, "CurrencyExchangeDb.mdf")}");
                
                // Create the ServiceHost - configuration is loaded from App.config
                using (ServiceHost host = new ServiceHost(typeof(CurrencyExchangeService)))
                {
                    // Display service endpoint information
                    Console.WriteLine("\nService Endpoints:");
                    foreach (ServiceEndpoint endpoint in host.Description.Endpoints)
                    {
                        Console.WriteLine($"- {endpoint.Address} ({endpoint.Binding.Name})");
                    }
                    
                    // Open the ServiceHost to start listening for messages
                    try
                    {
                        host.Open();
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("\nThe Currency Exchange Service is now running!");
                        Console.ResetColor();
                        
                        foreach (var baseAddress in host.BaseAddresses)
                        {
                            Console.WriteLine($"Base address: {baseAddress}");
                        }
                        
                        Console.WriteLine("\nPress <Enter> to stop the service.");
                        Console.ReadLine();
                    }
                    catch (AddressAccessDeniedException)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("\nERROR: Access denied when trying to open the service host.");
                        Console.WriteLine("Try running the application as Administrator.");
                        Console.ResetColor();
                        Console.ReadLine();
                        return;
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"\nERROR: Failed to open the service host: {ex.Message}");
                        Console.ResetColor();
                        Console.ReadLine();
                        return;
                    }

                    // Close the ServiceHost
                    try
                    {
                        host.Close();
                        Console.WriteLine("Service closed successfully.");
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Error closing service: {ex.Message}");
                        Console.ResetColor();
                        host.Abort(); // Force abort if clean close fails
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nCritical error initializing service: {ex.Message}");
                Console.ResetColor();
                Console.ReadLine();
            }
        }
    }
}
