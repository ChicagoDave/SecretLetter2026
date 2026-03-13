using System.Security.Cryptography;

string gameDir = Path.Combine(args.Length > 0 ? args[0] : ".", "SecretLetter", "GameFiles");
const string keykey = "Please don't share the game file";
const string ivString = "Hi from Textfyre";

foreach (var file in Directory.GetFiles(gameDir, "*.ulx"))
{
    var bytes = File.ReadAllBytes(file);
    if (bytes.Length < 36 || bytes[0] != (byte)'J' || bytes[1] != (byte)'A' || bytes[2] != (byte)'C' || bytes[3] != (byte)'K')
    {
        Console.WriteLine($"{Path.GetFileName(file)}: not encrypted, skipping");
        continue;
    }

    Console.Write($"{Path.GetFileName(file)}: decrypting... ");

    byte[] key = new byte[32];
    for (int i = 0; i < 32; i++)
        key[i] = (byte)(bytes[4 + i] ^ (byte)keykey[i]);

    byte[] iv = System.Text.Encoding.ASCII.GetBytes(ivString);

    using var aes = Aes.Create();
    aes.KeySize = 256;
    aes.Key = key;
    aes.IV = iv;

    byte[] encData = new byte[bytes.Length - 36];
    Array.Copy(bytes, 36, encData, 0, encData.Length);

    using var ms = new MemoryStream(encData);
    using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
    using var output = new MemoryStream();
    cs.CopyTo(output);
    var result = output.ToArray();

    if (result.Length >= 4 && result[0] == (byte)'G' && result[1] == (byte)'l' && result[2] == (byte)'u' && result[3] == (byte)'l')
    {
        File.WriteAllBytes(file, result);
        Console.WriteLine($"OK ({result.Length} bytes, Glul magic verified)");
    }
    else
    {
        Console.WriteLine($"FAILED (bad magic: {(char)result[0]}{(char)result[1]}{(char)result[2]}{(char)result[3]})");
    }
}
