using System;

namespace ClientLocal.Decorator;

public static class DecoratorDemo
{
    public static void Run()
    {
        // 1. Crear un script base
        var script = new BasicScript("demo_nuevo.py", "def hello():\n    print('Hola mundo')");

        // 2. Decorarlo con firma
        var signed = new SignedScriptDecorator(script);

        Console.WriteLine("=== DEMO PATRÓN DECORATOR ===\n");

        Console.WriteLine($"Archivo: {signed.GetPath()}");
        Console.WriteLine($"Contenido:\n{signed.GetText()}\n");

        // 3. Verificar sin firma
        Console.WriteLine($"Estado inicial: {signed.VerifySignature()}");

        // 4. Firmar
        signed.Sign();
        Console.WriteLine($"Después de firmar: {signed.VerifySignature()}");

        // 5. Regenerar firma
        signed.RegenerateSignature();
        Console.WriteLine($"Después de regenerar: {signed.VerifySignature()}");
    }
}