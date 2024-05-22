using System;
using System.Linq;

namespace Batcher.Utilities
{
    public static class Utility
    {
        public static string GenerateRandomString(int length, string chars = "abcdefghijklmnopqrstuvwxyz0123456789")
        {
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}