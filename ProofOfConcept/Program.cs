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
var loaderConfig = new ConfigurationBuilder().AddIniFile(loaderConfigPath).Build().GetSection("Loader");

Dictionary<string, int> moddedCodes = new Dictionary<string, int>();
Dictionary<string, int> moddedSprites = new Dictionary<string, int>();
Dictionary<string, int> moddedScripts = new Dictionary<string, int>();

UndertaleData gameData;

Console.WriteLine("Loading game data...");

try
{
    using (var stream = new FileStream(gameDataPath, FileMode.Open, FileAccess.ReadWrite))
        gameData = UndertaleIO.Read(stream);
} catch 
{
    Console.WriteLine("Error reading game data");
    return;
}

Console.WriteLine($"Loaded game {gameData.GeneralInfo.Name.Content}");

foreach (string folder in Directory.GetDirectories(modsFolder))
{
    var modConfig = new ConfigurationBuilder().AddIniFile($"{folder}/mod.ini").Build().GetSection("Loader");

    if (!Convert.ToBoolean(modConfig["enabled"]) || modConfig["game"] != gameData.GeneralInfo.Name.Content)
        continue;

    /*
    if (modConfig["gameVersion"] != gameData.GeneralInfo.Release.ToString() && !Convert.ToBoolean(loaderConfig["ignoreVersionMismatch"]))
    {
        Console.WriteLine($"Mod version does not match game version, skipping {folder}.");
        continue;
    }
    */
    
    Console.WriteLine($"Loading mod {modConfig["name"]}");
    
    if (Directory.Exists($"{folder}/Sprites"))
    {
        Console.WriteLine("Loading sprites...");
        foreach (string sprite in Directory.GetFiles($"{folder}/Sprites").Where(x => !x.EndsWith(".ini")))
        {
            Console.WriteLine($"Loading sprite {sprite}");
            ReplaceTexture(sprite, Convert.ToInt32(modConfig["priority"]));
        }
    }

    if (Directory.Exists($"{folder}/Code"))
    {
        Console.WriteLine("Loading code...");
        foreach (string code in Directory.GetFiles($"{folder}/Code").Where(x => x.EndsWith(".gml")))
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
            ReplaceScript(section["name"]!, section["code"]!, Convert.ToBoolean(section["isConstructor"]), Convert.ToInt32(modConfig["priority"]));
    }

    if (Path.Exists($"{folder}/globalInit.ini"))
    {
        Console.WriteLine("Adding global init scripts...");
        var scriptsIni = new ConfigurationBuilder().AddIniFile($"{folder}/globalInit.ini").Build();
        foreach (var section in scriptsIni.GetChildren())
            AddGlobalInitScript(section["code"]!);
    }

    if (Path.Exists($"{folder}/gameEndScripts.ini"))
    {
        Console.WriteLine("Adding game end scripts...");
        var scriptsIni = new ConfigurationBuilder().AddIniFile($"{folder}/gameEndScripts.ini").Build();
        foreach (var section in scriptsIni.GetChildren())
            AddGameEndScript(section["code"]!);
    }
}

using (var stream = new FileStream(Path.GetDirectoryName(gameDataPath) + "/modded.win", FileMode.OpenOrCreate, FileAccess.ReadWrite))
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

    IConfigurationSection fileConfig;
    if (Path.Exists($"./{codeName}.ini"))
        fileConfig = new ConfigurationBuilder().AddIniFile($"./{codeName}.ini").Build().GetSection("ReplaceValues");

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

void ReplaceTexture(string texturePath, int modPriority)
{
    string textureName = Path.GetFileNameWithoutExtension(texturePath);
    string spriteName = textureName.Remove(textureName.LastIndexOf('_'));
    int spriteIndex = Convert.ToInt32(textureName.Remove(0, textureName.LastIndexOf('_') + 1));

    if (moddedSprites.ContainsKey(textureName) && moddedSprites[textureName] < modPriority)
    {
        Console.WriteLine($"Sprite {textureName} already replaced with a higher priority mod, pain ahead.");
        return;
    }

    moddedSprites[textureName] = modPriority;

    UndertaleSprite spriteToReplace = gameData.Sprites.First(x => x.Name.Content == spriteName);
    IConfigurationSection fileConfig;
    if (Path.Exists($"./{textureName}.ini"))
        fileConfig = new ConfigurationBuilder().AddIniFile($"./{textureName}.ini").Build().GetSection("ReplaceValues");

    if (spriteToReplace is null)
    {
        Console.WriteLine("Sprite not found, creating new sprite...");
        spriteToReplace = new UndertaleSprite() {
            Name = new UndertaleString(textureName.Substring(0, textureName.Length - textureName.LastIndexOf('_')))
            // Add the replace values from fileConfig
        };
        gameData.Sprites.Add(spriteToReplace);
    }

    spriteToReplace.Textures[spriteIndex].Texture.ReplaceTexture(Image.FromFile(texturePath));
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

void AddGlobalInitScript(string scriptName)
{
    var codeToAdd = gameData.Code.First(x => x.Name.Content == scriptName);

    if (codeToAdd is null)
    {
        Console.WriteLine($"Script {scriptName} not found, skipping global init script, pain head.");
        return;
    }

    gameData.GlobalInitScripts.Add(new UndertaleGlobalInit(){
        Code = codeToAdd
    });
}

void AddGameEndScript(string scriptName)
{
    var codeToAdd = gameData.Code.First(x => x.Name.Content == scriptName);

    if (codeToAdd is null)
    {
        Console.WriteLine($"Script {scriptName} not found, skipping game end script, pain head.");
        return;
    }

    gameData.GameEndScripts.Add(new UndertaleGlobalInit(){
        Code = codeToAdd
    });
}