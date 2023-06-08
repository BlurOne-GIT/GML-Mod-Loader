using UndertaleModLib;

class UndertaleValueLookupHelper
{
    T? UndertaleValueLookup<T>(string name, UndertaleData data)
    where T : UndertaleNamedResource
        => new List<T>((data[typeof(T)] as IList<T>)!).FirstOrDefault((x => x!.Name.Content == name), default(T));
}