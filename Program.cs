using System;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ChannelsDemo
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            await ChannelOutOfCapacityExample();

            // await ChannelCompletedWithoutExceptionRaised();
        }

        private static async Task ChannelOutOfCapacityExample()
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
                for (int i = 0; ; i++)
                {
                    if (!channel.Writer.TryWrite(i))
                    {
                        ExtendedConsole.WriteLine($"Dropping {i}", ConsoleColor.Red);
                    }

                    await Task.Delay(writeDelay).ConfigureAwait(false);
                }
            });

            while (true)
            {
                var message = await channel.Reader.ReadAsync().ConfigureAwait(false);

                ExtendedConsole.WriteLine($"Readed {message}", ConsoleColor.Green);

                await Task.Delay(readDelay).ConfigureAwait(false);
            }
        }

        private static async Task ChannelCompletedWithoutExceptionRaised()
        {
            var channel = Channel.CreateBounded<int>(new BoundedChannelOptions(1)
            {
                FullMode = BoundedChannelFullMode.Wait
            });

            _ = Task.Run(async () =>
            {
                for (int i = 0; ; i++)
                {
                    await channel.Writer.WriteAsync(i);
                    ExtendedConsole.WriteLine($"Writing {i}", ConsoleColor.Blue);

                    if (i == 10)
                    {
                        ExtendedConsole.WriteLine($"Writer: completing channel after 10 executions", ConsoleColor.Yellow);
                        channel.Writer.TryComplete();
                    }
                }
            });

            // Using WaitToRead, no exception is raised when channel is completed, unless it is explicit passed on completion
            while (await channel.Reader.WaitToReadAsync(default).ConfigureAwait(false))
            {
                if (channel.Reader.TryRead(out int msg))
                {
                    ExtendedConsole.WriteLine($"Readed {msg}", ConsoleColor.Green);
                }
                else
                {
                    Console.WriteLine($"Message already {msg} consumed");
                }
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
