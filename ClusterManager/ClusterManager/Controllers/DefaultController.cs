using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using ClusterManager.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
        

        [HttpPost]
        public string CreateGenesis() {
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


            return jobj.ToString();
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