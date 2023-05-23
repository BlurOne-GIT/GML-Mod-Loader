// This program will be run from cmd as a proof of concept
// The first argument is the data.win location
// The second argument is the mod folder location

using Microsoft.Extensions.Configuration;
using UndertaleModLib;
using UndertaleModLib.Models;
using UndertaleModLib.Compiler;
using System.Drawing;

string gameDataPath = args[0];
string modsFolder = args[1];
string loaderConfigPath = $"{modsFolder}/loader.ini";
Dictionary<string, int> moddedCodes = new Dictionary<string, int>();
Dictionary<string, int> moddedSprites = new Dictionary<string, int>();
Dictionary<string, int> moddedScripts = new Dictionary<string, int>();

var loaderConfig = new ConfigurationBuilder().AddIniFile(loaderConfigPath).Build().GetSection("Loader");

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

foreach (string folder in Directory.GetDirectories(modsFolder))
{
    var modConfig = new ConfigurationBuilder().AddIniFile($"{folder}/mod.ini").Build().GetSection("Loader");

    if (!Convert.ToBoolean(modConfig["enabled"]) || modConfig["steamAppID"] != gameData.GeneralInfo.SteamAppID.ToString())
        continue;

    /* TODO: finish this line
    if (modConfig["gameVersion"] != gameData.GeneralInfo. && !Convert.ToBool(loaderConfig["ignoreVersionMismatch"]))
    {
        Console.WriteLine($"Mod version does not match game version, skipping {folder}.");
        continue;
    }
    */
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

    if (Path.Exists($"{folder}/scripts.ini"))
    {
        Console.WriteLine("Loading scripts...");
        var scriptsIni = new ConfigurationBuilder().AddIniFile($"{folder}/scripts.ini").Build();
        foreach (var section in scriptsIni.GetChildren())
            ReplaceScript(section["name"], section["code"], Convert.ToBoolean(section["isConstructor"]), Convert.ToInt32(modConfig["priority"]));
    }

    if (Path.Exists($"{folder}/removeGameEndScripts.ini"))
    {
        Console.WriteLine("Removing game end scripts...");
        var scriptsIni = new ConfigurationBuilder().AddIniFile($"{folder}/gameEndScripts.ini").Build();
    }

    if (Path.Exists($"{folder}/addGameEndScripts.ini"))
    {
        Console.WriteLine("Adding game end scripts...");
        var scriptsIni = new ConfigurationBuilder().AddIniFile($"{folder}/gameEndScripts.ini").Build();
        // TODO
    }

    gameData.Scripts[0] = new UndertaleScript();
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

void ReplaceScript(string scriptName, string codeName, bool isConstructor, int modPriority)
{
    if (moddedScripts.ContainsKey(scriptName) && moddedScripts[scriptName] < modPriority)
    {
        Console.WriteLine($"Script {scriptName} already replaced with a higher priority mod, pain ahead.");
        return;
    }

    moddedScripts[scriptName] = modPriority;

    UndertaleScript scriptToReplace = gameData.Scripts.First(x => x.Name.Content == scriptName);

    if (scriptToReplace is null)
    {
        scriptToReplace = new UndertaleScript() {
            Name = new UndertaleString(scriptName),
            IsConstructor = isConstructor
        };
        gameData.Scripts.Add(scriptToReplace);
    }

    UndertaleCode codeToUse = gameData.Code.First(x => x.Name.Content == codeName);

    if (codeToUse is null)
    {
        Console.WriteLine($"Code {codeName} not found, skipping script {scriptName}, pain head.");
        return;
    }

    scriptToReplace.Code = codeToUse;
}