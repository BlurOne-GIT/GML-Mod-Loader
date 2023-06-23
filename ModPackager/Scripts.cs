using System.Reflection;
using UndertaleModLib;
using UndertaleModLib.Models;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Util;

public static class Scripts
{
    public static UndertaleData Data { get; set; }
    public static string ExportFolder { get; set; } = @"./mod.gmmod";
    private static string CodeFolder { get => Path.Combine(ExportFolder, "Code"); }
    private static string TextureFolder { get => Path.Combine(ExportFolder, "Textures"); }
    private static string SpriteFolder { get => Path.Combine(TextureFolder, "Sprites"); }
    private static string FontFolder { get => Path.Combine(TextureFolder, "Fonts"); }
    private static string BackgroundFolder { get => Path.Combine(TextureFolder, "Backgrounds"); }
    

    // Modified versions of the ExportASM.csx functions from UTMT
    #region ExportASM.csx
    public static void ExportASM(UndertaleCode code)
    {
        string path = Path.Combine(CodeFolder, code.Name.Content + ".asm");
        if (!Directory.Exists(CodeFolder))
            Directory.CreateDirectory(CodeFolder);
        try
        {
            File.WriteAllText(path, (code != null ? code.Disassemble(Data.Variables, Data.CodeLocals.For(code)) : ""));
        }
        catch (Exception e)
        {
            File.WriteAllText(path, "/*\nDISASSEMBLY FAILED!\n\n" + e.ToString() + "\n*/"); // Please don't
        }
    }

    public static void ExportASM(string asm, string codeName)
    {
        string path = Path.Combine(CodeFolder, codeName + ".asm");
        if (!Directory.Exists(CodeFolder))
            Directory.CreateDirectory(CodeFolder);
        File.WriteAllText(path, asm);
    }
    #endregion

    // Modified versions of the ExportAllTexturesGroup.csx functions from UTMT
    #region ExportAllTexturesGroup.csx
    public static void DumpSprite(UndertaleSprite sprite)
    {
        TextureWorker worker = new();
        for (int i = 0; i < sprite.Textures.Count; i++)
            if (sprite.Textures[i]?.Texture != null)
            {
                UndertaleTexturePageItem tex = sprite.Textures[i].Texture;
                string sprFolder2 = Path.Combine(SpriteFolder, sprite.Name.Content);
                Directory.CreateDirectory(sprFolder2);
                worker.ExportAsPNG(tex, Path.Combine(sprFolder2, sprite.Name.Content + "_" + i + ".png"));
            }
    }

    public static void DumpFont(UndertaleFont font)
    {
        if (font.Texture is null)
            return;

        TextureWorker worker = new();
        UndertaleTexturePageItem tex = font.Texture;
        string fntFolder2 = Path.Combine(FontFolder, font.Name.Content);
        Directory.CreateDirectory(fntFolder2);
        worker.ExportAsPNG(tex, Path.Combine(fntFolder2, font.Name.Content + "_0.png"));
    }

    public static void DumpBackground(UndertaleBackground background)
    {
        if (background.Texture is null)
            return;
        
        TextureWorker worker = new();
        UndertaleTexturePageItem tex = background.Texture;
        string bgrFolder2 = Path.Combine(BackgroundFolder, background.Name.Content);
        Directory.CreateDirectory(bgrFolder2);
        worker.ExportAsPNG(tex, Path.Combine(bgrFolder2, background.Name.Content + "_0.png"));
    }
    #endregion

    // I hate the default .Equals() returning whether they are the same instance or not instead of comparing the class's values
    public static bool SimpleValuesComp<T>(T ogNamedResource, T moddedNamedResource, string[] propertiesToCompare)
    {
        if (ogNamedResource is null || moddedNamedResource is null)
            throw new NullReferenceException();

        foreach (string propertyName in propertiesToCompare)
        {
            var propertyToCompare = typeof(T).GetProperty(propertyName);
            if (propertiesToCompare is null)
                throw new NullReferenceException($"Property {propertyName} not found.");

            if (propertyToCompare.GetValue(ogNamedResource) != propertyToCompare.GetValue(moddedNamedResource))
                return true;
        }
    } 
}