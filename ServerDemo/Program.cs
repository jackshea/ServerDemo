using DotNetty.Handlers.Logging;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ServerDemo
{
    class Program
    {
        private static int port;

        private static readonly AutoResetEvent _closingEvent = new AutoResetEvent(false);
        private static IChannel _boundChannel;

        public static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder().SetBasePath(AppDomain.CurrentDomain.BaseDirectory).AddJsonFile("appsettings.json");
            var configuration = builder.Build();
            port = Convert.ToInt32(configuration["port"]);
            var p = GetArg("-p", args);
            if (!string.IsNullOrEmpty(p))
            {
                port = Convert.ToInt32(p);
            }

            RunServerAsync().Wait();
            Console.WriteLine("Bye!");
        }

        static async Task RunServerAsync()
        {
            IEventLoopGroup bossGroup = new MultithreadEventLoopGroup(1);
            IEventLoopGroup workerGroup = new MultithreadEventLoopGroup();

            Console.WriteLine("Server starting...");

            try
            {
                var bootstrap = new ServerBootstrap();
                bootstrap.Group(bossGroup, workerGroup);
                bootstrap.Channel<TcpServerSocketChannel>();

                bootstrap
                    .Option(ChannelOption.SoBacklog, 100)
                    .Handler(new LoggingHandler("SRV-LSTN"))
                    .ChildHandler(new ActionChannelInitializer<IChannel>(channel =>
                    {
                        IChannelPipeline pipeline = channel.Pipeline;
                        pipeline.AddLast(new LoggingHandler("SRV-CONN"));
                        pipeline.AddLast(new MessageHandler());
                    }));

                _boundChannel = await bootstrap.BindAsync(port);
                Console.WriteLine($"listening on port : {port}");

                Console.WriteLine("Press Ctrl + C to cancel!");
                Console.CancelKeyPress += (s, a) =>
                {
                    Console.WriteLine("Press Ctrl + C!");
                    _closingEvent.Set();
                };

                _closingEvent.WaitOne();
                await _boundChannel.CloseAsync();
            }
            finally
            {
                await Task.WhenAll(
                    bossGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1)),
                    workerGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1)));
            }
        }

        private static string GetArg(string argName, string[] args)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == argName)
                {
                    return args[i + 1];
                }
            }

            return null;
        }
    }
}
