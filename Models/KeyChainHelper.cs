using System;
using System.Text;
using System.Runtime.InteropServices;

namespace LichessClient.Models
{
    public class KeychainHelper
    {
        private const string k_ServiceName = "LichessOAuthTokenService";
        private const string k_AccountName = "LichessClient";
        private const int k_ErrSecItemNotFound = -25300; 
        
        public static void AddTokenToKeychain(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                throw new ArgumentException("Token cannot be null or empty.");
            }

            var serviceNameBytes = Encoding.UTF8.GetBytes(k_ServiceName);
            var accountNameBytes = Encoding.UTF8.GetBytes(k_AccountName);
            var passwordBytes = Encoding.UTF8.GetBytes(token);

            var result = Keychain.SecKeychainAddGenericPassword(IntPtr.Zero, (uint)serviceNameBytes.Length, serviceNameBytes, (uint)accountNameBytes.Length, accountNameBytes, (uint)passwordBytes.Length, passwordBytes, out var itemRef);

            if (result != 0)
            {
                throw new Exception($"Error adding token to Keychain. Error code: {result}");
            }
        }
        
        public static string GetTokenFromKeychain()
        {
            var serviceNameBytes = Encoding.UTF8.GetBytes(k_ServiceName);
            var accountNameBytes = Encoding.UTF8.GetBytes(k_AccountName);

            int result = Keychain.SecKeychainFindGenericPassword(IntPtr.Zero, (uint)serviceNameBytes.Length, serviceNameBytes, (uint)accountNameBytes.Length, accountNameBytes, out uint passwordLength, out IntPtr passwordData, out IntPtr itemRef);

            if (result != 0)
            {
                if (result == k_ErrSecItemNotFound)
                {
                    return null;
                }

                throw new Exception($"Error finding token in Keychain. Error code: {result}");
            }

            byte[] passwordBytes = new byte[passwordLength];
            Marshal.Copy(passwordData, passwordBytes, 0, (int)passwordLength);
            Keychain.SecKeychainItemFreeContent(IntPtr.Zero, passwordData);

            return Encoding.UTF8.GetString(passwordBytes);
        }
    }

    public static class Keychain
    {
        [DllImport("/System/Library/Frameworks/Security.framework/Security")]
        public static extern int SecKeychainAddGenericPassword(IntPtr keychain, uint serviceNameLength, byte[] serviceName, uint accountNameLength, byte[] accountName, uint passwordLength, byte[] passwordData, out IntPtr itemRef);

        [DllImport("/System/Library/Frameworks/Security.framework/Security")]
        public static extern int SecKeychainFindGenericPassword(IntPtr keychain, uint serviceNameLength, byte[] serviceName, uint accountNameLength, byte[] accountName, out uint passwordLength, out IntPtr passwordData, out IntPtr itemRef);

        [DllImport("/System/Library/Frameworks/Security.framework/Security")]
        public static extern int SecKeychainItemFreeContent(IntPtr itemContent, IntPtr passwordData);
    }

}

