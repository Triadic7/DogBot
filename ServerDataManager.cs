using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

/// <summary>
/// Manages loading, saving, and accessing server-specific data for Discord servers.
/// </summary>
public class ServerDataManager
{
    /// <summary>
    /// The path to the JSON file used for storing server data.
    /// </summary>
    private readonly string filePath;

    /// <summary>
    /// Dictionary holding all server data, keyed by Discord guild ID.
    /// </summary>
    private readonly Dictionary<ulong, DiscordServerData> serverDataDict = new Dictionary<ulong, DiscordServerData>();

    /// <summary>
    /// Initializes a new instance of the <see cref="ServerDataManager"/> class,
    /// loading existing data from the specified JSON file or creating a new file if it doesn't exist.
    /// </summary>
    /// <param name="filePath">The path to the JSON file for data storage. Defaults to "serverData.json".</param>
    public ServerDataManager(string filePath = "serverData.json")
    {
        this.filePath = filePath;
        LoadData();
    }

    /// <summary>
    /// Loads the server data dictionary from the JSON file.
    /// If the file does not exist, creates an empty JSON dictionary file for future use.
    /// </summary>
    private void LoadData()
    {
        if (!File.Exists(filePath))
        {
            // Create an empty JSON dictionary file for future use.
            File.WriteAllText(filePath, "{}");
        }

        var json = File.ReadAllText(filePath);
        var tempDict = JsonConvert.DeserializeObject<Dictionary<ulong, DiscordServerData>>(json);

        if (tempDict == null)
        {
            return;
        }

        // Clear the readonly dictionary contents and refill it.
        serverDataDict.Clear();
        foreach (var kvp in tempDict)
        {
            serverDataDict[kvp.Key] = kvp.Value;
        }
    }

    /// <summary>
    /// Saves the current server data dictionary to the JSON file.
    /// </summary>
    public void SaveData()
    {
        var json = JsonConvert.SerializeObject(serverDataDict, Formatting.Indented);
        File.WriteAllText(filePath, json);
    }

    /// <summary>
    /// Retrieves the <see cref="DiscordServerData"/> for a specific guild server Id.
    /// If no data exists for the guild, a new entry is created, saved, and returned.
    /// </summary>
    /// <param name="guildId">The Discord guild server Id.</param>
    /// <returns>The <see cref="DiscordServerData"/> associated with the specified guild.</returns>
    public DiscordServerData GetOrCreateServerData(ulong guildId)
    {
        if (!serverDataDict.TryGetValue(guildId, out var data))
        {
            data = new DiscordServerData(guildId);
            serverDataDict[guildId] = data;
            SaveData();
        }
        return data;
    }

    /// <summary>
    /// Updates the stored <see cref="DiscordServerData"/> for a server and persists the changes to file.
    /// </summary>
    /// <param name="data">The server data to update.</param>
    public void UpdateServerData(DiscordServerData data)
    {
        serverDataDict[data.Id] = data;
        SaveData();
    }
}