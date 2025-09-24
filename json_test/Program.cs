using System.Text.Json;
using redb.Core.Models;

public class TestProps
{
    public string? Name { get; set; }
    public int Value { get; set; }
}

class Program
{
    static void Main()
    {
        // Создаем объект
        var obj = new RedbObject<TestProps>
        {
            id = 123,
            scheme_id = 456,
            name = "Test Object",
            properties = new TestProps { Name = "Test", Value = 42 }
        };

        Console.WriteLine("=== ТЕСТ JSON СЕРИАЛИЗАЦИИ ===");
        Console.WriteLine($"obj.id = {obj.id}");
        Console.WriteLine($"obj.Id = {obj.Id}");
        Console.WriteLine($"obj.scheme_id = {obj.scheme_id}");
        Console.WriteLine($"obj.SchemeId = {obj.SchemeId}");
        Console.WriteLine($"obj.name = {obj.name}");
        Console.WriteLine($"obj.Name = {obj.Name}");

        // Сериализуем в JSON
        var json = JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
        Console.WriteLine("\n=== JSON РЕЗУЛЬТАТ ===");
        Console.WriteLine(json);

        // Десериализуем обратно
        var restored = JsonSerializer.Deserialize<RedbObject<TestProps>>(json);
        Console.WriteLine("\n=== ПОСЛЕ ДЕСЕРИАЛИЗАЦИИ ===");
        Console.WriteLine($"restored.id = {restored?.id}");
        Console.WriteLine($"restored.Id = {restored?.Id}");
        Console.WriteLine($"restored.properties.Name = {restored?.properties?.Name}");
    }
}
