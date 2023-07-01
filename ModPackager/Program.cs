#region Using Directives
using System.Linq;
using UndertaleModLib;
using UndertaleModLib.Models;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Util;
#endregion

#region Fields
string ogGameDataPath = args[0];
string moddedGameDataPath = args[1];
string exportPath = args[2]; // .gmmod folder
Scripts.ExportFolder = exportPath;

UndertaleData ogGameData;
UndertaleData moddedGameData;
#endregion

#region Program
#region Init
Console.CursorVisible = false;
Console.WriteLine("Reading original game data...");
using (FileStream stream = new FileStream(ogGameDataPath, FileMode.Open))
    ogGameData = UndertaleIO.Read(stream);
Console.WriteLine("Reading modded game data...");
using (FileStream stream = new FileStream(moddedGameDataPath, FileMode.Open))
    moddedGameData = UndertaleIO.Read(stream);
Scripts.Data = moddedGameData;
Console.WriteLine();
#endregion

#region Code
Console.WriteLine("Exporting code...");
var codeProgressBar = new ProgressBar(moddedGameData.Code.Count-1);
for (int i = 0; i < ogGameData.Code.Count; i++)
{
    codeProgressBar.UpdateProgress(i);
    if (ogGameData.Code[i].ParentEntry != null)
        continue;

    string ogAsm = (ogGameData.Code[i] != null ? ogGameData.Code[i].Disassemble(ogGameData.Variables, ogGameData.CodeLocals.For(ogGameData.Code[i])) : "");
    string moddedAsm = (moddedGameData.Code[i] != null ? moddedGameData.Code[i].Disassemble(moddedGameData.Variables, moddedGameData.CodeLocals.For(moddedGameData.Code[i])) : "");
    
    if (ogAsm == moddedAsm)
        continue;

    Console.WriteLine($"[{i}] Exporting {moddedGameData.Code[i].Name.Content}.asm...");
    Scripts.ExportASM(moddedAsm, moddedGameData.Code[i].Name.Content);
}
for (int i = ogGameData.Code.Count; i < moddedGameData.Code.Count; i++)
    if (moddedGameData.Code[i].ParentEntry is null)
    {
        Console.WriteLine($"[{i}] Exporting {moddedGameData.Code[i].Name.Content}.asm...");
        Scripts.ExportASM(moddedGameData.Code[i]);
        codeProgressBar.UpdateProgress(i);
    }
Console.WriteLine();
#endregion

#region Textures
TextureWorker worker = new();
Dictionary<string, bool> checkedPages = new(); // OIA momento
#region Sprites
Console.WriteLine("Exporting sprite textures...");
var spritesProgressBar = new ProgressBar(moddedGameData.Sprites.Count-1);
for (int i = 0; i < ogGameData.Sprites.Count; i++)
{
    spritesProgressBar.UpdateProgress(i);
    if (ogGameData.Sprites[i].Textures.Count == moddedGameData.Sprites[i].Textures.Count)
    {
        bool export = false;
        for (int j = 0; j < ogGameData.Sprites[i].Textures.Count; j++)
        {
            var ogTexture = ogGameData.Sprites[i].Textures[j].Texture;
            var moddedTexture = moddedGameData.Sprites[i].Textures[j].Texture;
            string moddedTexturePageName = moddedTexture.TexturePage.Name.Content;

            if (ogTexture.TexturePage.Name.Content != moddedTexturePageName)
            {
                export = true;
                break;
            }

            if (!checkedPages.ContainsKey(moddedTexturePageName))
                checkedPages[moddedTexturePageName] = ogTexture.TexturePage.TextureData.TextureBlob.SequenceEqual(moddedTexture.TexturePage.TextureData.TextureBlob);

            if (checkedPages[moddedTexture.TexturePage.Name.Content])
                break;

            if (!Scripts.TextureEquals(ogTexture, moddedTexture))
            {
                export = true;
                break;
            }
        }
        if (!export)
            continue;
    }

    Console.WriteLine($"[{i}] Exporting {moddedGameData.Sprites[i].Name.Content}...");
    Scripts.DumpSprite(moddedGameData.Sprites[i]);
}
for (int i = ogGameData.Sprites.Count; i < moddedGameData.Sprites.Count; i++)
{
    Console.WriteLine($"[{i}] Exporting {moddedGameData.Sprites[i].Name.Content}...");
    Scripts.DumpSprite(moddedGameData.Sprites[i]);
    spritesProgressBar.UpdateProgress(i);
}
Console.WriteLine();
#endregion

