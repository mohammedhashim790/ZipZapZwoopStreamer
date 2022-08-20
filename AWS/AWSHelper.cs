using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime.CredentialManagement;
using System;
using System.Collections.Generic;
using static Amazon.Internal.RegionEndpointProviderV2;

namespace Streamer.AWS
{
    public class AWSHelper
    {

        AmazonDynamoDBClient dbClient;

        public String TableName;

        //"Session-uwm6jl64dfcbrgnkyratx7wdim-dev"
        public AWSHelper(String TableName)
        {
            this.TableName = TableName;
            this.dbClient = new AmazonDynamoDBClient(Amazon.RegionEndpoint.APSouth1);
            this.WriteProfile(
                "Visual-Studio-22",
                "AKIA3RGPO7UIZXPIZZBP",
                "3E1bwfuV0Yrm6MSwuCsormKo6Vn/3Ysq+RxC5EuI"
                );
        }

        void WriteProfile(string profileName, string keyId, string secret)
        {
            System.Diagnostics.Debug.WriteLine($"Create the [{profileName}] profile...");
            var options = new CredentialProfileOptions
            {
                AccessKey = keyId,
                SecretKey = secret
            };
            var profile = new CredentialProfile(profileName, options);
            var sharedFile = new SharedCredentialsFile();
            sharedFile.RegisterProfile(profile);
        }


        public Document getSession(string sessionId)
        {

            var table = Table.LoadTable(dbClient, TableName);
            var item = table.GetItemAsync(sessionId).Result;

            return item;
        }


        public Document ListSessions()
        {

            var table = Table.LoadTable(dbClient, TableName);
            var item = table.GetItemAsync("dfd04617-1671-4677-9523-a777c8d64e9d").Result;

            //var request = new GetItemRequest
            //{
            //    TableName = TableName,
            //    Key = new Dictionary<string, AttributeValue>
            //    {
            //        {
            //            "id",
            //            new AttributeValue {
            //                S = "dfd04617-1671-4677-9523-a777c8d64e9d"
            //            }
            //        }
            //    },
            //};

            //this.dbClient.GetItemAsync(request).Result;

            return item;
        }


    }
}
