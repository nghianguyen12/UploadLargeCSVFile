
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

public class RabbitMqPublisher
{
    private readonly string _hostname;
    private readonly string _queueName;
    private readonly string _username;
    private readonly string _password;

    public RabbitMqPublisher(string hostname, string queueName, string username, string password)
    {
        _hostname = hostname;
        _queueName = queueName;
        _username = username;
        _password = password;
    }

    public async Task PublishJobAsync(string jobId)
    {
        var factory = new ConnectionFactory()
        {
            HostName = "localhost",
            UserName = "guest",
            Password = "guest"
        };
        var endpoints = new System.Collections.Generic.List<AmqpTcpEndpoint> {
            new AmqpTcpEndpoint("localhost")
            };
        IConnection conn = await factory.CreateConnectionAsync(endpoints);
        IChannel channel = await conn.CreateChannelAsync();

        await channel.QueueDeclareAsync(  queue: "jobs",
            durable: true,
            exclusive: false,
            autoDelete: false);

        var body = Encoding.UTF8.GetBytes(jobId);
        var props = new BasicProperties();
        await channel.BasicPublishAsync(
           "", "jobs", false, props, body);
    }
}