#region Fonts
Console.WriteLine("Exporting font textures...");
var fontsProgressBar = new ProgressBar(moddedGameData.Fonts.Count-1);
for (int i = 0; i < ogGameData.Fonts.Count; i++)
{
    fontsProgressBar.UpdateProgress(i);

    var ogTexture = ogGameData.Fonts[i].Texture;
    var moddedTexture = moddedGameData.Fonts[i].Texture;
    string moddedTexturePageName = moddedTexture.TexturePage.Name.Content;

    if (ogTexture.TexturePage.Name.Content == moddedTexturePageName)
        continue;

    if (!checkedPages.ContainsKey(moddedTexturePageName))
        checkedPages[moddedTexturePageName] = ogTexture.TexturePage.TextureData.TextureBlob.SequenceEqual(moddedTexture.TexturePage.TextureData.TextureBlob);

    if (checkedPages[moddedTexture.TexturePage.Name.Content])
        continue;

    if (Scripts.TextureEquals(ogTexture, moddedTexture))
        continue;
    
    Console.WriteLine($"[{i}] Exporting {moddedGameData.Fonts[i].Name.Content}...");
    Scripts.DumpFont(moddedGameData.Fonts[i]);
}
for (int i = ogGameData.Fonts.Count; i < moddedGameData.Fonts.Count; i++)
{
    Console.WriteLine($"[{i}] Exporting {moddedGameData.Fonts[i].Name.Content}...");
    Scripts.DumpFont(moddedGameData.Fonts[i]);
    fontsProgressBar.UpdateProgress(i);
}
Console.WriteLine();
#endregion

#region Backgrounds
Console.WriteLine("Exporting background textures...");
var backgroundsProgressBar = new ProgressBar(moddedGameData.Backgrounds.Count-1);
for (int i = 0; i < ogGameData.Backgrounds.Count; i++)
{
    backgroundsProgressBar.UpdateProgress(i);

    var ogTexture = ogGameData.Backgrounds[i].Texture;
    var moddedTexture = moddedGameData.Backgrounds[i].Texture;
    string moddedTexturePageName = moddedTexture.TexturePage.Name.Content;

    if (ogTexture.TexturePage.Name.Content == moddedTexturePageName)
        continue;

    if (!checkedPages.ContainsKey(moddedTexturePageName))
        checkedPages[moddedTexturePageName] = ogTexture.TexturePage.TextureData.TextureBlob.SequenceEqual(moddedTexture.TexturePage.TextureData.TextureBlob);

    if (checkedPages[moddedTexture.TexturePage.Name.Content])
        continue;

    if (Scripts.TextureEquals(ogTexture, moddedTexture))
        continue;
    
    Console.WriteLine($"[{i}] Exporting {moddedGameData.Backgrounds[i].Name.Content}...");
    Scripts.DumpBackground(moddedGameData.Backgrounds[i]);
}
for (int i = ogGameData.Backgrounds.Count; i < moddedGameData.Backgrounds.Count; i++)
{
    Console.WriteLine($"[{i}] Exporting {moddedGameData.Backgrounds[i].Name.Content}...");
    Scripts.DumpBackground(moddedGameData.Backgrounds[i]);
    backgroundsProgressBar.UpdateProgress(i);
}
Console.WriteLine();
#endregion
#endregion

Console.WriteLine("Done!");
Console.ReadKey();
#endregion