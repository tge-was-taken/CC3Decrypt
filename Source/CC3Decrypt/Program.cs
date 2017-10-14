using System;
using System.IO;
using System.Reflection;

namespace CC3Decrypt
{
    class Program
    {
        public static Version Version = Assembly.GetExecutingAssembly().GetName().Version;

        public static string InputFilePath;

        public static string OutputFilePath;

        // we know the original values up to the length of the xor key itself
        // makes extracting the key easy
        public static byte[] BundleXorKeyOriginalValues = new byte[]
        {
            0x55, 0x6E, 0x69, 0x74, 0x79, 0x46, 0x53, 0x00, 0x00, 0x00, 0x00
        };

        public static byte[] BundleXorKey;

        static void DisplayUsage()
        {
            Console.WriteLine();
            Console.WriteLine( $"CC3Decrypt {Version.Major}.{Version.Minor}.{Version.Revision} by TGE (2017)" );
            Console.WriteLine( "Decrypts Unity asset bundle headers used by Chain Chronicle 3." );
            Console.WriteLine();
            Console.WriteLine( "Usage:");
            Console.WriteLine( "    CC3Decrypt <path to bundle file> [optional path to output file]" );
            Console.WriteLine();
            Console.WriteLine( "    If no output file path is specified, the original file path appended with '.decrypted' is used." );
        }

        static void Main( string[] args )
        {
            if ( !TryParseArguments( args ) )
            {
                Console.WriteLine( "Error: Failed to parse arguments" );
                DisplayUsage();
                return;
            }

            if ( !TryDecryptFile() )
            {
                Console.WriteLine( "Error: Failed to decrypt file" );
                return;
            }

            Console.WriteLine( "File successfully decrypted!" );
        }

        static bool TryParseArguments( string[] args )
        {
            if ( args.Length < 1 )
            {
                Console.WriteLine( "Error: Missing path to bundle file" );
                return false;
            }

            InputFilePath = args[0];
            if ( !File.Exists(InputFilePath) )
            {
                Console.WriteLine( $"Error: Specified bundle file doesn't exist: ('{InputFilePath}')" );
                return false;
            }

            if ( args.Length > 1 )
            {
                OutputFilePath = args[1];
            }
            else
            {
                OutputFilePath = InputFilePath + ".decrypted";
            }

            return true;
        }

        static bool TryDecryptFile()
        {
            using ( var stream = File.OpenRead( InputFilePath ) )
            {
                if ( stream.Length < 0x100 )
                {
                    Console.WriteLine( "Error: Bundle file is too small (less than 0x100 bytes)" );
                    return false;
                }

                ExtractXorKey( stream );
                DecryptHeader( stream );
            }

            return true;
        }

        static void ExtractXorKey( Stream stream )
        {
            var xoredValues = new byte[BundleXorKeyOriginalValues.Length];
            stream.Read( xoredValues, 0, xoredValues.Length );
            stream.Position = 0;

            BundleXorKey = new byte[BundleXorKeyOriginalValues.Length];
            for ( int i = 0; i < xoredValues.Length; i++ )
            {
                BundleXorKey[i] = ( byte )( xoredValues[i] ^ BundleXorKeyOriginalValues[i] );
            }
        }

        static void DecryptHeader( Stream input )
        {
            var headerBytes = new byte[0x100];
            input.Read( headerBytes, 0, headerBytes.Length );

            for ( int i = 0; i < headerBytes.Length; i++ )
            {
                headerBytes[i] ^= BundleXorKey[i % BundleXorKey.Length];
            }

            using ( var output = File.Create( OutputFilePath ) )
            {
                output.Write( headerBytes, 0, headerBytes.Length );
                input.CopyTo( output );
            }
        }
    }
}
