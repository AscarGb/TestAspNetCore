using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Types
{
    public class Helper
    {
        IOptions<ConfigurationManager> _configurationManager;
        public Helper(IOptions<ConfigurationManager> configurationManager)
        {
            _configurationManager = configurationManager;
        }
        public string GetHash(string input)
        {
            using (HashAlgorithm hashAlgorithm = new SHA256CryptoServiceProvider())
            {
                byte[] byteValue = System.Text.Encoding.UTF8.GetBytes(input
                    + _configurationManager.Value.Hash);
                byte[] byteHash = hashAlgorithm.ComputeHash(byteValue);
                return Convert.ToBase64String(byteHash);
            }
        }
        public string GenerateRandomCryptographicKey(int keyLength)
        {
            using (RNGCryptoServiceProvider rngCryptoServiceProvider = new RNGCryptoServiceProvider())
            {
                byte[] randomBytes = new byte[keyLength];
                rngCryptoServiceProvider.GetBytes(randomBytes);
                return Convert.ToBase64String(randomBytes);
            }
        }        
    }
}
