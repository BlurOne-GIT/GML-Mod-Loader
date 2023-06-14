// This program will be run from cmd as a proof of concept
// The first argument is the data.win location
// The second argument is the mod folder location

#region Using directives
using Microsoft.Extensions.Configuration;
using UndertaleModLib;
using UndertaleModLib.Models;
using UndertaleModLib.Compiler;
using System;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
#endregion

#region Fields
string gameDataPath = args[0];
string modsFolder = args[1];
string loaderConfigPath = $"{modsFolder}/loader.ini";
var loaderConfig = new ConfigurationBuilder().AddIniFile(loaderConfigPath).Build().GetSection("Loader");
int modPriority;
string modName;
bool multiplePropertyReplacement = Convert.ToBoolean(loaderConfig["multiplePropertyReplacement"]);

Dictionary<string, int> moddedCodes = new Dictionary<string, int>();
Dictionary<string, int> moddedSprites = new Dictionary<string, int>();
Dictionary<string, int> moddedScripts = new Dictionary<string, int>();
List<ReplacedAssetInfo> replacedAssets = new List<ReplacedAssetInfo>();

string[] forbiddenFiles = {"data.win"};

UndertaleData gameData;
string gameFolderPath = Path.GetDirectoryName(gameDataPath)!;

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
    modPriority = Convert.ToInt32(modConfig["priority"]);
    modName = modConfig["name"] ?? Path.GetFileNameWithoutExtension(folder)!;

    if (!Convert.ToBoolean(modConfig["enabled"]) || modConfig["game"] != gameData.GeneralInfo.Name.Content)
        continue;

    /*
    if (modConfig["gameVersion"] != gameData.GeneralInfo.Release.ToString() && !Convert.ToBoolean(loaderConfig["ignoreVersionMismatch"]))
    {
        Console.WriteLine($"Mod version does not match game version, skipping {folder}.");
        continue;
    }
    */
    
    Console.WriteLine($"Loading mod {modName}");

    if (Path.Exists($"{folder}/sprites.ini"))
    {
        Console.WriteLine("Loading sprites...");
        var spritesIni = new ConfigurationBuilder().AddIniFile($"{folder}/sprites.ini").Build();
        foreach (IConfigurationSection sprite in spritesIni.GetChildren())
        {
            Console.WriteLine($"Loading sprite {sprite.Key}");
            ReplaceSprite(sprite);
        }
    }
    
    if (Directory.Exists($"{folder}/Textures"))
    {
        Console.WriteLine("Loading texture...");
        foreach (string sprite in Directory.GetFiles($"{folder}/Textures").Where(x => !x.EndsWith(".ini")))
        {
            Console.WriteLine($"Loading texture {sprite}");
            ReplaceTexture(sprite);
        }
    }

    if (Directory.Exists($"{folder}/Code"))
    {
        Console.WriteLine("Loading code...");
        foreach (string code in Directory.GetFiles($"{folder}/Code").Where(x => x.EndsWith(".gml")))
        {
            Console.WriteLine($"Loading code {code}");
            ReplaceCode(code);
        }
    }

    if (Path.Exists($"{folder}/scripts.ini"))
    {
        Console.WriteLine("Loading scripts...");
        var scriptsIni = new ConfigurationBuilder().AddIniFile($"{folder}/scripts.ini").Build();
        foreach (IConfigurationSection pair in scriptsIni.GetSection("Scripts").GetChildren())
        {
            Console.WriteLine($"Loading script {pair.Key}");
            ReplaceScript(pair.Key, pair.Value!, false);
        }
        foreach (IConfigurationSection pair in scriptsIni.GetSection("Constructors").GetChildren())
        {
            Console.WriteLine($"Loading global script {pair.Key}");
            ReplaceScript(pair.Key, pair.Value!, true);
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

    if (Path.Exists($"{folder}/objects.ini"))
    {
        Console.WriteLine("Modifying objects...");
        var objectsIni = new ConfigurationBuilder().AddIniFile($"{folder}/objects.ini").Build();
        foreach (IConfigurationSection objectSection in objectsIni.GetChildren())
        {
            Console.WriteLine($"Modifying object {objectSection.Key}");
            ModifyObject(objectSection);
        }
    }

    if (Directory.Exists($"{folder}/ObjectPhysicsShapeVertices"))
    {
        Console.WriteLine("Modifying object physics shape vertices...");
        foreach (string objectIniPath in Directory.GetFiles($"{folder}/ObjectPhysicsShapeVertices").Where(x => x.EndsWith(".ini")))
        {
            string objectName = Path.GetFileNameWithoutExtension(objectIniPath);
            Console.WriteLine($"Modifying object {objectName}");
            var fileConfig = new ConfigurationBuilder().AddIniFile(objectIniPath).Build();
            ModifyObjectPhysicsShapeVertices(Path.GetFileNameWithoutExtension(objectName), fileConfig.GetSection("Remove"), true);
            ModifyObjectPhysicsShapeVertices(Path.GetFileNameWithoutExtension(objectName), fileConfig.GetSection("Add"), false);
        }
    }

    if (Directory.Exists($"{folder}/RemoveObjectEvents"))
    {
        Console.WriteLine("Removing object events...");
        foreach (string objectIniPath in Directory.GetFiles($"{folder}/RemoveObjectEvents").Where(x => x.EndsWith(".ini")))
        {
            string objectName = Path.GetFileNameWithoutExtension(objectIniPath);
            Console.WriteLine($"Modifying object {objectName}");
            var fileConfig = new ConfigurationBuilder().AddIniFile(objectIniPath).Build();
            ModifyObjectEvents(objectName, fileConfig, true);
        }
    }

    if (Directory.Exists($"{folder}/AddObjectEvents"))
    {
        Console.WriteLine("Adding object events...");
        foreach (string objectIniPath in Directory.GetFiles($"{folder}/AddObjectEvents").Where(x => x.EndsWith(".ini")))
        {
            string objectName = Path.GetFileNameWithoutExtension(objectIniPath);
            Console.WriteLine($"Modifying object {objectName}");
            var fileConfig = new ConfigurationBuilder().AddIniFile(objectIniPath).Build();
            ModifyObjectEvents(objectName, fileConfig, false);
        }
    }

    if (Path.Exists($"{folder}/rooms.ini"))
    {
        Console.WriteLine("Modifying rooms...");
        var roomsIni = new ConfigurationBuilder().AddIniFile($"{folder}/rooms.ini").Build();
        foreach (IConfigurationSection roomSection in roomsIni.GetChildren())
        {
            Console.WriteLine($"Modifying room {roomSection.Key}");
            ModifyRoomValues(roomSection);
        }
    }

    if (Directory.Exists($"{folder}/RoomBackgrounds"))
    {
        Console.WriteLine("Modifying room backgrounds...");
        foreach (string roomIniPath in Directory.GetFiles($"{folder}/RoomBackgrounds").Where(x => x.EndsWith(".ini")))
        {
            string roomName = Path.GetFileNameWithoutExtension(roomIniPath);
            Console.WriteLine($"Modifying room {roomName}");
            var fileConfig = new ConfigurationBuilder().AddIniFile(roomIniPath).Build();
            foreach (var section in fileConfig.GetChildren())
                ModifyRoomBackground(roomName, section);
        }
    }

    if (Directory.Exists($"{folder}/ExternalAssets") && Path.Exists($"{folder}/ExternalAssets/externalAssets.ini"))
    {
        Console.WriteLine("Copying external assets...");
        var iniFile = new ConfigurationBuilder().AddIniFile($"{folder}/ExternalAssets/externalAssets.ini").Build().GetSection("Locations");
        foreach (string file in Directory.GetFiles($"{folder}/ExternalAssets").Where(x => !x.EndsWith("externalAssets.ini")))
        {
            Console.WriteLine($"Copying {file}");
            string fileName = Path.GetFileName(file);
            string destination = $"{gameFolderPath}/{iniFile[fileName]!}{(iniFile[fileName]!.EndsWith("/") ? "" : "/")}{fileName}";
            if (destination.Contains("..") || destination.Contains("%") || destination.Contains(".exe") || destination.Contains(".dll"))
            {
                Console.WriteLine("Invalid destination, skipping");
                continue;
            }

            CopyExternalAsset(file, destination);
        }
    }
}

using (var stream = new FileStream($"{gameFolderPath}/modded.win", FileMode.Create, FileAccess.ReadWrite))
    UndertaleIO.Write(stream, gameData);

Console.WriteLine("Done");

Console.ReadKey();
#endregion

#region Methods
bool IsAssetUnavailable(Type assetType, string assetName, string? property = null)
{
    bool isUnavailable = replacedAssets.Any(x => x.assetName == assetName && x.modPriority < modPriority && x.assetType == assetType && x.propertyName == property);

    if (isUnavailable)
        Console.WriteLine($"Asset {assetName} of type {assetType.Name} already replaced with a higher priority mod, pain ahead.");
    else
        replacedAssets.Add(new ReplacedAssetInfo() {
            assetName = assetName,
            modPriority = modPriority,
            modName = modName,
            assetType = assetType,
            propertyName = property
        });

    return isUnavailable;
}

void NamedValueReplacer(UndertaleNamedResource undertaleClass, string propertyName, string? value, string[]? nonModifiableProperties = null) =>
    ValueReplacer(undertaleClass, undertaleClass.Name.Content, propertyName, value, nonModifiableProperties);

void ValueReplacer(UndertaleObject undertaleObject, string objectName, string propertyName, string? value, string[]? nonModifiableProperties = null)
{
    if (IsAssetUnavailable(undertaleObject.GetType(), objectName, propertyName) || value is null || value is "" || nonModifiableProperties?.Contains(value) == true)
        return;

    PropertyInfo? property = undertaleObject.GetType().GetProperty(propertyName);
    
    if (property is null || !property.CanWrite)
    {
        Console.WriteLine($"Property {propertyName} for type {undertaleObject.GetType()} (asset {objectName}) not found or not writable, skipping.");
        return;
    }

    if (property.PropertyType.IsSubclassOf(typeof(UndertaleNamedResource)))
    {
        var method = typeof(UndertaleValueLookupHelper).GetMethod("UndertaleValueLookup")!.MakeGenericMethod(property.PropertyType);
        var helper = new UndertaleValueLookupHelper();
        var result = method.Invoke(helper, new object[] { value, gameData });

        if (result is null)
        {
            Console.WriteLine($"Failed to find asset {value} of type {property.PropertyType}, skipping.");
            return;
        }

        property.SetValue(undertaleObject, result);
        Console.WriteLine($"Set property {propertyName} of asset {objectName} to {value}.");
        return;
    }

    if (property.PropertyType == typeof(UndertaleString))
    {
        property.SetValue(undertaleObject, gameData.Strings.MakeString(value));
        Console.WriteLine($"Set property {propertyName} of asset {objectName} to {value}.");
        return;
    }

    try
    {
        property.SetValue(undertaleObject, Convert.ChangeType(value, property.PropertyType));
        Console.WriteLine($"Set property {propertyName} of asset {objectName} to {value}.");
    }
    catch (Exception)
    {
        Console.WriteLine($"Failed to convert {value} to type {property.PropertyType}, skipping.");
    }
}

T? UndertaleValueLookup<T>(string name)
    where T : UndertaleNamedResource
        => new List<T>((gameData[typeof(T)] as IList<T>)!).FirstOrDefault((x => x!.Name.Content == name), default(T));

void ReplaceCode(string codePath)
{
    string codeName = Path.GetFileNameWithoutExtension(codePath);

    if (IsAssetUnavailable(typeof(UndertaleCode), codeName))
        return;

    UndertaleCode? codeToReplace = gameData.Code.FirstOrDefault((x => x!.Name.Content == codeName), null);

    if (codeToReplace is null)
    {
        Console.WriteLine($"Code {codeName} not found, creating new code.");
        codeToReplace = new UndertaleCode() {
            Name = gameData.Strings.MakeString(codeName)
        };

        if (gameData.GeneralInfo.BytecodeVersion > 14)
        {
            UndertaleCodeLocals locals = new UndertaleCodeLocals(){
                Name = codeToReplace.Name
            };
            UndertaleCodeLocals.LocalVar argsLocal = new UndertaleCodeLocals.LocalVar(){
                Name = gameData.Strings.MakeString("arguments"),
                Index = 0
            };
            locals.Locals.Add(argsLocal);
            codeToReplace.LocalsCount = 1;
            gameData.CodeLocals.Add(locals);
        }

        gameData.Code.Add(codeToReplace);
    }

    CompileContext context = Compiler.CompileGMLText(File.ReadAllText(codePath), gameData, codeToReplace);
    codeToReplace.Replace(context.ResultAssembly);

    if (!Path.Exists(Path.GetDirectoryName(codePath) + "/code.ini"))
        return;
    
    IConfigurationSection fileConfig = new ConfigurationBuilder().AddIniFile(Path.GetDirectoryName(codePath) + "./code.ini").Build().GetSection(codeName);
    string[] nonModifiableProperties = {
        "CurrCodeIndex",
        "Length",
        "Name"
    };
    foreach (var pair in fileConfig.GetChildren())
        NamedValueReplacer(codeToReplace, pair.Key, pair.Value, nonModifiableProperties);
}

void ReplaceTexture(string texturePath)
{
    string textureName = Path.GetFileNameWithoutExtension(texturePath);
    string spriteName = textureName.Remove(textureName.LastIndexOf('_'));
    int textureIndex = Convert.ToInt32(textureName.Remove(0, textureName.LastIndexOf('_') + 1));

    if (IsAssetUnavailable(typeof(UndertaleSprite), spriteName))
        return;

    UndertaleSprite? textureSprite = gameData.Sprites.FirstOrDefault((x => x!.Name.Content == spriteName), null);

    if (textureSprite is null)
    {
        Console.WriteLine("Sprite not found, creating new sprite...");
        textureSprite = new UndertaleSprite() {
            Name = gameData.Strings.MakeString(spriteName)
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

    ushort width = Convert.ToUInt16(imageToUse.Width);
    ushort height = Convert.ToUInt16(imageToUse.Height);

    if (textureToReplace.SourceWidth < width || textureToReplace.SourceHeight < height)
    {
        Console.WriteLine($"Texture {textureName} has different size than original, creating it's own embedded texture.");

        textureToReplace.SourceX = 0;
        textureToReplace.SourceY = 0;
        var embeddedTexture = new UndertaleEmbeddedTexture(){
            Name = new UndertaleString($"Texture {gameData.EmbeddedTextures.Count}"),
            TextureWidth = width,
            TextureHeight = height,
            Scaled = 1,
            GeneratedMips = 0,
            TextureData = new UndertaleEmbeddedTexture.TexData() {
                TextureBlob = File.ReadAllBytes(texturePath)
            }
        };

        gameData.EmbeddedTextures.Add(embeddedTexture);
        textureToReplace.TexturePage = embeddedTexture;

    }

    textureToReplace.SourceWidth = width;
    textureToReplace.TargetWidth = width;
    textureToReplace.SourceHeight = height;
    textureToReplace.TargetHeight = height;

    if (Path.Exists(Path.GetDirectoryName(texturePath) + "/texture.ini"))
    {
        var fileConfig = new ConfigurationBuilder().AddIniFile(Path.GetDirectoryName(texturePath) + "./textures.ini").Build().GetSection(textureName);
        string[] nonModifiableProperties = {
            "SourceX",
            "SourceY",
            "SourceWidth",
            "SourceHeight",
            "TargetWidth",
            "TargetHeight",
            "TexturePage",
            "Name"
        };
        foreach (var pair in fileConfig.GetChildren())
            NamedValueReplacer(textureToReplace, pair.Key, pair.Value, nonModifiableProperties);
    }

    textureToReplace.ReplaceTexture(imageToUse);
}

void ReplaceScript(string scriptName, string codeName, bool isConstructor)
{
    if (IsAssetUnavailable(typeof(UndertaleScript), scriptName))
        return;

    UndertaleCode codeToUse = gameData.Code.First(x => x.Name.Content == codeName);

    if (codeToUse is null)
    {
        Console.WriteLine($"Code {codeName} not found, skipping script {scriptName}, pain head.");
        return;
    }

    UndertaleScript? scriptToReplace = gameData.Scripts.FirstOrDefault((x => x!.Name.Content == scriptName), null);

    if (scriptToReplace is null)
    {
        Console.WriteLine($"Script {scriptName} not found, creating new script.");
        scriptToReplace = new UndertaleScript() {
            Name = gameData.Strings.MakeString(scriptName),
            IsConstructor = isConstructor
        };
        gameData.Scripts.Add(scriptToReplace);
    }

    scriptToReplace.Code = codeToUse;
}

void ReplaceSprite(IConfigurationSection section)
{
    string spriteName = section.Key;

    if (IsAssetUnavailable(typeof(UndertaleSprite), spriteName) && !multiplePropertyReplacement)
        return;
    
    UndertaleSprite? spriteToReplace = gameData.Sprites.FirstOrDefault((x => x!.Name.Content == spriteName), null);

    if (spriteToReplace is null)
    {
        Console.WriteLine($"Sprite {spriteName} not found, creating new sprite.");
        spriteToReplace = new UndertaleSprite() {
            Name = gameData.Strings.MakeString(spriteName)
        };
        gameData.Sprites.Add(spriteToReplace);
    }

    string[] nonModifiableProperties = {
        "SWFVersion",
        "Textures",
        "CollisionMasks",
        "Name"
    };
    foreach (var pair in section.GetChildren())
        NamedValueReplacer(spriteToReplace, pair.Key, pair.Value, nonModifiableProperties);
    
    // TODO
    //spriteToReplace.CollisionMasks
}

void ModifyRoomValues(IConfigurationSection section)
{
    string roomName = section.Key;

    if (IsAssetUnavailable(typeof(UndertaleRoom), roomName))
        return;
    
    UndertaleRoom? roomToModify = gameData.Rooms.FirstOrDefault((x => x!.Name.Content == roomName), null);

    if (roomToModify is null)
    {
        Console.WriteLine($"Room {roomName} not found, creating new room.");
        roomToModify = new UndertaleRoom() {
            Name = gameData.Strings.MakeString(roomName)
        };
        gameData.Rooms.Add(roomToModify);
    }

    string[] nonModifiableProperties = {
        "Backgrounds",
        "Flags",
        "GameObjects",
        "Layers",
        "Name",
        "Sequences",
        "Tiles",
        "Views"
    };
    foreach (var pair in section.GetChildren())
        NamedValueReplacer(roomToModify, pair.Key, pair.Value, nonModifiableProperties);

    if (section["Flags"] is null or "") return;
    
    roomToModify.Flags = 0;

    if (section["Flags"]!.Contains("EnableViews"))
        roomToModify.Flags |= UndertaleRoom.RoomEntryFlags.EnableViews;

    if (section["Flags"]!.Contains("ShowColor"))
        roomToModify.Flags |= UndertaleRoom.RoomEntryFlags.ShowColor;

    if (section["Flags"]!.Contains("ClearDisplayBuffer"))
        roomToModify.Flags |= UndertaleRoom.RoomEntryFlags.ClearDisplayBuffer;

    if (section["Flags"]!.Contains("IsGMS2_3"))
        roomToModify.Flags |= UndertaleRoom.RoomEntryFlags.IsGMS2_3;

    if (section["Flags"]!.Contains("IsGMS2"))
        roomToModify.Flags |= UndertaleRoom.RoomEntryFlags.IsGMS2;
}

void ModifyRoomBackground(string roomName, IConfigurationSection section)
{
    if (IsAssetUnavailable(typeof(UndertaleRoom), roomName))
        return;

    var roomToModify = gameData.Rooms.FirstOrDefault((x => x!.Name.Content == roomName), null);

    if (roomToModify is null)
    {
        Console.WriteLine($"Room {roomName} not found, skipping.");
        return;
    }

    string layerName = section.Key;

    var layerOfBackground = roomToModify.Layers.FirstOrDefault((x => x!.LayerName.Content == layerName), null);
    
    if (layerOfBackground is null || layerOfBackground.LayerType is not UndertaleRoom.LayerType.Background)
    {
        Console.WriteLine($"Layer of type background {layerName} not found, skipping.");
        // TODO: Copy AddLayer function from UndertaleModTool UndertaleRoomEditor.xaml.cs
        return;
    }

    string[] nonModifiableProperties = {
        "Color",
        "ParentLayer"
    };
    foreach (var pair in section.GetChildren())
        ValueReplacer(layerOfBackground.BackgroundData, layerName, pair.Key, pair.Value, nonModifiableProperties);

    if (section["Color"] is null or "") return;

    layerOfBackground.BackgroundData.Color = Convert.ToUInt32(section["Color"]!, 16);
}

void ModifyRoomView(string roomName, IConfigurationSection section)
{
    if (IsAssetUnavailable(typeof(UndertaleRoom), roomName))
        return;

    var roomToModify = gameData.Rooms.FirstOrDefault((x => x!.Name.Content == roomName), null);

    if (roomToModify is null)
    {
        Console.WriteLine($"Room {roomName} not found, skipping.");
        return;
    }

    int viewIndex = Convert.ToInt32(section.Key);

    var viewToModify = roomToModify.Views[viewIndex];
    
    if (viewToModify is null)
    {
        Console.WriteLine($"View {viewIndex} not found, skipping.");
        return;
    }

    foreach (var pair in section.GetChildren())
        ValueReplacer(viewToModify, $"{viewIndex}", pair.Key, pair.Value);
}

void ModifyRoomTiles(string roomName, IConfigurationSection section)
{
    if (IsAssetUnavailable(typeof(UndertaleRoom), roomName))
        return;

    var roomToModify = gameData.Rooms.FirstOrDefault((x => x!.Name.Content == roomName), null);

    if (roomToModify is null)
    {
        Console.WriteLine($"Room {roomName} not found, skipping.");
        return;
    }

    string layerName = section.Key;

    var layerOfTiles = roomToModify.Layers.FirstOrDefault((x => x!.LayerName.Content == layerName), null);
    
    if (layerOfTiles is null || layerOfTiles.LayerType is not UndertaleRoom.LayerType.Tiles)
    {
        Console.WriteLine($"Layer of type tiles {layerName} not found, skipping.");
        // TODO: Copy AddLayer method from UndertaleModTool UndertaleRoomEditor.xaml.cs
        return;
    }

    string[] nonModifiableProperties = {
        "ParentLayer",
        "TileData"
    };
    foreach (var pair in section.GetChildren())
        ValueReplacer(layerOfTiles.TilesData, layerName, pair.Key, pair.Value, nonModifiableProperties);

    if (section["TileData"] is null or "") return;

    // TODO: Copy TileDataImport_Click method from UndertaleModTool UndertaleRoomEditor.xaml.cs    
}

/*
void ModifyInstances()
{

}

void ModifyRoomLayers()
{

}

void ModifyRoomSequences()
{

}
*/

void ModifyObject(IConfigurationSection section)
{
    string objectName = section.Key;

    if (IsAssetUnavailable(typeof(UndertaleObject), objectName))
        return;

    UndertaleGameObject? objectToModify = gameData.GameObjects.FirstOrDefault((x => x!.Name.Content == objectName), null);

    if (objectToModify is null)
    {
        Console.WriteLine($"Object {objectName} not found, creating new object.");
        objectToModify = new UndertaleGameObject() {
            Name = gameData.Strings.MakeString(objectName)
        };
        gameData.GameObjects.Add(objectToModify);
        return;
    }

    string[] nonModifiableProperties = {
        "Events",
        "Name",
        "PhysicsVertices"
    };

    foreach (var pair in section.GetChildren())
        NamedValueReplacer(objectToModify, pair.Key, pair.Value, nonModifiableProperties);
}

void ModifyObjectPhysicsShapeVertices(string objectName, IConfigurationSection section, bool isRemove)
{
    var objectToModify = gameData.GameObjects.First(x => x.Name.Content == objectName);

    if (objectToModify is null)
    {
        Console.WriteLine($"Object {objectName} not found, skipping object, pain head.");
        return;
    }

    foreach (var pair in section.GetChildren())
    {
        if (isRemove)
            objectToModify.PhysicsVertices.Remove(objectToModify.PhysicsVertices.First(x => x.X == Convert.ToSingle(pair.Key) && x.Y == Convert.ToSingle(pair.Value)));
        else
            objectToModify.PhysicsVertices.Add(new UndertaleGameObject.UndertalePhysicsVertex(){
                X = Convert.ToSingle(pair.Key),
                Y = Convert.ToSingle(pair.Value)
            });
    }
}

void ModifyObjectEvents(string objectName, IConfigurationRoot section, bool isRemove)
{
    var objectWhosEventsAreWishedToBeModified = gameData.GameObjects.First(x => x.Name.Content == objectName);

    if (objectWhosEventsAreWishedToBeModified is null)
    {
        Console.WriteLine($"Object {objectName} not found, skipping object, pain head.");
        return;
    }

    foreach (var eventSection in section.GetChildren())
    {
        var eventToModify = objectWhosEventsAreWishedToBeModified.Events[Convert.ToInt32(eventSection.Key)];

        if (eventToModify is null)
        {
            Console.WriteLine($"Event {eventSection.Key} not found, skipping event, pain head.");
            continue;
        }

        foreach (var subtypeSections in eventSection.GetChildren())
        {
            if (subtypeSections.Value is null)
                continue;

            if (isRemove)
            {
                Console.WriteLine($"Removing code {subtypeSections.Value} from event {eventSection.Key} subtype {subtypeSections.Key}.");
                eventToModify.Remove(eventToModify.First(x => x.EventSubtype == Convert.ToUInt16(subtypeSections.Key) && x.Actions[0].CodeId.Name.Content == subtypeSections.Value));
                continue;
            }

            Console.WriteLine($"Adding code {subtypeSections.Value} to event {eventSection.Key} subtype {subtypeSections.Key}.");

            var subtypeToModify = new UndertaleGameObject.Event(){
                EventSubtype = Convert.ToUInt16(subtypeSections.Key)
            };

            subtypeToModify.Actions.Add(new UndertaleGameObject.EventAction(){
                CodeId = gameData.Code.First(x => x.Name.Content == subtypeSections.Value)
            });

            eventToModify.Add(subtypeToModify);
        }
    }
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

void CopyExternalAsset(string fileToCopyPath, string destinationPath)
{
    if (IsAssetUnavailable(typeof(File), fileToCopyPath))
        return;

    File.Copy(fileToCopyPath, destinationPath, true);
}
#endregion