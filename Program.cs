using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace DogPlus
{
    /// <summary>
    /// Main bot program class that initializes and runs the Discord bot.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Manages saving and loading server data.
        /// </summary>
        public static ServerDataManager ServerDataManagerInstance;

        /// <summary>
        /// The Discord client instance.
        /// </summary>
        private DiscordSocketClient botClient;

        /// <summary>
        /// Handles slash command interactions.
        /// </summary>
        private InteractionService interactionService;

        /// <summary>
        /// Service provider (can be used for dependency injection, currently unused).
        /// </summary>
        private IServiceProvider services;

        /// <summary>
        /// Application entry point.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        static void Main(string[] args)
        {
            new Program().MainAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Initializes and starts the bot asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task MainAsync()
        {
            // Configure the client with necessary intents.
            DiscordSocketConfig config = new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.MessageContent | GatewayIntents.GuildMembers
            };

            // Create the client and interaction service instances.
            botClient = new DiscordSocketClient(config);
            interactionService = new InteractionService(botClient);
            ServerDataManagerInstance = new ServerDataManager();
            services = null;

            // Subscribe to events.
            botClient.Log += LogAsync;
            botClient.Ready += ReadyAsync;
            botClient.UserJoined += NewUserEvent;
            botClient.MessageReceived += MessageReceivedAsync;
            botClient.InteractionCreated += InteractionCreatedAsync;

            // Register slash commands from this assembly.
            await interactionService.AddModulesAsync(typeof(SlashCommandHandler).Assembly, services);

            // Get bot token from config file.
            var json = File.ReadAllText("./config.json");
            dynamic configData = JsonConvert.DeserializeObject(json);
            string token = configData.DiscordToken;
            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("Bot token is missing.");
                return;
            }

            // Login and start the bot.
            await botClient.LoginAsync(TokenType.Bot, token);
            await botClient.StartAsync();

            // Prevent the program from exiting.
            await Task.Delay(-1);
        }

        /// <summary>
        /// Handles when an interaction is created.
        /// </summary>
        /// <param name="interaction">The interaction that was created.</param>
        /// <returns></returns>
        private async Task InteractionCreatedAsync(SocketInteraction interaction)
        {
            var context = new SocketInteractionContext(botClient, interaction);
            await interactionService.ExecuteCommandAsync(context, services);
        }

        /// <summary>
        /// Logs messages from the Discord client to the console.
        /// </summary>
        /// <param name="log">The log message.</param>
        /// <returns>A completed task.</returns>
        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }

        /// <summary>
        /// Called when the bot is fully connected and ready.
        /// </summary>
        /// <returns></returns>
        private async Task ReadyAsync()
        {
            // Register commands globally.
            await interactionService.RegisterCommandsGloballyAsync();

            Console.WriteLine($"Bot is ready! Logged in as {botClient.CurrentUser.Username}#{botClient.CurrentUser.Discriminator}");
        }

        /// <summary>
        /// Handles incoming messages from guild channels.
        /// </summary>
        /// <param name="message">The received message.</param>
        /// <returns></returns>
        private async Task MessageReceivedAsync(SocketMessage message)
        {
            if (message.Author.IsBot)
                return;

            SocketGuildChannel guildChannel = message.Channel as SocketGuildChannel;
            if (guildChannel == null)
                return;

            SocketUserMessage userMessage = message as SocketUserMessage;
            if (userMessage == null)
                return;

            SocketGuildUser user = message.Author as SocketGuildUser;
            if (user == null)
                return;

            var guild = guildChannel.Guild;
            ulong guildId = guild.Id;

            Console.WriteLine($"Message from {message.Author.Username}: [{message.Content}] Server {guild.Name}");

            DiscordServerData serverEntry = ServerDataManagerInstance.GetOrCreateServerData(guildId);

            var userRoleIds = user.Roles.Select(role => role.Id);
            bool hasWatchedRole = userRoleIds.Any(id => serverEntry.RolesWatched.Contains(id));

            SocketGuildUser botUser = guild.CurrentUser;
            string contentLower = userMessage.Content.ToLowerInvariant();

            if (hasWatchedRole && !serverEntry.IsUserAdmin(guild, user.Id))
            {
                // Check for bad words.
                foreach (string bannedWord in serverEntry.BadWords)
                {
                    if (contentLower.Contains(bannedWord.ToLowerInvariant()))
                    {
                        var channelPerms = botUser.GetPermissions(guildChannel);
                        if (!channelPerms.ManageMessages)
                            return;

                        await message.DeleteAsync();
                        await serverEntry.RecordBadWordUsageAsync(guild, user.Id);

                        string warnMessage = $"{user.Mention} used a banned word and their message was removed.";
                        await message.Channel.SendMessageAsync(warnMessage);

                        // Exit after deleting the message.
                        return;
                    }
                }
            }

            // Check for reaction permission.
            var reactionPerms = botUser.GetPermissions(guildChannel);
            if (!reactionPerms.AddReactions)
            {
                Console.WriteLine($"{guild.Name} has not given Dog permission to add reactions");
                return;
            }

            // No bad words, so add reaction.
            if (Extensions.rng.Next(0, 101) <= serverEntry.EmojiReactionChance)
            {
                IEmote emoji = EmojiManager.GetRandomEmoji(guild);
                await message.AddReactionAsync(emoji);
            }
        }

        /// <summary>
        /// On a new user joined, adds a default role to them.
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        private async Task NewUserEvent(SocketGuildUser user)
        {
            var serverData = ServerDataManagerInstance.GetOrCreateServerData(user.Guild.Id);
            Console.WriteLine($"New user joined: {user.Username}, DefaultRoles count: {serverData.DefaultRoles?.Count}");

            if (serverData.DefaultRoles == null || serverData.DefaultRoles.Count == 0)
            {
                Console.WriteLine("No default roles set.");
                return;
            }

            SocketGuildUser bot = user.Guild.CurrentUser;

            // Check if bot has manage roles permission.
            if (!bot.GuildPermissions.ManageRoles)
            {
                Console.WriteLine("Bot does not have the manage roles permission.");
                return;
            }

            foreach (ulong roleId in serverData.DefaultRoles)
            {
                var role = user.Guild.GetRole(roleId);
                if (role != null)
                {
                    Console.WriteLine($"Trying to add role {role.Name} to user {user.Username}");
                    try
                    {
                        await user.AddRoleAsync(role);
                        Console.WriteLine($"Added role {role.Name} to user {user.Username}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to add role {role.Name} to user {user.Username}: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"Role ID {roleId} not found in guild.");
                }
            }
        }
    }
}
