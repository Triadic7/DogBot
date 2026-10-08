using Discord;
using Discord.Interactions;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DogPlus
{
    public class SlashCommandHandler : InteractionModuleBase<SocketInteractionContext>
    {
        /// <summary>
        /// Manages saving and loading server data.
        /// </summary>
        private readonly ServerDataManager serverDataManager;

        public SlashCommandHandler()
        {
            this.serverDataManager = Program.ServerDataManagerInstance;
        }

        #region No param commands

        /// <summary>
        /// Displays all of the commands and how to use them, and some options. 
        /// </summary>
        /// <returns></returns>
        [SlashCommand("help", "Displays all commands and setup options")]
        public async Task HelpAsync()
        {
            EmbedBuilder embed = new EmbedBuilder()
                .WithTitle("Dog Help")
                .WithDescription(
                    "**Options and help**" +
                    "\nThese are for commands for setting up Dog on your server, and for displaying help on what certain commands do.\n" +
                    "\n**• /help** - Displays this help menu.\n" +
                    "\n**• /setup** - Sends a message with some configurable options for the server. If you haven't set this up when Dog joined your server, it's recommended you do this now.\n" +
                    "\n**• /adddefaultroles [role(s) separated by spaces, or quotes if multiple words]** - Adds the role(s) to the default users list. Any user that joins will get these roles.\n" +
                    "\n**• /removedefaultroles [role(s) separated by spaces, or quotes if multiple words]** - Removes the role(s) from the default users list.\n" +

                    "\n\n**Security commands**" +
                    "\nThis is a collection of commands you can use for security in your server. Assign admin roles using /addadminroles. Users assigned an admin role can add default roles, banned words, and assign other roles to a watch list.\n" +
                    "\n**• /addadminroles [role(s) separated by spaces, or quotes if multiple words]** - Adds the role(s) to the admin list.\n" +
                    "\n**• /removeadminroles [role(s) separated by spaces, or quotes if multiple words]** - Removes the role(s) from the admin list.\n" +
                    "\n**• /addroleswatched [role(s) separated by spaces, or quotes if multiple words]** - Adds the role(s) to the watch list.\n" +
                    "\n**• /removeroleswatched [role(s) separated by spaces, or quotes if multiple words]** - Removes the role(s) from the watch list.\n" +
                    "\n**• /addbadwords [word(s) separated by spaces, or quotes if multiple words]** - Adds the word(s) to a banned word list. Any user with a watched role will be punished if they say these words.\n" +
                    "\n**• /removebadwords [word(s) separated by spaces, or quotes if multiple words]** - Removes the word(s) from the banned word list.\n" +
                    "\n**• /viewlists** - Sends a message containing admin, default, and watched roles, along with banned words.\n" +

                    "\n\n**Misc commands**" +
                    "\nThese are some random commands you can additionally use in your server.\n" +
                    "\n**• /bark** - Makes Dog bark in your server.\n" +
                    "\n**• /dice [a number of sides on the dice]** - Rolls a dice with the amount of sides given.\n" +
                    "\n**• /dogfact** - Sends a cool fact about dogs."
                )


                .WithColor(Color.Magenta)
                .WithFooter(footer => footer.Text = $"{Context.User.Username}")
                .WithCurrentTimestamp();

            await SafeRespondAsync(embed: embed.Build());
        }

        #region Setup Command
        /// <summary>
        /// Command for sending a setup message to the users.
        /// </summary>
        /// <returns></returns>
        [SlashCommand("setup", "Displays bot setup and options")]
        public async Task SetupAsync()
        {
            var embed = BuildSetupEmbed();
            var components = BuildSetupComponents();

            await SafeRespondAsync(embed: embed, components: components.Build());
        }

        /// <summary>
        /// Sets up the pages for the setup / options message.
        /// </summary>
        /// <returns>Returns a component builder with page details.</returns>
        private ComponentBuilder BuildSetupComponents(string choice = null)
        {
            var builder = new ComponentBuilder();

            string placeholder;
            switch (choice)
            {
                case "punishment_type":
                    placeholder = "Selected: Punishment Type";
                    break;
                case "warnings_count":
                    placeholder = "Selected: Warnings Until Punishment";
                    break;
                case "emoji_use":
                    placeholder = "Selected: Dog Emoji Use";
                    break;
                case "timeout_time":
                    placeholder = "Selected: Timeout Time";
                    break;
                case "message_visibility":
                    placeholder = "Selected: Message Visibility";
                    break;
                default:
                    placeholder = "Select setup category";
                    break;
            }

            builder.WithSelectMenu("setup_category_select", new List<SelectMenuOptionBuilder>
            {
                new SelectMenuOptionBuilder("Punishment Type", "punishment_type"),
                new SelectMenuOptionBuilder("Warnings Until Punishment", "warnings_count"),
                new SelectMenuOptionBuilder("Dog Emoji Use", "emoji_use"),
                new SelectMenuOptionBuilder("Timeout Duration", "timeout_time"),
                new SelectMenuOptionBuilder("Message Visibility", "message_visibility")
            }, placeholder);

            return builder;
        }

        [ComponentInteraction("setup_category_select")]
        public async Task HandleCategorySelection(string[] selected)
        {
            string choice = selected.First();

            // Build the base components with updated placeholder
            var builder = BuildSetupComponents(choice);

            // Dynamically add the dependent dropdown based on selection.
            if (choice == "punishment_type")
            {
                builder.WithSelectMenu("setup_punishment_select", new List<SelectMenuOptionBuilder>
                {
                    new SelectMenuOptionBuilder("None", "none"),
                    new SelectMenuOptionBuilder("Timeout", "timeout"),
                    new SelectMenuOptionBuilder("Kick User", "kick"),
                    new SelectMenuOptionBuilder("Ban User", "ban")
                }, "Select punishment type");
            }
            else if (choice == "warnings_count")
            {
                builder.WithSelectMenu("setup_warnings_select", new List<SelectMenuOptionBuilder>
                {
                    new SelectMenuOptionBuilder("1", "1"),
                    new SelectMenuOptionBuilder("3", "3"),
                    new SelectMenuOptionBuilder("5", "5"),
                    new SelectMenuOptionBuilder("10", "10"),
                    new SelectMenuOptionBuilder("Unlimited", "unlimited")
                }, "Select warnings until punishment");
            }
            else if (choice == "emoji_use")
            {
                var options = new List<SelectMenuOptionBuilder>();
                for (int percent = 10; percent <= 100; percent += 10)
                {
                    options.Add(new SelectMenuOptionBuilder($"{percent}%", percent.ToString()));
                }

                builder.WithSelectMenu("setup_emoji_use", options, "Select the chance Dog uses an emoji reaction");
            }
            else if (choice == "timeout_time")
            {
                var options = new List<SelectMenuOptionBuilder>();
                for (int time = 5; time <= 55; time += 5)
                {
                    options.Add(new SelectMenuOptionBuilder($"{time} Minutes", time.ToString()));
                }

                builder.WithSelectMenu("setup_timeout_time", options, "Select how long someone is timed out for");
            }
            else if (choice == "message_visibility")
            {
                builder.WithSelectMenu("setup_message_visibility", new List<SelectMenuOptionBuilder>
                {
                    new SelectMenuOptionBuilder("Ephemeral (user specific)", "true"),
                    new SelectMenuOptionBuilder("Visible to everyone", "false")
                }, "Select if messages are visible");
            }

            await DeferAsync();
            await ModifyOriginalResponseAsync(msg =>
            {
                msg.Embed = BuildSetupEmbed();
                msg.Components = builder.Build();
            });
        }


        [ComponentInteraction("setup_punishment_select")]
        public async Task HandlePunishmentSelection(string[] selected)
        {
            string choice = selected.First();

            var guildId = Context.Guild.Id;
            var serverData = serverDataManager.GetOrCreateServerData(guildId);

            switch (choice)
            {
                case "none": serverData.SetPunishmentType(DiscordServerData.PunishmentType.None); break;
                case "timeout": serverData.SetPunishmentType(DiscordServerData.PunishmentType.Timeout); break;
                case "kick": serverData.SetPunishmentType(DiscordServerData.PunishmentType.KickUser); break;
                case "ban": serverData.SetPunishmentType(DiscordServerData.PunishmentType.BanUser); break;
            }

            serverDataManager.UpdateServerData(serverData);

            await DeferAsync();
            await ModifyOriginalResponseAsync(msg =>
            {
                msg.Embed = BuildSetupEmbed();
                msg.Components = BuildSetupComponents("punishment_type").Build();
            });
        }

        /// <summary>
        /// Sets up the selection for warnings.
        /// </summary>
        /// <param name="selected">The selection of punishments.</param>
        /// <returns></returns>
        [ComponentInteraction("setup_warnings_select")]
        public async Task HandleWarningsSelection(string[] selected)
        {
            string choice = selected.First();

            var guildId = Context.Guild.Id;
            var serverData = serverDataManager.GetOrCreateServerData(guildId);

            // Set bad word count.
            serverData.SetBadWordCount(choice == "unlimited" ? 99999 : int.Parse(choice));

            serverDataManager.UpdateServerData(serverData);

            await DeferAsync();
            await ModifyOriginalResponseAsync(msg =>
            {
                msg.Embed = BuildSetupEmbed();
                msg.Components = BuildSetupComponents("warnings_count").Build();
            });
        }

        [ComponentInteraction("setup_emoji_use")]
        public async Task HandleEmojiSelection(string[] selected)
        {
            string choice = selected.First();

            var guildId = Context.Guild.Id;
            var serverData = serverDataManager.GetOrCreateServerData(guildId);

            serverData.SetEmojiUsage(int.Parse(choice));
            serverDataManager.UpdateServerData(serverData);

            await DeferAsync();
            await ModifyOriginalResponseAsync(msg =>
            {
                msg.Embed = BuildSetupEmbed();
                msg.Components = BuildSetupComponents("emoji_use").Build();
            });
        }

        [ComponentInteraction("setup_timeout_time")]
        public async Task HandleTimeoutTimeSelection(string[] selected)
        {
            string choice = selected.First();

            var guildId = Context.Guild.Id;
            var serverData = serverDataManager.GetOrCreateServerData(guildId);

            // Save timeout as minutes
            serverData.SetTimoutTime(TimeSpan.FromMinutes(int.Parse(choice)));
            serverDataManager.UpdateServerData(serverData);

            await DeferAsync();
            await ModifyOriginalResponseAsync(msg =>
            {
                msg.Embed = BuildSetupEmbed();
                msg.Components = BuildSetupComponents("timeout_time").Build();
            });
        }

        [ComponentInteraction("setup_message_visibility")]
        public async Task HandleMessageVisibilitySelection(string[] selected)
        {
            string choice = selected.First();

            var guildId = Context.Guild.Id;
            var serverData = serverDataManager.GetOrCreateServerData(guildId);

            serverData.SetMessageVisability(bool.Parse(choice));
            serverDataManager.UpdateServerData(serverData);

            await DeferAsync();
            await ModifyOriginalResponseAsync(msg =>
            {
                msg.Embed = BuildSetupEmbed();
                msg.Components = BuildSetupComponents("message_visibility").Build();
            });
        }


        /// <summary>
        /// Builds the setup embed.
        /// </summary>
        /// <param name="page">The page to display on the embed.</param>
        /// <returns>Returns an embed with the given page displayed.</returns>
        private Embed BuildSetupEmbed()
        {
            var embed = new EmbedBuilder()
                .WithTitle("Bot Setup & Options")
                .WithColor(Color.Orange)
                .WithFooter(footer => footer.Text = $"{Context.User.Username}")
                .WithCurrentTimestamp();

            var server = serverDataManager.GetOrCreateServerData(Context.Guild.Id);

            embed.WithDescription("**Punishment Settings**\n" +
                                          "Use the dropdown below to configure punishment preferences.\n\n" +
                                          "**Current Settings**\n" +
                                          $"Punishment: {server.Punishment}\n" +
                                          $"Warnings until punishment: {server.MaxBannedWordsSaid}\n" +
                                          $"Emoji use: {server.EmojiReactionChance}%\n" +
                                          $"Timeout duration: {server.TimeoutTime}\n" +
                                          $"Message visability: {(server.AreMessagesEphemeral ? "User specific" : "Everyone")}\n"
                                          );

            return embed.Build();
        }
        #endregion

        /// <summary>
        /// Makes dog bark.
        /// </summary>
        /// <returns></returns>
        [SlashCommand("bark", "Makes Dog bark")]
        public async Task BarkAsync()
        {
            string[] barks =
            {
                 "Arf!",
                 "Bark!",
                 "AWOOOO!",
                 "*sniffs you*",
                 "Woof!",
                 "Yip!",
                 "Bork bork!",
                 "Wuff!",
                 "Bow wow!",
                 "ARF ARF!!",
                 "WOOF WOOF!!",
                 "YAP YAP YAP!",
                 "Blep!",
                 "SQUIRREL!!",
                 "*runs in circles*",
                 "*soft snore*",
                 "*yawns*",
                 "huff..."
            };

            string response = "🐶 " + barks.RandomElement();
            await SafeRespondAsync(embed: BuildEmbed(null, response, Color.Blue));
        }

        /// <summary>
        /// Gets a random fact about dogs.
        /// </summary>
        /// <returns></returns>
        [SlashCommand("dogfact", "Replies with a random dog fact.")]
        public async Task DogFactAsync()
        {
            var fact = FactData.DogFacts.RandomElement();
            await SafeRespondAsync(embed: BuildEmbed(null, $"🐶 **Dog Fact:** {fact}", Color.Green));
        }

        [SlashCommand("viewlists", "Sends a message with the servers data lists.")]
        public async Task ViewListsFromServerDataAsync()
        {
            if (!IsUserAdmin())
            {
                await RespondWithErrorAsync("You are not allowed to use this command.");
                return;
            }

            ulong guildId = Context.Guild.Id;
            var serverData = serverDataManager.GetOrCreateServerData(guildId);

            // If the server has any admin roles.
            string response = "**Admin roles:**";
            if(serverData.AdminRoles.Count > 0)
            {
                var adminRoles = serverData.AdminRoles
                    .Select(id => Context.Guild.GetRole(id)?.Name)
                    .Where(name => name != null)
                    .OrderBy(name => name);

                string roleList = string.Join(", ", adminRoles);

                response += $" {roleList}";
            }
            else
            {
                response += " N/A - Use /addadminroles to add some";
            }

            // If the server has watched roles.
            response += $"\n\n**Roles watched:**";
            if (serverData.RolesWatched.Count > 0)
            {
                var roleNames = serverData.RolesWatched
                    .Select(id => Context.Guild.GetRole(id)?.Name)
                    .Where(name => name != null)
                    .OrderBy(name => name);

                string roleList = string.Join(", ", roleNames);

                response += $" {roleList}";
            }
            else
            {
                response += " N/A - Use /addroleswatched to add some";
            }

            // If the server has any bad words.
            response += $"\n\n**Banned words:**";
            if (serverData.BadWords.Count > 1)
            {
                // Turns bad words into readible input. Eg: bAnaNa => Banana.
                var badWords = serverData.BadWords
                    .OrderBy(name => name.ToUpper())
                    .Select(name => string.IsNullOrEmpty(name) ? name : char.ToUpper(name[0]) + name.Substring(1).ToLower());

                string badWordsList = string.Join(", ", badWords);

                // Restrict to first 1000 characters.
                if (badWordsList.Length > 1000)
                {
                    // Add ellipsis if trimmed.
                    badWordsList = badWordsList.Substring(0, 1000) + "...";
                }

                response += $" {badWordsList}";
            }
            else
            {
                response += " N/A - Use /addbadwords to add some";
            }

            // If the server has any default roles.
            response += $"\n\n**Default roles:**";
            if(serverData.DefaultRoles.Count > 0)
            {
                var roleNames = serverData.DefaultRoles
                    .Select(id => Context.Guild.GetRole(id)?.Name)
                    .Where(name => name != null)
                    .OrderBy(name => name);

                string rolesList = string.Join(", ", roleNames);

                response += $" {rolesList}";
            }
            else
            {
                response += " N/A - Use /adddefaultroles to add some";
            }

            await SafeRespondAsync(embed: BuildEmbed("Server information", response, Color.Blue));
        }
        #endregion

        #region Param commands
        /// <summary>
        /// Adds roles to the list of roles to watch.
        /// </summary>
        /// <param name="rolesRaw">Space separated list of role names. Use quotes for multi-word names.</param>
        [SlashCommand("addroleswatched", "Adds roles to the server's watchlist.")]
        public async Task AddRolesWatchedAsync([Summary("roles", "Space separated list of roles. Use quotes for multi-word names.")] string rolesRaw)
        {
            if (!IsUserAdmin())
            {
                await RespondWithErrorAsync("You are not allowed to use this command.");
                return;
            }

            if (string.IsNullOrWhiteSpace(rolesRaw))
            {
                await RespondWithErrorAsync("Please provide at least one role name.");
                return;
            }

            ulong guildId = Context.Guild.Id;
            var serverData = serverDataManager.GetOrCreateServerData(guildId);

            var roleNames = ParseQuotedInput(rolesRaw).Select(n => n.ToLowerInvariant());

            int addedCount = 0;
            foreach (var name in roleNames)
            {
                var match = Context.Guild.Roles.FirstOrDefault(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (match != null && serverData.RolesWatched.Add(match.Id))
                {
                    addedCount++;
                }
            }

            serverDataManager.UpdateServerData(serverData);

            await SafeRespondAsync(embed: BuildEmbed("Add Roles Watched", $"Added {addedCount} role(s) to the watchlist.", Color.Green));
        }


        /// <summary>
        /// Removes roles from the list of roles to watch.
        /// </summary>
        /// <param name="rolesRaw">Space separated list of role names. Use quotes for multi-word names.</param>
        [SlashCommand("removeroleswatched", "Removes roles from the server's watchlist.")]
        public async Task RemoveRolesWatchedAsync([Summary("roles", "Space separated list of roles. Use quotes for multi-word names.")] string rolesRaw)
        {
            if (!IsUserAdmin())
            {
                await RespondWithErrorAsync("You are not allowed to use this command.");
                return;
            }

            if (string.IsNullOrWhiteSpace(rolesRaw))
            {
                await RespondWithErrorAsync("Please provide at least one role name.");
                return;
            }

            ulong guildId = Context.Guild.Id;
            var serverData = serverDataManager.GetOrCreateServerData(guildId);

            var roleNames = ParseQuotedInput(rolesRaw).Select(n => n.ToLowerInvariant());

            int removedCount = 0;
            foreach (var name in roleNames)
            {
                var match = Context.Guild.Roles.FirstOrDefault(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (match != null && serverData.RolesWatched.Remove(match.Id))
                {
                    removedCount++;
                }
            }

            serverDataManager.UpdateServerData(serverData);

            await SafeRespondAsync(embed: BuildEmbed("Remove Roles Watched", $"Removed {removedCount} role(s) from the watchlist.", Color.Orange));
        }

        /// <summary>
        /// Adds bad words to the server.
        /// </summary>
        /// <returns></returns>
        /// <param name="wordsRaw">Space separated list of bad words. Use quotes for multi-word names.</param>
        [SlashCommand("addbadwords", "Add bad words to the server's bad word list")]
        public async Task AddBadWordsAsync([Summary("words", "Space separated list of bad words. Use quotes for double words.")] string wordsRaw)
        {
            if (!IsUserAdmin())
            {
                await RespondWithErrorAsync("You are not allowed to use this command.");
                return;
            }

            if (string.IsNullOrWhiteSpace(wordsRaw))
            {
                await RespondWithErrorAsync("Please provide at least one word.");
                return;
            }

            ulong guildId = Context.Guild.Id;
            var serverData = serverDataManager.GetOrCreateServerData(guildId);

            // Split by spaces and normalize to lowercase.
            var newWords = ParseQuotedInput(wordsRaw).Select(w => w.ToLowerInvariant());

            int addedCount = 0;
            foreach (var word in newWords)
            {
                // HashSet.Add returns true if added.
                if (serverData.BadWords.Add(word))
                {
                    addedCount++;
                }
            }

            serverDataManager.UpdateServerData(serverData);

            await SafeRespondAsync(embed: BuildEmbed("Add Bad Words", $"Added {addedCount} new bad word(s) to this server.", Color.Green));
        }

        /// <summary>
        /// Removes bad words from the server.
        /// </summary>
        /// <returns></returns>
        /// <param name="wordsRaw">Space separated list of words. Use quotes for multi-word names.</param>
        [SlashCommand("removebadwords", "Removes bad words from the server's bad word list")]
        public async Task RemoveBadWordsAsync([Summary("words", "Space separated list of bad words. Use quotes for double words.")] string wordsRaw)
        {
            if (!IsUserAdmin())
            {
                await RespondWithErrorAsync("You are not allowed to use this command.");
                return;
            }

            if (string.IsNullOrWhiteSpace(wordsRaw))
            {
                await RespondWithErrorAsync("Please provide at least one word.");
                return;
            }

            ulong guildId = Context.Guild.Id;
            var serverData = serverDataManager.GetOrCreateServerData(guildId);

            // Split by spaces and normalize to lowercase.
            var newWords = ParseQuotedInput(wordsRaw).Select(w => w.ToLowerInvariant());

            int removedCount = 0;
            foreach (var word in newWords)
            {
                // HashSet.Remove returns true if removed.
                if (serverData.BadWords.Remove(word))
                {
                    removedCount++;
                }
            }

            serverDataManager.UpdateServerData(serverData);

            await SafeRespondAsync(embed: BuildEmbed("Remove Bad Words", $"Removed {removedCount} bad word(s) from this server.", Color.Orange));
        }

        /// <summary>
        /// Adds roles to the default roles assigned to new users.
        /// </summary>
        /// <param name="rolesRaw">Space separated list of role names. Use quotes for multi-word names.</param>
        [SlashCommand("adddefaultroles", "Add roles to be automatically assigned to new users.")]
        public async Task AddDefaultRolesAsync([Summary("roles", "Space separated list of roles. Use quotes for multi-word names.")] string rolesRaw)
        {
            if (!IsUserAdmin())
            {
                await RespondWithErrorAsync("You are not allowed to use this command.");
                return;
            }

            if (string.IsNullOrWhiteSpace(rolesRaw))
            {
                await RespondWithErrorAsync("Please provide at least one role name.");
                return;
            }

            ulong guildId = Context.Guild.Id;
            var serverData = serverDataManager.GetOrCreateServerData(guildId);

            var roleNames = ParseQuotedInput(rolesRaw).Select(n => n.ToLowerInvariant());

            int addedCount = 0;
            foreach (var name in roleNames)
            {
                var role = Context.Guild.Roles.FirstOrDefault(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (role != null && (serverData.DefaultRoles == null || !serverData.DefaultRoles.Contains(role.Id)))
                {
                    serverData.DefaultRoles.Add(role.Id);
                    addedCount++;
                }
            }

            serverDataManager.UpdateServerData(serverData);

            await SafeRespondAsync(embed: BuildEmbed("Add Default Roles", $"Added {addedCount} role(s) to the default roles list.", Color.Green));
        }

        /// <summary>
        /// Removes roles from the default roles assigned to new users.
        /// </summary>
        /// <param name="rolesRaw">Space separated list of role names. Use quotes for multi-word names.</param>
        [SlashCommand("removedefaultroles", "Remove roles from the default roles assigned to new users.")]
        public async Task RemoveDefaultRolesAsync([Summary("roles", "Space separated list of roles. Use quotes for multi-word names.")] string rolesRaw)
        {
            if (!IsUserAdmin())
            {
                await RespondWithErrorAsync("You are not allowed to use this command.");
                return;
            }

            if (string.IsNullOrWhiteSpace(rolesRaw))
            {
                await RespondWithErrorAsync("Please provide at least one role name.");
                return;
            }

            ulong guildId = Context.Guild.Id;
            var serverData = serverDataManager.GetOrCreateServerData(guildId);

            if (serverData.DefaultRoles == null)
            {
                await RespondWithInfoAsync("No default roles are currently set.");
                return;
            }

            var roleNames = ParseQuotedInput(rolesRaw).Select(n => n.ToLowerInvariant());

            int removedCount = 0;
            foreach (var name in roleNames)
            {
                var role = Context.Guild.Roles.FirstOrDefault(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (role != null && serverData.DefaultRoles.Contains(role.Id))
                {
                    serverData.DefaultRoles.Remove(role.Id);
                    removedCount++;
                }
            }

            serverDataManager.UpdateServerData(serverData);

            await SafeRespondAsync(embed: BuildEmbed("Remove Default Roles", $"Removed {removedCount} role(s) from the default roles list.", Color.Orange));
        }



        /// <summary>
        /// Adds admin roles.
        /// </summary>
        /// <param name="rolesRaw">Space separated list of role names. Use quotes for multi-word names.</param>
        [SlashCommand("addadminroles", "Add roles for admins on the server.")]
        public async Task AddAdminRolesAsync([Summary("roles", "Space separated list of roles. Use quotes for multi-word names.")] string rolesRaw)
        {
            if (!IsUserAdmin())
            {
                await RespondWithErrorAsync("You are not allowed to use this command.");
                return;
            }

            if (string.IsNullOrWhiteSpace(rolesRaw))
            {
                await RespondWithErrorAsync("Please provide at least one role name.");
                return;
            }

            ulong guildId = Context.Guild.Id;
            var serverData = serverDataManager.GetOrCreateServerData(guildId);

            var roleNames = ParseQuotedInput(rolesRaw).Select(n => n.ToLowerInvariant());

            int addedCount = 0;
            foreach (var name in roleNames)
            {
                var role = Context.Guild.Roles.FirstOrDefault(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (role != null && (serverData.AdminRoles == null || !serverData.AdminRoles.Contains(role.Id)))
                {
                    serverData.AdminRoles.Add(role.Id);
                    addedCount++;
                }
            }

            serverDataManager.UpdateServerData(serverData);

            await SafeRespondAsync(embed: BuildEmbed("Add Admin Roles", $"Added {addedCount} role(s) to the admin roles list.", Color.Green));
        }

        /// <summary>
        /// Removes admin roles from the server.
        /// </summary>
        /// <param name="rolesRaw">Space separated list of role names. Use quotes for multi-word names.</param>
        [SlashCommand("removeadminroles", "Remove admin roles.")]
        public async Task RemoveAdminRolesAsync([Summary("roles", "Space separated list of roles. Use quotes for multi-word names.")] string rolesRaw)
        {
            if (!IsUserAdmin())
            {
                await RespondWithErrorAsync("You are not allowed to use this command.");
                return;
            }

            if (string.IsNullOrWhiteSpace(rolesRaw))
            {
                await RespondWithErrorAsync("Please provide at least one role name.");
                return;
            }

            ulong guildId = Context.Guild.Id;
            var serverData = serverDataManager.GetOrCreateServerData(guildId);

            if (serverData.AdminRoles == null)
            {
                await RespondWithInfoAsync("No admin roles are currently set.");
                return;
            }

            var roleNames = ParseQuotedInput(rolesRaw).Select(n => n.ToLowerInvariant());

            int removedCount = 0;
            foreach (var name in roleNames)
            {
                var role = Context.Guild.Roles.FirstOrDefault(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (role != null && serverData.AdminRoles.Contains(role.Id))
                {
                    serverData.AdminRoles.Remove(role.Id);
                    removedCount++;
                }
            }

            serverDataManager.UpdateServerData(serverData);

            await SafeRespondAsync(embed: BuildEmbed("Remove Admin Roles", $"Removed {removedCount} role(s) from the admin roles list.", Color.Orange));
        }

        /// <summary>
        /// Takes a number and rolls a dice with the same amount of sides as the number.
        /// </summary>
        /// <param name="sides">The number of sides the dice has.</param>
        /// <returns></returns>
        [SlashCommand("diceroll", "Rolls a dice based on the given number.")]
        public async Task DiceAsync([Summary("sides", "How many sides does the dice have?")] int sides)
        {
            if (sides <= 0)
            {
                await CorrectParameterMessage();
                return;
            }

            int result = sides.RandomPositiveNumber();

            await SafeRespondAsync(embed: BuildEmbed("🎲 Dice Roll", $"You rolled a **{result}** out of {sides} sides!", Color.Orange));
        }
        #endregion

        /// <summary>
        /// Sends a message to enter the argument correctly.
        /// </summary>
        /// <returns></returns>
        private async Task CorrectParameterMessage()
        {
            await SafeRespondAsync(embed: BuildEmbed("Error", "Enter a valid argument.", Color.Red));
        }

        /// <summary>
        /// Checks if user is on a watch list.
        /// </summary>
        /// <returns>Returns true if they're found on a list.</returns>
        private bool IsUserAdmin()
        {
            var user = Context.User as SocketGuildUser;
            var serverData = serverDataManager.GetOrCreateServerData(Context.Guild.Id);

            return serverData.IsUserAdmin(Context.Guild, user.Id);
        }

        /// <summary>
        /// Helper: Send error embed response.
        /// </summary>
        private async Task RespondWithErrorAsync(string message)
        {
            await SafeRespondAsync(embed: BuildEmbed("Error", message, Color.Red));
        }

        /// <summary>
        /// Helper: Send info embed response.
        /// </summary>
        private async Task RespondWithInfoAsync(string message)
        {
            await SafeRespondAsync(embed: BuildEmbed(null, message, Color.Blue));
        }

        /// <summary>
        /// Gets the current server for context.
        /// </summary>
        /// <returns>Returns the current server.</returns>
        private DiscordServerData GetCurrentServer()
        {
            DiscordServerData discordServer = serverDataManager.GetOrCreateServerData(Context.Guild.Id);
            return discordServer;
        }

        /// <summary>
        /// Safely responds to async.
        /// </summary>
        /// <param name="embed">The embed to send.</param>
        /// <param name="content">The content.</param>
        /// <param name="ephemeral">Is seen only by 1 person.</param>
        /// <returns></returns>
        private async Task SafeRespondAsync(string content = null, Embed embed = null, MessageComponent components = null, bool deferIfNeeded = true)
        {
            try
            {
                // Try to respond immediately.
                await RespondAsync(text: content, embed: embed, components: components, ephemeral: GetCurrentServer().AreMessagesEphemeral);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Interaction has already been acknowledged"))
            {
                // Already responded or deferred, send a followup
                try
                {
                    await FollowupAsync(text: content, embed: embed, components: components, ephemeral: GetCurrentServer().AreMessagesEphemeral);
                }
                catch (Exception followupEx)
                {
                    Console.WriteLine($"[SafeRespondAsync] Followup failed: {followupEx}");
                }
            }
            catch (TimeoutException) when (deferIfNeeded)
            {
                // Interaction timed out; can't respond or defer
                Console.WriteLine("[SafeRespondAsync] Timeout: Interaction expired.");
            }
            catch (HttpException ex) when (ex.HttpCode == System.Net.HttpStatusCode.BadRequest && ex.Message.Contains("Unknown interaction"))
            {
                // Missing permission
                Console.WriteLine($"[SafeRespondAsync] Forbidden (Missing Permissions): {ex.Message}");
            }
            catch (Exception ex)
            {
                // Could not respond immediately, try to defer then followup
                if (deferIfNeeded)
                {
                    try
                    {
                        await DeferAsync(ephemeral: GetCurrentServer().AreMessagesEphemeral);
                        await FollowupAsync(text: content, embed: embed, components: components);
                    }
                    catch (Exception deferEx)
                    {
                        Console.WriteLine($"[SafeRespondAsync] Defer or Followup failed: {deferEx}");
                    }
                }
                else
                {
                    Console.WriteLine($"[SafeRespondAsync] Unexpected error: {ex}");
                }
            }
        }


        /// <summary>
        /// Constructs a Discord embed with the specified title, description, and color,
        /// and appends the invoking user's username and the current timestamp as the footer.
        /// </summary>
        /// <param name="title">The title of the embed.</param>
        /// <param name="description">The main content or body text of the embed.</param>
        /// <param name="color">The color accent of the embed (used for the embed's border).</param>
        /// <returns></returns>
        private Embed BuildEmbed(string title, string description, Color color)
        {
            return new EmbedBuilder()
                .WithTitle(title)
                .WithDescription(description)
                .WithColor(color)
                .WithFooter(footer => footer.Text = $"{Context.User.Username}")
                .WithCurrentTimestamp()
                .Build();
        }

        /// <summary>
        /// Helper: Parse a quoted input.
        /// </summary>
        /// <param name="input">The user input.</param>
        /// <returns></returns>
        private IEnumerable<string> ParseQuotedInput(string input)
        {
            return Regex.Matches(input, @"[\""].+?[\""]|\S+")
                .Cast<Match>()
                .Select(m => m.Value.Trim('"'));
        }
    }
}