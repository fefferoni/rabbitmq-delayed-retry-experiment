using System;
using System.Collections.Generic;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Consumer
{
    class Consumer
    {
        const string WORK_EXCHANGE_NAME = "work_exchange";
        const string WORK_QUEUE_NAME = "work_queue";
        const string RETRY_EXCHANGE_NAME = "retry_exchange";
        const string RETRY_QUEUE_NAME = "retry_queue";

        static void Main(string[] args)
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.ExchangeDeclare(exchange: WORK_EXCHANGE_NAME, type: ExchangeType.Fanout);
                channel.ExchangeDeclare(exchange: RETRY_EXCHANGE_NAME, type: ExchangeType.Direct);

                channel.QueueDeclare(queue: WORK_QUEUE_NAME, durable: false);
                channel.QueueBind(queue: WORK_QUEUE_NAME, exchange: WORK_EXCHANGE_NAME, routingKey: "");

                // Declare the retry queue, set its DLX and bind it to the retry exchange
                var queueArgs = new Dictionary<string, object>();
                queueArgs.Add("x-dead-letter-exchange", WORK_EXCHANGE_NAME);
                var retryQueue = channel.QueueDeclare(queue: RETRY_QUEUE_NAME, arguments: queueArgs);
                channel.QueueBind(queue: RETRY_QUEUE_NAME, exchange: RETRY_EXCHANGE_NAME, routingKey: "retry");

                Console.WriteLine("Waiting for messages.");

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body;
                    var message = Encoding.UTF8.GetString(body);

                    Console.WriteLine("Received {0}", message);

                    if (message.Length <= 11)
                    {
                        // Simulate fail
                        Console.WriteLine("FAIL {0}", message);

                        var props = ea.BasicProperties;
                        int retryCount = 1;
                        if (props.Headers != null)
                        {
                            if (props.Headers.ContainsKey("x-death"))
                            {
                                var headers = (List<object>)props.Headers["x-death"];
                                var dlxHeaders = (Dictionary<string, object>)headers[0];
                                if (dlxHeaders.ContainsKey("count"))
                                {
                                    retryCount = (int)(long)dlxHeaders["count"] + 1;
                                }
                            }
                        }

                        if (retryCount <= 5)
                        {
                            props.Expiration = (retryCount * 1500).ToString();

                            Console.WriteLine("RETRY #{0} EXPIRATION {1}ms", retryCount, props.Expiration);

                            channel.BasicPublish(exchange: RETRY_EXCHANGE_NAME,
                                                                routingKey: "retry",
                                                                basicProperties: props,
                                                                body: body);
                        }
                        else
                        {
                            Console.WriteLine("Retried 5 times -> Discarding {1}", retryCount, message);

                        }
                    }   
                };
                channel.BasicConsume(queue: WORK_QUEUE_NAME,
                                     autoAck: true,
                                     consumer: consumer);

                Console.WriteLine(" Press [enter] to exit.");
                Console.ReadLine();
            }
        }
    }
}
