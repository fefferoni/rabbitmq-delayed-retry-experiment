using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Producer
{
    class Producer
    {
        const string WORK_EXCHANGE_NAME = "work_exchange";

        static void Main(string[] args)
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using(var connection = factory.CreateConnection())
            using(var channel = connection.CreateModel())
            {
                channel.ExchangeDeclare(exchange: WORK_EXCHANGE_NAME, type: ExchangeType.Fanout, durable: false);

                var client = new WebClient();
                for (int i = 0; i < 5; i++)
                {
                    var message = GetMessage(client);
                    var body = Encoding.UTF8.GetBytes(message);
                    channel.BasicPublish(exchange: WORK_EXCHANGE_NAME,
                                        routingKey: "",
                                        basicProperties: null,
                                        body: body);
                    Console.WriteLine("Sent {0}", message); 
                }
            }

            Console.WriteLine(" Press [enter] to exit.");
            Console.ReadLine();
        }

        private static string GetMessage(WebClient webClient)
        {
            return webClient.DownloadString("http://names.drycodes.com/1?format=text");
        }
    }
}
