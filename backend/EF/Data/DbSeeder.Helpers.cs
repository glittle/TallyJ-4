using System.Security.Cryptography;
using System.Text;

namespace Backend.EF.Data;

public static partial class DbSeeder
{
    private static Guid CreateGuid(string seed)
    {
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(seed));
        return new Guid(hash);
    }
}
