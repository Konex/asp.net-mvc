using System.Diagnostics;
using System.Security.Cryptography;

namespace RsaKeyGen
{
    class Program
    {
        static void Main(string[] args)
        {
            var rsa = new RSACryptoServiceProvider();
            var privateParameters = rsa.ExportParameters(true);
            var publicParameters = rsa.ExportParameters(false);
   
            //Export private parameter XML representation of privateParameters
            //object created above
            Debug.WriteLine(rsa.ToXmlString(true));
 
            //Export private parameter XML representation of publicParameters
            //object created above
            Debug.WriteLine(rsa.ToXmlString(false));
        }
    }
}
