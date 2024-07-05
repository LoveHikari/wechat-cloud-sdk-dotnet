using Hikari.WeChatCloud.Sdk;

namespace TestProject1
{
    public class UnitTest1
    {
        [Fact]
        public async void Test1()
        {
            var db = new DataBaseClient("wx5db128dc64b77642", "0f5eab86e03b99810ee3f550b6130ed2", "yunying-yogl8");
            await db.GetAccessTokenAsync();
            var v1 = await db.QueryListAsync<Building>("building", new QueryParameter()
            {
                //Limit = 10,
                Where = new Dictionary<string, string>()
                {
                    {"park" , "7fbac6cf5f2b77230001227c278d93da"}
                }
            });


            var dd = new Hikari.UniCloud.Sdk.DataBaseClient("mp-6b46bbf4-5e74-44a6-97f0-b3a7ea457d8f", "QqHMPRDqxY60TEap2kwVhA==");
            await dd.GetAccessTokenAsync();
            var v2 = await dd.QueryListAsync<Building1>("building", new Hikari.UniCloud.Sdk.QueryParameter()
            {
                Where = "'parkId'=='63e9e80528064aa7a8330cfd' && 'isDel'!=true"
            });
            foreach (var b2 in v2)
            {
                var v = v1.FirstOrDefault(x => x.building == b2.building && x.No == b2.No);
                b2.SalePrice = v?.SalePrice;
                if (v != null && v.SalePrice != null)
                {
                    await dd.UpdateAsync("building", $"'_id' == '{b2.Id}'", new Dictionary<string, object>()
                    {
                        { "salePrice", v.SalePrice }
                    });
                }

            }
            Assert.True(true);
        }
    }
}