using Discord;
using Discord.WebSocket;

namespace DogPlus
{
    public static class EmojiManager
    {
        /// <summary>
        /// Default emojis to use.
        /// </summary>
        public static HashSet<Emoji> Emojis { get; private set; } = new HashSet<Emoji>()
        {
            new Emoji("👍"),
            new Emoji("👎"),
            new Emoji("😂"),
            new Emoji("😄"),
            new Emoji("🔥"),
            new Emoji("🎉"),
            new Emoji("💯"),
            new Emoji("🙌"),
            new Emoji("😮"),
            new Emoji("🤔"),
            new Emoji("👀"),
            new Emoji("😢"),
            new Emoji("❓"),
            new Emoji("❌"),
            new Emoji("🥳"),
            new Emoji("🤝"),
            new Emoji("🤷"),
            new Emoji("😇"),
            new Emoji("🤡"),
            new Emoji("🙃"),
            new Emoji("🤫"),
            new Emoji("😎"),
            new Emoji("💀"),
            new Emoji("😤"),
            new Emoji("🥺"),
            new Emoji("😜"),
            new Emoji("🤯"),
            new Emoji("😴"),
        };

        /// <summary>
        /// Gets a random emoji either from guild or set emoji list.
        /// </summary>
        /// <param name="guild">The guild.</param>
        /// <returns>Returns either a custom guild emoji or a normal emoji.</returns>
        public static IEmote GetRandomEmoji(SocketGuild guild)
        {
            IEmote emoji = null;

            int random = Extensions.rng.Next(0,11);

            // 1 in 10 to choose custom guild emoji.
            if(random == 7)
            {
                emoji = GetRandomGuildEmoji(guild);
            }

            if(emoji == null)
            {
                emoji = Emojis.RandomElement();
            }

            return emoji;
        }

        /// <summary>
        /// Tries to find a random custom guild emoji.
        /// Returns null if not found.
        /// </summary>
        private static Emote GetRandomGuildEmoji(SocketGuild guild)
        {
            Emote emote = guild.Emotes.RandomElement();
            return emote;
        }
    }
}
