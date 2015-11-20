
using System;

using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.IO;
using System.Net;
using System.Net.Sockets;
using wolfSSL.CSharp;



public class wolfSSL_DTLS_PSK_Server
{


    /// <summary>
    /// Example of a PSK function call back
    /// </summary>
    /// <param name="ssl">pointer to ssl structure</param>
    /// <param name="identity">identity of client connecting</param>
    /// <param name="key">buffer to hold key</param>
    /// <param name="max_key">max key size</param>
    /// <returns>size of key set</returns>
    public static uint my_psk_server_cb(IntPtr ssl, string identity, IntPtr key, uint max_key)
    {
        /* perform a check on the identity sent across 
         * log function must be set for print out of logging information
         */
        wolfssl.log(1, "PSK Client Identity = " + identity);

        /* Use desired key, note must be a key smaller than max key size parameter 
            Replace this with desired key. Is trivial one for testing */
        if (max_key < 4)
            return 0;
        byte[] tmp = { 26, 43, 60, 77 };
        Marshal.Copy(tmp, 0, key, 4);

        return (uint)4;
    }


    public static void Main(string[] args)
    {
        IntPtr ctx;
        IntPtr ssl;

        /* These paths should be changed according to use */
        string fileCert = @"server-cert.pem";
        string fileKey = @"server-key.pem";
        StringBuilder dhparam = new StringBuilder("dh2048.pem");

        wolfssl.psk_delegate psk_cb = new wolfssl.psk_delegate(my_psk_server_cb);

        StringBuilder buff = new StringBuilder(1024);
        StringBuilder reply = new StringBuilder("Hello, this is the wolfSSL C# wrapper");

        wolfssl.Init();

        Console.WriteLine("Calling ctx Init from wolfSSL");
        ctx = wolfssl.CTX_dtls_new(wolfssl.useDTLSv1_2_server());
        Console.WriteLine("Finished init of ctx .... now load in cert and key");

        if (!File.Exists(fileCert) || !File.Exists(fileKey))
        {
            Console.WriteLine("Could not find cert or key file");
            return;
        }


        if (wolfssl.CTX_use_certificate_file(ctx, fileCert, wolfssl.SSL_FILETYPE_PEM) != wolfssl.SUCCESS)
        {
            Console.WriteLine("Error setting cert file");
            return;
        }


        if (wolfssl.CTX_use_PrivateKey_file(ctx, fileKey, 1) != wolfssl.SUCCESS)
        {
            Console.WriteLine("Error setting key file");
            return;
        }


        /* Test psk use with DHE */
        StringBuilder hint = new StringBuilder("cyassl server");
        wolfssl.CTX_use_psk_identity_hint(ctx, hint);
        wolfssl.CTX_set_psk_server_callback(ctx, psk_cb);

        short minDhKey = 128;
        wolfssl.CTX_SetMinDhKey_Sz(ctx, minDhKey);
        Console.Write("Setting cipher suite to ");
        StringBuilder set_cipher = new StringBuilder("DHE-PSK-AES128-CBC-SHA256");
        Console.WriteLine(set_cipher);
        if (wolfssl.CTX_set_cipher_list(ctx, set_cipher) != wolfssl.SUCCESS)
        {
            Console.WriteLine("Failed to set cipher suite");
            return;
        }

        IPAddress ip = IPAddress.Parse("0.0.0.0");
        UdpClient udp = new UdpClient(11111);
        IPEndPoint ep = new IPEndPoint(ip, 11111);
        Console.WriteLine("Started UDP and waiting for a connection");

        ssl = wolfssl.new_ssl(ctx);

        if (wolfssl.SetTmpDH_file(ssl, dhparam, wolfssl.SSL_FILETYPE_PEM) != wolfssl.SUCCESS)
        {
            Console.WriteLine("Error in setting dhparam");
            Console.WriteLine(wolfssl.get_error(ssl));
            return;
        }

        if (wolfssl.set_dtls_fd(ssl, udp, ep) != wolfssl.SUCCESS)
        {
            Console.WriteLine(wolfssl.get_error(ssl));
            return;
        }

        if (wolfssl.accept(ssl) != wolfssl.SUCCESS)
        {
           Console.WriteLine(wolfssl.get_error(ssl));
           return;
        }

        /* print out results of TLS/SSL accept */
        Console.WriteLine("SSL version is " + wolfssl.get_version(ssl));
        Console.WriteLine("SSL cipher suite is " + wolfssl.get_current_cipher(ssl));

        /* get connection information and print ip - port */
        wolfssl.DTLS_con con = wolfssl.get_dtls_fd(ssl);
        Console.Write("Connected to ip ");
        Console.Write(con.ep.Address.ToString());
        Console.Write(" on port ");
        Console.WriteLine(con.ep.Port.ToString());

        /* read information sent and send a reply */
        if (wolfssl.read(ssl, buff, 1023) < 0)
        {
            Console.WriteLine("Error reading message");
            Console.WriteLine(wolfssl.get_error(ssl));
            return;
        }
        Console.WriteLine(buff);

        if (wolfssl.write(ssl, reply, reply.Length) != reply.Length)
        {
            Console.WriteLine("Error writing message");
            Console.WriteLine(wolfssl.get_error(ssl));
            return;
        }

        Console.WriteLine("At the end freeing stuff");
        wolfssl.shutdown(ssl);
        wolfssl.free(ssl);
        udp.Close();

        wolfssl.CTX_free(ctx);
        wolfssl.Cleanup();
    }
}
