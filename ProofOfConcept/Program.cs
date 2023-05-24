// This program will be run from cmd as a proof of concept
// The first argument is the data.win location
// The second argument is the mod folder location

#region Using directives
using Microsoft.Extensions.Configuration;
using UndertaleModLib;
using UndertaleModLib.Models;
using UndertaleModLib.Compiler;
using System.Drawing;
#endregion

#region Fields
string gameDataPath = args[0];
string modsFolder = args[1];
string loaderConfigPath = $"{modsFolder}/loader.ini";
var loaderConfig = new ConfigurationBuilder().AddIniFile(loaderConfigPath).Build().GetSection("Loader");

Dictionary<string, int> moddedCodes = new Dictionary<string, int>();
Dictionary<string, int> moddedSprites = new Dictionary<string, int>();
Dictionary<string, int> moddedScripts = new Dictionary<string, int>();
List<ReplacedAssetInfo> replacedAssets = new List<ReplacedAssetInfo>();

UndertaleData gameData;

#endregion

#region Program
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
    
    if (Directory.Exists($"{folder}/Textures"))
    {
        Console.WriteLine("Loading texture...");
        foreach (string sprite in Directory.GetFiles($"{folder}/Textures").Where(x => !x.EndsWith(".ini")))
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
        foreach (IConfigurationSection pair in scriptsIni.GetSection("Scripts").GetChildren())
        {
            Console.WriteLine($"Loading script {pair.Key}");
            ReplaceScript(pair.Key, pair.Value!, false, Convert.ToInt32(modConfig["priority"]));
        }
        foreach (IConfigurationSection pair in scriptsIni.GetSection("Constructors").GetChildren())
        {
            Console.WriteLine($"Loading global script {pair.Key}");
            ReplaceScript(pair.Key, pair.Value!, true, Convert.ToInt32(modConfig["priority"]));
        }
    }

    if (Path.Exists($"{folder}/globalInit.ini"))
    {
        Console.WriteLine("Adding global init scripts...");
        var scriptsIni = new ConfigurationBuilder().AddIniFile($"{folder}/globalInit.ini").Build();
        foreach (var section in scriptsIni.GetChildren())
            AddScriptToSequence(section.Key, gameData.GlobalInitScripts);
    }

    if (Path.Exists($"{folder}/gameEndScripts.ini"))
    {
        Console.WriteLine("Adding game end scripts...");
        var scriptsIni = new ConfigurationBuilder().AddIniFile($"{folder}/gameEndScripts.ini").Build();
        foreach (var section in scriptsIni.GetChildren())
            AddScriptToSequence(section.Key, gameData.GameEndScripts);
    }
}

using (var stream = new FileStream(Path.GetDirectoryName(gameDataPath) + "/modded.win", FileMode.Create, FileAccess.ReadWrite))
    UndertaleIO.Write(stream, gameData);
#endregion

#region Methods
bool IsAssetUnavailable(Type assetType, string assetName, int modPriority)
{
    bool isUnavailable = replacedAssets.Any(x => x.assetName == assetName && x.modPriority < modPriority && x.assetType == assetType);

    if (isUnavailable)
        Console.WriteLine($"Asset {assetName} of type {assetType.Name} already replaced with a higher priority mod, pain ahead.");
    else
        replacedAssets.Add(new ReplacedAssetInfo() {
            assetName = assetName,
            modPriority = modPriority,
            assetType = assetType
        });

    return isUnavailable;
}

void ReplaceCode(string codePath, int modPriority)
{
    string codeName = Path.GetFileNameWithoutExtension(codePath);

    if (IsAssetUnavailable(typeof(UndertaleCode), codeName, modPriority))
        return;

    UndertaleCode codeToReplace = gameData.Code.First(x => x.Name.Content == codeName);

    if (codeToReplace is null)
    {
        Console.WriteLine($"Code {codeName} not found, creating new code.");
        codeToReplace = new UndertaleCode() {
            Name = new UndertaleString(codeName)
        };
        gameData.Code.Add(codeToReplace);
    }

    CompileContext context = Compiler.CompileGMLText(File.ReadAllText(codePath), gameData, codeToReplace);
    codeToReplace.Replace(context.ResultAssembly);

    if (Path.Exists($"./{codeName}.ini"))
    {
        IConfigurationSection fileConfig = new ConfigurationBuilder().AddIniFile("./code.ini").Build().GetSection(codeName);
        codeToReplace.LocalsCount = fileConfig["localsCount"] is not null ? Convert.ToUInt16(fileConfig["localsCount"]) : codeToReplace.LocalsCount;
        codeToReplace.ArgumentsCount = fileConfig["argumentsCount"] is not null ? Convert.ToUInt16(fileConfig["argumentsCount"]) : codeToReplace.ArgumentsCount;
        codeToReplace.Offset = fileConfig["offset"] is not null ? Convert.ToUInt32(fileConfig["offset"]) : codeToReplace.Offset;
    }
}

