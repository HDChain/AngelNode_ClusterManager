using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using ClusterManager.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Nethereum.KeyStore;
using Nethereum.Signer;
using Newtonsoft.Json.Linq;

namespace ClusterManager.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class DefaultController : ControllerBase
    {
        public static string JsonConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"genesis.json");
        private static Random Random = new Random();
        
        [HttpGet]
        [HttpPost]
        public IActionResult CreateGenesis() {
            var json = System.IO.File.ReadAllText(JsonConfigPath);
            dynamic jobj = JObject.Parse(json);

            jobj.config.chainId = Random.Next(100000000, 1000000000);
            jobj.timestamp = $"0x{ GetUnixTime():X}";

            jobj.config.clique.period = 2;
            

            var accounts = new[] {
                CreateAccount(),
                CreateAccount(),
                CreateAccount(),
            };

            jobj.extraData =
                $"0x0000000000000000000000000000000000000000000000000000000000000000{string.Join("",accounts.Select(n=>n.Address.Substring(2)))}0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000";

            
            jobj.alloc = new JObject(
                accounts.Select(n=> 
                        new JProperty(n.Address,new JObject(
                            new JProperty("balance","0x200000000000000000000000000000000000000000000000000000000000000")
                            ))
                        )
                );

            using (var ms = new MemoryStream()) {
                var zip = new ZipArchive(ms,ZipArchiveMode.Create);

                foreach (var jsonAccount in accounts) {
                    var entry = zip.CreateEntry($"accounts\\{jsonAccount.Address}.json", CompressionLevel.Optimal);
                    using (var sw = new StreamWriter(entry.Open())) {
                        sw.Write(jsonAccount.Json);
                    }

                    var pass = zip.CreateEntry($"accounts\\{jsonAccount.Address}_pass.txt");
                    using (var sw = new StreamWriter(pass.Open())) {
                        sw.Write(jsonAccount.Password);
                    }
                }

                return new FileContentResult(ms.ToArray(),"application/zip") {
                    FileDownloadName = "Genesis.zip"
                };
            }
        }
        
        private static long GetUnixTime()
        {
            return (long) (DateTime.Now - (new DateTime(1970, 1, 1).ToLocalTime())).TotalSeconds;

        }

        private JsonAccount CreateAccount() {

            var key = EthECKey.GenerateKey();
            var service = new KeyStoreService();

            var pass = Guid.NewGuid().ToString("N").Substring(0,6);
            var publicAddress = key.GetPublicAddress().ToLower();
            var json = service.EncryptAndGenerateDefaultKeyStoreAsJson(pass, key.GetPrivateKeyAsBytes(), publicAddress);
            
            return new JsonAccount() {
                Address = publicAddress,
                Json = json,
                Password = pass
            };
        }

        
    }
}