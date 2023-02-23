
using NATS.Client;

public class NatsOptions {
    public string[] Servers { get; set; }

    public int RetryCount { get; set; } = 5;


    public int PublishRetryCount { get; set; } = 5;

    /// <summary>
    /// login user name
    /// </summary>
    public string Username { get; set; }

    public string Password { get; set; }

    /// <summary>
    /// Client Name
    /// </summary>
    /// <value></value>
    public string Client { get; set; }

    public string[] Exchanges { get; set; }

    public string Token { get; set; }

    public IConnection TryConnect(NATS.Client.ConnectionFactory factory) {
        NATS.Client.Options defaultOptions = ConnectionFactory.GetDefaultOptions();
        defaultOptions.Servers = this.Servers;
        defaultOptions.MaxReconnect = this.RetryCount;
        if (!string.IsNullOrEmpty(this.Username)) {
            defaultOptions.User = this.Username;
        }
        if (!string.IsNullOrEmpty(this.Password)) {
            defaultOptions.Password = this.Password;
        }
        if (!string.IsNullOrEmpty(this.Token)) {
            defaultOptions.Token = this.Token;
        }

        return factory.CreateConnection(defaultOptions);
    }
}
