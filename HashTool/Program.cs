using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace HashTool
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.Error.WriteLine("Usage: HashTool <password>");
                return;
            }

            string password = args[0];

            // 1) Configure the hasher exactly as in your app:
            var options = Options.Create(new PasswordHasherOptions
            {
                CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV3,
                IterationCount = 100_000
            });
            var hasher = new PasswordHasher<IdentityUser>(options);

            // 2) Hash:
            string hash = hasher.HashPassword(user: null, password);
            Console.WriteLine($"Hash: {hash}");
        }
    }
}