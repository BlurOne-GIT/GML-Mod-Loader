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
if (ogGameData.Code.Count < moddedGameData.Code.Count)
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
    if (ogGameData.Sprites[i].Textures.Count == moddedGameData.Sprites[i].Textures.Count)
    {
        bool allEqual = true;
        for (int j = 0; j < ogGameData.Sprites[i].Textures.Count; j++)
            if (worker.GetTextureFor(ogGameData.Sprites[i].Textures[j].Texture, "").Equals(worker.GetTextureFor(moddedGameData.Sprites[i].Textures[j].Texture, "")))
            {
                allEqual = false;
                break;
            }
        if (allEqual)
            continue;
    }

    Console.WriteLine("Exporting " + moddedGameData.Sprites[i].Name.Content + "...");
    Scripts.DumpSprite(moddedGameData.Sprites[i]);
}
#endregion

Console.WriteLine("Done!");
Console.ReadKey();

#endregion