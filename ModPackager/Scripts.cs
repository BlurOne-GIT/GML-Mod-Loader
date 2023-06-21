using UndertaleModLib;
using UndertaleModLib.Models;
using UndertaleModLib.Decompiler;

public static class Scripts
{
    public static UndertaleData Data { get; set; }
    public static string ExportFolder { get; set; } = @"./mod.gmmod";
    private static string CodeFolder { get => Path.Combine(ExportFolder, "Code"); }

    // Modified version of the ExportASM.csx script from UTMT
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
}