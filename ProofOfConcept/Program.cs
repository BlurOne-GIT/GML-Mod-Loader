// This program will be run from cmd as a proof of concept
// The first argument is the data.win location
// The second argument is the mod folder location

using Microsoft.Extensions.Configuration;
using UndertaleModLib;
using UndertaleModLib.Models;
using UndertaleModLib.Compiler;
using System.Drawing;

string gameDataPath = args[0];
string modFolder = args[1];
string modsConfig = $"{modFolder}/mods.ini";
Dictionary<string, int> moddedCodes = new Dictionary<string, int>();
Dictionary<string, int> moddedSprites = new Dictionary<string, int>();

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

foreach (string folder in Directory.GetDirectories(modFolder))
{
    var modConfig = new ConfigurationBuilder().AddIniFile($"{folder}/mod.ini").Build().GetSection("Loader");

    if (modConfig["enabled"] == "false")
        continue;

    Console.WriteLine($"Loading mod {modConfig["name"]}");
    
    if (Directory.Exists($"{folder}/Sprites"))
    {
        Console.WriteLine("Loading sprites...");
        foreach (string sprite in Directory.GetFiles($"{folder}/sprites").Where(x => !x.EndsWith(".ini")))
        {
            Console.WriteLine($"Loading sprite {sprite}");
            ReplaceSprite(sprite, Convert.ToInt32(modConfig["priority"]));
        }
    }

    if (Directory.Exists($"{folder}/Code"))
    {
        Console.WriteLine("Loading code...");
        foreach (string code in Directory.GetFiles($"{folder}/scripts").Where(x => x.EndsWith(".gml")))
        {
            Console.WriteLine($"Loading code {code}");
            ReplaceCode(code, Convert.ToInt32(modConfig["priority"]));
        }
    }
}

using (var stream = new FileStream(Path.GetDirectoryName(gameDataPath) + "modded.win", FileMode.OpenOrCreate, FileAccess.ReadWrite))
    UndertaleIO.Write(stream, gameData);

void ReplaceCode(string codePath, int modPriority)
{
    string codeName = Path.GetFileNameWithoutExtension(codePath);

    if (moddedCodes.ContainsKey(codeName) && moddedCodes[codeName] < modPriority)
    {
        Console.WriteLine($"Code {codeName} already replaced with a higher priority mod, pain ahead.");
        return;
    }

    moddedCodes[codeName] = modPriority;

    UndertaleCode codeToReplace = gameData.Code.First(x => x.Name.Content == codeName);
    var fileConfig = new ConfigurationBuilder().AddIniFile($"./{codeName}.ini").Build().GetSection("ReplaceValues");

    if (codeToReplace is null)
    {
        codeToReplace = new UndertaleCode() {
            Name = new UndertaleString(codeName)
            // Add the replace values from fileConfig
        };
        gameData.Code.Add(codeToReplace);
    }

    CompileContext context = Compiler.CompileGMLText(File.ReadAllText(codePath), gameData, codeToReplace);
    codeToReplace.Replace(context.ResultAssembly);
}

void ReplaceSprite(string spritePath, int modPriority)
{
    string spriteName = Path.GetFileNameWithoutExtension(spritePath);
    int spriteIndex = Convert.ToInt32(spriteName.Substring(spriteName.LastIndexOf('_') + 1));

    if (moddedSprites.ContainsKey(spriteName) && moddedSprites[spriteName] < modPriority)
    {
        Console.WriteLine($"Sprite {spriteName} already replaced with a higher priority mod, pain ahead.");
        return;
    }

    moddedSprites[spriteName] = modPriority;

    UndertaleSprite spriteToReplace = gameData.Sprites.First(x => x.Name.Content == spriteName);
    var fileConfig = new ConfigurationBuilder().AddIniFile($"./{spriteName}.ini").Build().GetSection("ReplaceValues");

    if (spriteToReplace is null)
    {
        spriteToReplace = new UndertaleSprite() {
            Name = new UndertaleString(spriteName.Substring(0, spriteName.Length - spriteName.LastIndexOf('_')))
            // Add the replace values from fileConfig
        };
        gameData.Sprites.Add(spriteToReplace);
    }

    spriteToReplace.Textures[spriteIndex].Texture.ReplaceTexture(Image.FromFile(spritePath));
}

