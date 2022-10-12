using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Azure.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Common.Config;

namespace ServiceBusPublisher
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

        // the sender used to publish messages to the topic
        static ServiceBusSender sender;

        static async Task Main()
        {
            Console.WriteLine("Enter APP Config Link");
            uri = Console.ReadLine();

            Console.WriteLine("Input Config Routing");
            routing = Console.ReadLine();

            var busConfig = getConfig(uri, routing);

            Console.WriteLine($"Now writing to {busConfig.TopicName}");

            client = new ServiceBusClient(busConfig.ConnectionString);
            sender = client.CreateSender(busConfig.TopicName);

            bool endMessaging = false;

            Console.WriteLine($"Please enter the message to be added to topic {busConfig.TopicName}");
            Console.WriteLine("Type 'END' to stop sending messages");

            while (!endMessaging)
            {
                var userInput = Console.ReadLine();

                if (userInput == "END")
                {
                    endMessaging = true;
                }
                else
                {
                    ServiceBusMessage newMessage = new ServiceBusMessage(userInput);
                    await sender.SendMessageAsync(newMessage);
                    Console.WriteLine("Message Sent");
                }

            }
            // Calling DisposeAsync on client types is required to ensure that network
            // resources and other unmanaged objects are properly cleaned up.
            await sender.DisposeAsync();
            await client.DisposeAsync();

            Console.WriteLine("Message Sending Ended");

            Console.WriteLine("Press any key to end the application");
            Console.ReadKey();
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
