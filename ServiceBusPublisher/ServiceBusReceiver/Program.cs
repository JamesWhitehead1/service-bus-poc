using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Common.Config;
using Microsoft.Extensions.Configuration;
using System;
using System.Net;
using System.Threading.Tasks;

namespace ServiceBusReceiver
{
    internal class Program
    {
        //Can be substituted for hard-coded connection string etc. or other app config
        //FORMAT REQUIRED FOR SERVICE BUS CONNECTION
        static string uri = "https://example-app-config.azconfig.io";
        static string routing = "ServiceBus:Config";
        //Azure App Config would need items in the following format:
        //ServiceBus:Config:ConnectionString
        //ServiceBus:Config:TopicName
        //ServiceBus:Config:Subscription

        // the client that owns the connection and can be used to create senders and receivers
        static ServiceBusClient client;

        static ServiceBusProcessor processor;

        static void Main(string[] args)
        {
            Console.WriteLine("Enter APP Config Link");
            uri = Console.ReadLine();

            Console.WriteLine("Input Config Routing");
            routing = Console.ReadLine();

            var busConfig = getConfig(uri, routing);

            Console.WriteLine($"Now receiving from {busConfig.TopicName}, subscription {busConfig.SubscriptionName}");
            Console.WriteLine("Press any key to stop receiving messages");

            client = new ServiceBusClient(busConfig.ConnectionString);

            var _serviceBusProcessorOptions = new ServiceBusProcessorOptions { };

            processor = client.CreateProcessor(busConfig.TopicName, busConfig.SubscriptionName, _serviceBusProcessorOptions);

            //Adds Handlers to _processor
            processor.ProcessMessageAsync += MessageHandler;
            processor.ProcessErrorAsync += ErrorHandler;

            // start processing 
            processor.StartProcessingAsync();
            Console.ReadKey();
            StopProcessor();
        }

        public static async Task StopProcessor()
        {
            //stops processing
            await processor.StopProcessingAsync();
            //disposes of any remaining artifacts
            await processor.DisposeAsync();
            await client.DisposeAsync();
        }

        // handle received messages
        public static async Task MessageHandler(ProcessMessageEventArgs args)
        {
            string body = args.Message.Body.ToString();
            Console.WriteLine($"Received: {body}");
            await args.CompleteMessageAsync(args.Message);
        }

        // handle any errors when receiving messages
        public static Task ErrorHandler(ProcessErrorEventArgs args)
        {
            Console.WriteLine(args.Exception.ToString());
            return Task.CompletedTask;
        }


        public static ServiceBusConfig getConfig(string uri, string route)
        {
            IConfiguration config = new ConfigurationBuilder()
                .AddAzureAppConfiguration(options =>
                {
                    var credential = new DefaultAzureCredential();

                    options.Connect(new Uri(uri), credential);

                    options.ConfigureKeyVault(kv =>
                    {
                        kv.SetCredential(new DefaultAzureCredential());
                    });
                })
                .Build();

            var newConfig = new ServiceBusConfig();
            config.GetSection(route).Bind(newConfig);

            return newConfig;
        }
    }
}
