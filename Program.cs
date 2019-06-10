using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace discord_soundboard
{
    public class Program
    {
        public static void Main(string[] args)
        {
            new Program().DiscordLoop().GetAwaiter().GetResult();
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();

        private DiscordSocketClient _client;
        
        public async Task DiscordLoop()
        {
            _client = new DiscordSocketClient();
            _client.Log += Log;
            _client.Ready += () => JoinChannel();

            await _client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("DISCORD_TOKEN"));
            await _client.StartAsync();
        }
        
        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
        
        public async Task JoinChannel(IVoiceChannel channel = null)
        {
            channel = channel ?? (_client.Guilds.First().Owner as IGuildUser)?.VoiceChannel;
            if (channel == null) { return; }

            var audioClient = await channel.ConnectAsync();
            await SendAsync(audioClient, @"media\philippe_edit.ogg");
        }
        
        private Process CreateStream(string path)
        {
            return Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true,
            });
        }
        
        private async Task SendAsync(IAudioClient client, string path)
        {
            using (var ffmpeg = CreateStream(path))
            using (var output = ffmpeg.StandardOutput.BaseStream)
            using (var discord = client.CreatePCMStream(AudioApplication.Mixed))
            {
                try { await output.CopyToAsync(discord); }
                finally { await discord.FlushAsync(); }
            }
        }
    }
}