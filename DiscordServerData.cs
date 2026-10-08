using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class DiscordServerData
{
    /// <summary>
    /// The Id of the server.
    /// </summary>
    public ulong Id { get; private set; }

    /// <summary>
    /// The list of bad words.
    /// </summary>
    public HashSet<string> BadWords { get; private set; }

    /// <summary>
    /// The list of roles watched to make sure they say no bad words.
    /// </summary>
    public HashSet<ulong> RolesWatched { get; private set; }

    /// <summary>
    /// The list of roles that can edit the settings on the bot.
    /// </summary>
    public HashSet<ulong> AdminRoles { get; private set; }

    /// <summary>
    /// The list of roles that a user gets when they first join a guild (default roles).
    /// </summary>
    public HashSet<ulong> DefaultRoles { get; private set; }

    /// <summary>
    /// The percentage chance to use an emoji. Default = 0.
    /// </summary>
    [JsonProperty]
    public int EmojiReactionChance { get; private set; }

    /// <summary>
    /// Tracks if messages are visable to only one person.
    /// </summary>
    [JsonProperty]
    public bool AreMessagesEphemeral { get; private set; }

    /// <summary>
    /// List for keeping track of a user saying bad words.
    /// </summary>
    private Dictionary<ulong, int> userBadWordCounts = new Dictionary<ulong, int>();

    /// <summary>
    /// The amount of time a user is timed out for.
    /// </summary>
    [JsonProperty]
    public TimeSpan TimeoutTime { get; private set; }

    /// <summary>
    /// Determines what will happen to bad users.
    /// </summary>
    public enum PunishmentType
    {
        None,
        Timeout,
        KickUser,
        BanUser
    }

    /// <summary>
    /// Max number of bad words said.
    /// </summary>
    [JsonProperty]
    public int MaxBannedWordsSaid { get; private set; }

    /// <summary>
    /// The punishment for saying naughty words.
    /// </summary>
    [JsonProperty]
    public PunishmentType Punishment { get; private set; } = PunishmentType.None;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="id">The server Id.</param>
    public DiscordServerData(ulong id)
    {
        this.Id = id;
        this.BadWords = new HashSet<string>();
        this.RolesWatched = new HashSet<ulong>();
        this.AdminRoles = new HashSet<ulong>();
        this.DefaultRoles = new HashSet<ulong>();
        this.TimeoutTime = TimeSpan.Zero;
        this.MaxBannedWordsSaid = 999999;
        this.EmojiReactionChance = 0;
        this.AreMessagesEphemeral = true;
    }

    /// <summary>
    /// Checks if a user has admin role.
    /// </summary>
    /// <param name="guild">The guild.</param>
    /// <param name="userId">The users Id.</param>
    /// <returns>Returns true if the user has an admin role, otherwise returns false.</returns>
    public bool IsUserAdmin(SocketGuild guild, ulong userId)
    {
        // Get user who said bad word.
        var user = guild.GetUser(userId);
        if (user == null)
        {
            return false;
        }

        // Allow server owner.
        if (user.Id == guild.OwnerId)
            return true;

        // Allow Discord admins.
        if (user.GuildPermissions.Administrator)
            return true;

        // Has admin role or admin roles are empty.
        if (user.Roles.Any(r => AdminRoles.Contains(r.Id)) || AdminRoles.Count == 0)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Sets the chance to use an emoji. 10-100%
    /// </summary>
    /// <param name="chance">The percentage to use an emoji.</param>
    public void SetEmojiUsage(int chance)
    {
        this.EmojiReactionChance = chance;
    }

    /// <summary>
    /// Sets message visability.
    /// </summary>
    /// <param name="isVisable">Are messages visable?</param>
    public void SetMessageVisability(bool isVisable)
    {
        this.AreMessagesEphemeral = isVisable;
    }

    /// <summary>
    /// Sets the punishment type.
    /// </summary>
    /// <param name="punishment">The type of punishment.</param>
    public void SetPunishmentType(PunishmentType punishment)
    {
        this.Punishment = punishment;
    }

    /// <summary>
    /// Sets the amount of bad words you can say before you get punished.
    /// </summary>
    /// <param name="amount">The amount before punishment.</param>
    public void SetBadWordCount(int amount)
    {
        this.MaxBannedWordsSaid = amount;
    }

    /// <summary>
    /// Sets the timeout time.
    /// </summary>
    /// <param name="time">The timeout time.</param>
    public void SetTimoutTime(TimeSpan time)
    {
        this.TimeoutTime = time;
    }

    /// <summary>
    /// Adds a mark on the users record for saying a bad word.
    /// </summary>
    /// <param name="userId">The user saying the bad word.</param>
    public async Task RecordBadWordUsageAsync(SocketGuild guild, ulong userId)
    {
        if (!this.userBadWordCounts.ContainsKey(userId))
        {
            this.userBadWordCounts[userId] = 0;
        }

        this.userBadWordCounts[userId]++;

        if (this.userBadWordCounts[userId] >= MaxBannedWordsSaid)
        {
            await ApplyPunishmentAsync(guild, userId);
        }
    }

    /// <summary>
    /// Depending on the punishment, affects the user diffrently.
    /// </summary>
    /// <param name="userId">The user saying the bad word.</param>
    private async Task ApplyPunishmentAsync(SocketGuild guild, ulong userId)
    {
        // Checks if bot has permissions.
        var botUser = guild.CurrentUser;
        if (!botUser.GuildPermissions.KickMembers)
        {
            Console.WriteLine("Bot lacks Kick Members permission.");
            return;
        }

        if (!botUser.GuildPermissions.BanMembers)
        {
            Console.WriteLine("Bot lacks Ban Members permission.");
            return;
        }

        if (!botUser.GuildPermissions.ModerateMembers)
        {
            Console.WriteLine("Bot lacks Timeout Members permission.");
            return;
        }

        // Get user who said bad word.
        var user = guild.GetUser(userId);
        if (user == null)
        {
            return;
        }

        // Try to apply punishment.
        try
        {
            switch (this.Punishment)
            {
                case PunishmentType.None:
                    break;

                case PunishmentType.KickUser:
                    await user.KickAsync("Exceeded max bad words limit");
                    break;

                case PunishmentType.BanUser:
                    await guild.AddBanAsync(user, 0, "Exceeded max bad words limit");
                    break;

                case PunishmentType.Timeout:
                    await user.SetTimeOutAsync(TimeoutTime);
                    break;
            }
        }
        catch (Exception ex)
        {
            // Log exception.
            Console.WriteLine($"Failed to apply punishment: {ex.Message}");
        }
        finally
        {
            // Reset count regardless of success or failure.
            this.userBadWordCounts[userId] = 0;
        }
    }
}
