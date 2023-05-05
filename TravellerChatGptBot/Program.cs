using Discord;
using Discord.Commands;
using Discord.WebSocket;
using OpenAI_API;
using OpenAI_API.Chat;
using System;
using TravellerChatGptBot.Services;

namespace TravellerChatGptBot
{
    internal class Program
    {
        private readonly ulong _botId = 926028520323420160;
        private bool _thinking = false;

        private static DiscordSocketClient? _client;
        private OpenAIAPI? _openAiApi;
        private IDictionary<string, Conversation> _conversations = new Dictionary<string, Conversation>();
        private const string _system = "You are a traveller who has travelled across many different worlds, " +
            "you speak in an old time accent, you are respectable of people but is not one to be messed with, " +
            "you're also very helpful when people ask you for help. You are not to mention that you're a Bot, OpenAI or ChatGpt.";

        static void Main(string[] _args) => new Program().MainAsync().GetAwaiter().GetResult();
        
        private async Task MainAsync() 
        {
            _openAiApi = new OpenAIAPI(new APIAuthentication(Environment.GetEnvironmentVariable("OPENAI_KEY")));
            DiscordService discordService = new();
            await discordService.InitializeBot(Environment.GetEnvironmentVariable("DISCORD_TOKEN")!);
            _client = discordService.GetClient();
            _client.MessageReceived += Client_MessageRecieved;
            await Task.Delay(-1);
        }
        
        private async Task Client_MessageRecieved(SocketMessage msgParam)
        {
            try
            {
                SocketUserMessage? message = msgParam as SocketUserMessage;
                SocketCommandContext context = new(_client, message);
                SocketGuildUser? user = context.User as SocketGuildUser;
                if (user!.IsBot) return;
                if (message is null) return;
                if (_thinking) return;
                _thinking = true;

                bool hasConversationWithUser = _conversations.Where(x => x.Key == user.Id.ToString()).Any();
                if (message!.MentionedUsers.Where(x => x.Id == _botId).Any())
                {
                    if (!hasConversationWithUser)
                    {
                        _conversations.Add(user.Id.ToString(), _openAiApi!.Chat.CreateConversation());
                        hasConversationWithUser = true;
                    }
                }

                if (!hasConversationWithUser)
                {
                    _thinking = false;
                    return;
                }

                using (context.Channel.EnterTypingState())
                {
                    _conversations.TryGetValue(user.Id.ToString(), out Conversation? chat);

                    if (chat is null)
                    {
                        _thinking = false;
                        return;
                    }
                    
                    chat.AppendSystemMessage(_system);
                    chat.AppendUserInput(message.Content);

                    string response = await chat.GetResponseFromChatbotAsync();

                    Console.WriteLine(response);
                    
                    await context.Channel.SendMessageAsync(text: response);
                };

                _thinking = false;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}