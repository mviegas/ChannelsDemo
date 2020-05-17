using System;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ChannelsDemo
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            // Setting a read delay bigger than a wrote delay, so that we can see what happens when channels are "full"
            const int readDelay = 500;

            const int writeDelay = 100;

            // Creating a bounded channel with capacity of 1 
            var channel = Channel.CreateBounded<int>(new BoundedChannelOptions(1)
            {
                // Setting this property we say that when the channel is full and another item is dispatched to it, it should wait until the current item is processed to process the next
                FullMode = BoundedChannelFullMode.Wait
            });

            // Calling Task.Run so that the Channel.Writer executes in a different synchronization context than the Channel.Reader
            _ = Task.Run(async () =>
            {
                await TryWrite(channel, writeDelay).ConfigureAwait(false);

                // await WriteAsync(channel, writeDelay).ConfigureAwait(false);

                // await WaitToWrite(channel, writeDelay).ConfigureAwait(false);
            });

            await ReadAsync(channel, readDelay).ConfigureAwait(false);
        }

        private static async Task ReadAsync(Channel<int> channel, int delay)
        {
            while (true)
            {
                var message = await channel.Reader.ReadAsync().ConfigureAwait(false);

                ExtendedConsole.WriteLine($"Readed {message}", ConsoleColor.Green);

                await Task.Delay(delay).ConfigureAwait(false);
            }
        }

        private static async Task TryWrite(Channel<int> channel, int delay)
        {
            for (int i = 0; ; i++)
            {
                if (!channel.Writer.TryWrite(i))
                {
                    ExtendedConsole.WriteLine($"Dropping {i}", ConsoleColor.Red);
                }

                await Task.Delay(delay).ConfigureAwait(false);
            }
        }

        private static async Task WriteAsync(Channel<int> channel, int delay)
        {
            for (int i = 0; ; i++)
            {
                await channel.Writer.WriteAsync(i);
                await Task.Delay(delay).ConfigureAwait(false);
                ExtendedConsole.WriteLine($"Writing {i}", ConsoleColor.Blue);
            }
        }

        private static async Task WaitToWrite(Channel<int> channel, int delay)
        {
            int i = 1;

            while (await channel.Writer.WaitToWriteAsync(default).ConfigureAwait(false))
            {
                await channel.Writer.WriteAsync(i, default);
                await Task.Delay(delay).ConfigureAwait(false);
                ExtendedConsole.WriteLine($"Writing {i}", ConsoleColor.Blue);
                i++;
            }
        }
    }

    public static class ExtendedConsole
    {
        public static void WriteLine(string message, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }
}
