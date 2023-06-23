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

Console.WriteLine("Reading original game data...");
using (FileStream stream = new FileStream(ogGameDataPath, FileMode.Open))
    ogGameData = UndertaleIO.Read(stream);
Console.WriteLine("Reading modded game data...");
using (FileStream stream = new FileStream(moddedGameDataPath, FileMode.Open))
    moddedGameData = UndertaleIO.Read(stream);
Scripts.Data = moddedGameData;

#region Code
Console.WriteLine("Exporting code...");
for (int i = 0; i < ogGameData.Code.Count; i++)
{
    if (ogGameData.Code[i].ParentEntry != null)
        continue;

    string ogAsm = (ogGameData.Code[i] != null ? ogGameData.Code[i].Disassemble(ogGameData.Variables, ogGameData.CodeLocals.For(ogGameData.Code[i])) : "");
    string moddedAsm = (moddedGameData.Code[i] != null ? moddedGameData.Code[i].Disassemble(moddedGameData.Variables, moddedGameData.CodeLocals.For(moddedGameData.Code[i])) : "");
    
    if (ogAsm == moddedAsm)
        continue;

    Console.WriteLine("Exporting " + moddedGameData.Code[i].Name.Content + ".asm...");
    
    //Scripts.ExportASM(moddedGameData.Code[i]);
    Scripts.ExportASM(moddedAsm, moddedGameData.Code[i].Name.Content);
}
for (int i = ogGameData.Code.Count; i < moddedGameData.Code.Count; i++)
    if (moddedGameData.Code[i].ParentEntry is null)
    {
        Console.WriteLine("Exporting " + moddedGameData.Code[i].Name.Content + ".asm...");
        Scripts.ExportASM(moddedGameData.Code[i]);
    }
#endregion

#region Textures
// Sprites
TextureWorker worker = new();
for (int i = 0; i < ogGameData.Sprites.Count; i++)
{
    /*
    bool export = ogGameData.Sprites[i].Textures.Count != moddedGameData.Sprites[i].Textures.Count 
        || ogGameData.Sprites[i].Width != moddedGameData.Sprites[i].Width
        || ogGameData.Sprites[i].Height != moddedGameData.Sprites[i].Height
        || ogGameData.Sprites[i].MarginBottom != moddedGameData.Sprites[i].MarginBottom
        || ogGameData.Sprites[i].MarginLeft != moddedGameData.Sprites[i].MarginLeft
        || ogGameData.Sprites[i].MarginRight != moddedGameData.Sprites[i].MarginRight
        || ogGameData.Sprites[i].MarginTop != moddedGameData.Sprites[i].MarginTop
        || ogGameData.Sprites[i].OriginXWrapper
    */

    if (ogGameData.Sprites[i].Textures.Count == moddedGameData.Sprites[i].Textures.Count && ogGameData.Sprites[i].Width == moddedGameData.Sprites[i].Width && ogGameData.Sprites[i].Height == moddedGameData.Sprites[i].Height)
    {
        bool allEqual = true;
        for (int j = 0; j < ogGameData.Sprites[i].Textures.Count; j++)
        {


            if (worker.GetTextureFor(ogGameData.Sprites[i].Textures[j].Texture, "").Equals(worker.GetTextureFor(moddedGameData.Sprites[i].Textures[j].Texture, "")))
            {
                allEqual = false;
                break;
            }
        }
        if (allEqual)
            continue;
    }

    Console.WriteLine("Exporting " + moddedGameData.Sprites[i].Name.Content + "...");
    Scripts.DumpSprite(moddedGameData.Sprites[i]);
}
for (int i = ogGameData.Sprites.Count; i < moddedGameData.Sprites.Count; i++)
{
    Console.WriteLine("Exporting " + moddedGameData.Sprites[i].Name.Content + "...");
    Scripts.DumpSprite(moddedGameData.Sprites[i]);
}

// Fonts
for (int i = 0; i < ogGameData.Fonts.Count; i++)
{
    if (worker.GetTextureFor(ogGameData.Fonts[i].Texture, "").Equals(worker.GetTextureFor(moddedGameData.Fonts[i].Texture, "")))
        continue;
    
    Console.WriteLine("Exporting " + moddedGameData.Fonts[i].Name.Content + "...");
    Scripts.DumpFont(moddedGameData.Fonts[i]);
}
for (int i = ogGameData.Fonts.Count; i < moddedGameData.Fonts.Count; i++)
{
    Console.WriteLine("Exporting " + moddedGameData.Fonts[i].Name.Content + "...");
    Scripts.DumpFont(moddedGameData.Fonts[i]);
}

// Backgrounds
for (int i = 0; i < ogGameData.Backgrounds.Count; i++)
{
    if (worker.GetTextureFor(ogGameData.Backgrounds[i].Texture, "").Equals(worker.GetTextureFor(moddedGameData.Backgrounds[i].Texture, "")))
        continue;
    
    Console.WriteLine("Exporting " + moddedGameData.Backgrounds[i].Name.Content + "...");
    Scripts.DumpBackground(moddedGameData.Backgrounds[i]);
}
for (int i = ogGameData.Backgrounds.Count; i < moddedGameData.Backgrounds.Count; i++)
{
    Console.WriteLine("Exporting " + moddedGameData.Backgrounds[i].Name.Content + "...");
    Scripts.DumpBackground(moddedGameData.Backgrounds[i]);
}
#endregion

Console.WriteLine("Done!");
Console.ReadKey();

#endregion