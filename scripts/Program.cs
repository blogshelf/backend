using System.Security.Cryptography;
using System.Text;

var ec = ECDsa.Create(ECCurve.NamedCurves.nistP256);

var privateKeyDer = ec.ExportPkcs8PrivateKey();
var publicKeyDer = ec.ExportSubjectPublicKeyInfo();

string ToPem(byte[] der, string label)
{
    var base64 = Convert.ToBase64String(der, Base64FormattingOptions.InsertLineBreaks);
    return $"-----BEGIN {label}-----\n{base64}\n-----END {label}-----";
}

var dir = Path.GetDirectoryName(typeof(Program).Assembly.Location)!;
var privatePem = ToPem(privateKeyDer, "PRIVATE KEY");
var publicPem = ToPem(publicKeyDer, "PUBLIC KEY");

File.WriteAllText(Path.Combine(dir, "ec_private.pem"), privatePem, new UTF8Encoding(false));
File.WriteAllText(Path.Combine(dir, "ec_public.pem"), publicPem, new UTF8Encoding(false));

Console.WriteLine("Generated ec_private.pem & ec_public.pem (P-256)");
