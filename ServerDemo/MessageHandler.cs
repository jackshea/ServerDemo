using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using System;
using System.Text;

namespace ServerDemo
{
    public class MessageHandler : ChannelHandlerAdapter
    {
        public override void ChannelRead(IChannelHandlerContext context, object message)
        {

            var buffer = message as IByteBuffer;
            if (buffer != null)
            {
                var msg = buffer.ToString(Encoding.UTF8);
                Console.Write(msg);
                context.WriteAndFlushAsync(message);
            }
        }

        public override void ChannelReadComplete(IChannelHandlerContext context)
        {
            context.Flush();
        }

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            Console.WriteLine("Exception: " + exception);
            context.CloseAsync();
        }
    }
}