#region Using Directives
using UndertaleModLib;
using UndertaleModLib.Models;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Util;
#endregion

#region Fields
string ogGameDataPath = args[0];
string moddedGameDataPath = args[1];
string exportPath = args[3]; // .gmmod folder
int numberOfErrors = 0;
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

    if (ogAsm == null || moddedAsm == null)
    {
        Console.WriteLine($"[{i}] Skipping Code export due to null reference.");
        numberOfErrors++;
        continue;
    }

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
var texturesProgressBar = new ProgressBar(0);
for (int i = 0; i < ogGameData.Sprites.Count; i++)
{
    spritesProgressBar.UpdateProgress(i);
    if (ogGameData.Sprites[i].Textures.Count == moddedGameData.Sprites[i].Textures.Count)
    {
        texturesProgressBar.Total = ogGameData.Sprites[i].Textures.Count;
        for (int j = 0; j < ogGameData.Sprites[i].Textures.Count; j++)
        {
            texturesProgressBar.UpdateProgress(j);
            var ogTexture = ogGameData.Sprites[i].Textures[j].Texture;
            var moddedTexture = moddedGameData.Sprites[i].Textures[j].Texture;

            if (ogTexture == null || moddedTexture == null)
            {
                Console.WriteLine($"[{i}] Skipping Sprite export due to null reference.");
                numberOfErrors++;
                continue;
            }

            string moddedTexturePageName = moddedTexture.TexturePage.Name.Content;

            if (ogTexture.TexturePage.Name.Content != moddedTexturePageName)
                break;

            if (!checkedPages.ContainsKey(moddedTexturePageName))
                checkedPages[moddedTexturePageName] = ogTexture.TexturePage.TextureData.TextureBlob.SequenceEqual(moddedTexture.TexturePage.TextureData.TextureBlob);

            if (checkedPages[moddedTexture.TexturePage.Name.Content])
                break;

            if (!Scripts.TextureEquals(ogTexture, moddedTexture))
            {
                Console.WriteLine($"[{i}][{j}] Exporting {moddedGameData.Sprites[i].Name.Content}...");
                Scripts.DumpSingleSpriteTexture(moddedGameData.Sprites[i], j);
            }
        }
        continue;
    }
    texturesProgressBar.Total = 1;
    texturesProgressBar.UpdateProgress(1);

    Console.WriteLine($"[{i}] Exporting {moddedGameData.Sprites[i].Name.Content}...");
    Scripts.DumpSprite(moddedGameData.Sprites[i]);
}
texturesProgressBar.Total = 0;
texturesProgressBar.UpdateProgress(0);
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

    if (ogTexture == null || moddedTexture == null)
    {
        Console.WriteLine($"[{i}] Skipping Fonts export due to null reference.");
        numberOfErrors++;
        continue;
    }

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

    if (ogTexture == null || moddedTexture == null)
    {
        Console.WriteLine($"[{i}] Skipping background export due to null reference.");
        numberOfErrors++;
        continue;
    }

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

#region FontData
Console.WriteLine("Exporting font data...");
var fontDataProgressBar = new ProgressBar(moddedGameData.Fonts.Count-1);
string[] properties = {
    "EmSize",
    "Bold",
    "Italic",
    "Charset",
    "AntiAliasing",
    "ScaleX",
    "ScaleY"
};
string[] glyphProperties = {
    "Character",
    "SourceX",
    "SourceY",
    "SourceWidth",
    "SourceHeight",
    "Shift",
    "Offset"
};
for (int i = 0; i < ogGameData.Fonts.Count; ++i)
{
    fontDataProgressBar.UpdateProgress(i);
    
    var ogFont = ogGameData.Fonts[i];
    var moddedFont = moddedGameData.Fonts[i];

    if (ogFont == null || moddedFont == null)
    {
        Console.WriteLine($"[{i}] Skipping FontData export due to null reference.");
        numberOfErrors++;
        continue;
    }

    if (ogFont.DisplayName.Content == moddedFont.DisplayName.Content && Scripts.PropertiesEqual<UndertaleFont>(ogFont, moddedFont, properties) && ogFont.Glyphs.Count == moddedFont.Glyphs.Count)
    {
        bool export = false;
        for (int j = 0; j < ogFont.Glyphs.Count; ++j)
        {
            if (!Scripts.PropertiesEqual<UndertaleFont.Glyph>(ogFont.Glyphs[j], moddedFont.Glyphs[j], glyphProperties))
            {
                export = true;
                break;
            }
        }
        if (!export)
            continue;
    }

    Console.WriteLine($"[{i}] Exporting {moddedFont.Name.Content}.csv...");
    Scripts.DumpFontData(moddedFont);
}
for (int i = ogGameData.Fonts.Count; i < moddedGameData.Fonts.Count; ++i)
{
    Console.WriteLine($"[{i}] Exporting {moddedGameData.Fonts[i].Name.Content}.csv...");
    Scripts.DumpFontData(moddedGameData.Fonts[i]);
    fontDataProgressBar.UpdateProgress(i);
}
Console.WriteLine();
#endregion

Console.WriteLine("Done!");
if (numberOfErrors > 1)
{
    Console.WriteLine($"There's a total of {numberOfErrors} errors during exporting");
}
Console.ReadKey();
#endregion
