// This program will be run from cmd as a proof of concept
// The first argument is the data.win location
// The second argument is the mod folder location

using Microsoft.Extensions.Configuration;
using UndertaleModLib;
using UndertaleModLib.Decompiler;

string gameDataPath = args[0];
string modFolder = args[1];
string modsConfig = $"{modFolder}/mods.ini";

UndertaleData gameData;

try
{
    using (var stream = new FileStream(gameDataPath, FileMode.Open, FileAccess.ReadWrite))
        gameData = UndertaleIO.Read(stream);
} catch 
{
    Console.WriteLine("Error reading game data");
    return;
}

foreach(string folder in Directory.GetDirectories(modFolder))
{
    var modConfig = new ConfigurationBuilder().AddIniFile($"{folder}/mod.ini").Build().GetSection("Loader");
    if (modConfig["enabled"] == "false")
        continue;

    Console.WriteLine($"Loading mod {modConfig["name"]}");    
}