void ReplaceTexture(string texturePath, int modPriority)
{
    string textureName = Path.GetFileNameWithoutExtension(texturePath);
    string spriteName = textureName.Remove(textureName.LastIndexOf('_'));
    int textureIndex = Convert.ToInt32(textureName.Remove(0, textureName.LastIndexOf('_') + 1));

    if (IsAssetUnavailable(typeof(UndertaleSprite), spriteName, modPriority))
        return;

    UndertaleSprite textureSprite = gameData.Sprites.First(x => x.Name.Content == spriteName);

    if (textureSprite is null)
    {
        Console.WriteLine("Sprite not found, creating new sprite...");
        textureSprite = new UndertaleSprite() {
            Name = new UndertaleString(spriteName)
        };
        gameData.Sprites.Add(textureSprite);
    }

    Image imageToUse;
    try {
        imageToUse = Image.FromFile(texturePath);
    } catch (Exception e) {
        Console.WriteLine($"Error replacing texture {textureName}: {e.Message}. Pain ahead.");
        return;
    }

    UndertaleTexturePageItem textureToReplace = textureSprite.Textures[textureIndex].Texture;

    textureToReplace.SourceX = 0;
    textureToReplace.SourceY = 0;
    textureToReplace.SourceWidth = Convert.ToUInt16(imageToUse.Width);
    textureToReplace.TargetWidth = Convert.ToUInt16(imageToUse.Width);
    textureToReplace.SourceHeight = Convert.ToUInt16(imageToUse.Height);
    textureToReplace.TargetHeight = Convert.ToUInt16(imageToUse.Height);

    if (Path.Exists($"./textures.ini"))
    {
        var fileConfig = new ConfigurationBuilder().AddIniFile($"./textures.ini").Build().GetSection(textureName);
        textureToReplace.TargetX = fileConfig["targetX"] is not null ? Convert.ToUInt16(fileConfig["targetX"]) : textureToReplace.TargetX;
        textureToReplace.TargetY = fileConfig["targetY"] is not null ? Convert.ToUInt16(fileConfig["targetY"]) : textureToReplace.TargetY;
        textureToReplace.BoundingWidth = fileConfig["boundingWidth"] is not null ? Convert.ToUInt16(fileConfig["boundingWidth"]) : textureToReplace.BoundingWidth;
        textureToReplace.BoundingHeight = fileConfig["boundingHeight"] is not null ? Convert.ToUInt16(fileConfig["boundingHeight"]) : textureToReplace.BoundingHeight;
    }

    textureSprite.Textures[textureIndex].Texture.ReplaceTexture(Image.FromFile(texturePath));
}

void ReplaceScript(string scriptName, string codeName, bool isConstructor, int modPriority)
{
    if (IsAssetUnavailable(typeof(UndertaleScript), scriptName, modPriority))
        return;

    UndertaleCode codeToUse = gameData.Code.First(x => x.Name.Content == codeName);

    if (codeToUse is null)
    {
        Console.WriteLine($"Code {codeName} not found, skipping script {scriptName}, pain head.");
        return;
    }

    UndertaleScript scriptToReplace = gameData.Scripts.First(x => x.Name.Content == scriptName);

    if (scriptToReplace is null)
    {
        Console.WriteLine($"Script {scriptName} not found, creating new script.");
        scriptToReplace = new UndertaleScript() {
            Name = new UndertaleString(scriptName),
            IsConstructor = isConstructor
        };
        gameData.Scripts.Add(scriptToReplace);
    }

    scriptToReplace.Code = codeToUse;
}

void AddScriptToSequence(string codeName, IList<UndertaleGlobalInit> list)
{
    var codeToAdd = gameData.Code.First(x => x.Name.Content == codeName);

    if (codeToAdd is null)
    {
        Console.WriteLine($"Code {codeName} not found, skipping script, pain head.");
        return;
    }

    list.Add(new UndertaleGlobalInit(){
        Code = codeToAdd
    });
}
#endregion