﻿using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DiscordBot.Services
{
    public class CommandHandlingService
    {
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;
        private readonly IConfiguration _config;
        private readonly IServiceProvider _provider;

        public CommandHandlingService(IServiceProvider provider)
        {
            _discord = provider.GetRequiredService<DiscordSocketClient>();
            _commands = provider.GetRequiredService<CommandService>();
            _provider = provider;
            _config = provider.GetRequiredService<IConfiguration>();
        }

        public async Task InitializeAsync(IServiceProvider provider)
        {
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _provider);
            // Add additional initialization code here...
            _discord.MessageReceived += HandleCommandAsync;
            _commands.CommandExecuted += OnCommandExecutedAsync;
        }

        private async Task HandleCommandAsync(SocketMessage rawMessage)
        {
            // Ignore system messages and messages from bots
            if (!(rawMessage is SocketUserMessage message)) return;
            if (message.Source != MessageSource.User) return;

            int argPos = 0;

            // set prefix from config
            char prefix = char.Parse(_config["prefix"]);
            // Ban user from server if trying to massmention or raid
            if (message.MentionedUsers.Count > 13 && !((IGuildUser)message.Author).GuildPermissions.KickMembers == true) await ((message.Author as IGuildUser)).BanAsync( 1, "raid or massmention");
            if (!message.HasMentionPrefix(_discord.CurrentUser, ref argPos) && !message.HasCharPrefix(prefix, ref argPos)) return;

            var context = new SocketCommandContext(_discord, message);
            await _commands.ExecuteAsync(context, argPos, _provider);

        }

        public async Task OnCommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            // if a command isn't found, log that info to console and exit this method
            if (!command.IsSpecified)
            {
                Console.WriteLine($"Command failed to execute!");
                return;
            }

            if (result.IsSuccess)
            {
                return;
            }

            // failure scenario, let's let the user know
            await context.Channel.SendMessageAsync($"Sorry, {context.Message.Author.Mention} something went wrong!\n" +
                $"{result.ErrorReason}");
        }
    }
}

