using MD.Lib.Log;
using MD.Lib.Util;
using MD.Configuration;
using MD.Model.Configuration.ElasticSearch;
using MD.Model.Index;
using Nest;
using Senparc.Weixin.MP.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Lib.ElasticSearch
{
    public static class LocationManager
    {
        static readonly object LockObject = new object();
        static void LogError(Exception ex)
        {
            MDLogger.LogErrorAsync(typeof(LocationManager), ex);
        }
        static ElasticClient _client = null;
        static LocationConfig _config = null;//MdConfigurationManager.GetConfig<LocationConfig>();
        static LocationManager()
        {
            if (_client != null && _config != null) return;
            lock (LockObject)
            {
                if (_client != null && _config != null) return;

                _client = ESHeper.GetClient<LocationConfig>();
                if (_client == null)
                {
                    var err = new Exception("_client没有正确初始化！");
                    LogError(err);
                    throw err;
                }

                _config = MdConfigurationManager.GetConfig<LocationConfig>();
                if (_config == null)
                {
                    var err = new Exception("配置没有正确初始化！");
                    LogError(err);
                    throw err;
                }

                init();
            }
        }

        static void init()
        {
            //if(!ESHeper.BeSureMapping<Location>(_client, _config.IndexName, new IndexSettings()
            //{
            //    NumberOfReplicas = Convert.ToInt32(_config.NumberOfReplica),
            //    NumberOfShards = Convert.ToInt32(_config.NumberOfShards),
            //}))
            //{
            //    var err = new Exception("Mapping没有正确初始化！");
            //    LogError(err);
            //    throw err;
            //}
            IGetMappingResponse mapping = _client.GetMapping<Location>();
            if (mapping != null && (mapping.Mappings == null || mapping.Mappings.Count != 0)) return ;
            _client.CreateIndex(_config.IndexName, s => s
                .AddMapping<Location>(f => f
                .MapFromAttributes()
                .Properties(p => p
                    .GeoPoint(g => g.Name(n => n.Coordinate).IndexLatLon())
                 )
               )
            );
        }

        public static async Task<bool> AddOrUpdateLocationAsync(string id, double lat, double lon)
        {
            var result = await _client.SearchAsync<Location>(s => s.Query(q => q.QueryString(ss => ss.Query("Id:" + id))));
            Location l = null;
            if (result.Total >= 1)
            {
                string _id = result.Hits.First().Id;
                var r = await _client.UpdateAsync<Location>((u) =>
                {
                    u.Id(_id);
                    l = new Location() { Id = id, Coordinate = new Coordinate() { Lat = lat, Lon = lon }, TimeStamp = DateTime.Now.ToString() };
                    u.Doc(l);
                    u.Index(_config.IndexName);
                    return u;
                });
                return r.IsValid;
            }
            else
            {
                l = new Location()
                {
                    Id = id,
                    Coordinate = new Coordinate()
                    {
                        Lon = lon,
                        Lat = lat,
                    },
                    TimeStamp = DateTime.Now.ToString()
                };
                List<Location> list = new List<Location>() { l };
                var resoponse = await _client.IndexAsync<Location>(l, (i) => { i.Index(_config.IndexName); return i; });
                return resoponse.Created;
            }
        }

        public static bool AddOrUpdateLocation(string id, double lat, double lon)
        {
            var result = _client.Search<Location>(s => s.Query(q => q.QueryString(ss => ss.Query("Id:" + id))));
            Location l = null;
            if (result.Total >= 1)
            {
                string _id = result.Hits.First().Id;
                var r = _client.Update<Location>((u) =>
                {
                    u.Id(_id);
                    l = new Location() { Id = id, Coordinate = new Coordinate() { Lat = lat, Lon = lon }, TimeStamp = DateTime.Now.ToString() };
                    u.Doc(l);
                    u.Index(_config.IndexName);
                    return u;
                });
                return r.IsValid;
            }
            else
            {
                l = new Location()
                {
                    Id = id,
                    Coordinate = new Coordinate()
                    {
                        Lon = lon,
                        Lat = lat,
                    },
                    TimeStamp = DateTime.Now.ToString()
                };
                List<Location> list = new List<Location>() { l };
                var resoponse = _client.Index<Location>(l, (i) => { i.Index(_config.IndexName); return i; });
                return resoponse.Created;
            }
        }

        public static async Task<Location> GetLocationObjectByOpenidAsync(string openid)
        {
            var result = await _client.SearchAsync<Location>(s => s.Query(q => q.QueryString(ss => ss.Query("Id:" + openid))));
            if (result.Total == 1)
                return result.Documents.First();
            return null;
        }

        public static Location GetLocationObjectByOpenid(string openid)
        {
            var result = _client.Search<Location>(s => s.Query(q => q.QueryString(ss => ss.Query("Id:" + openid))));
            if (result.Total == 1)
                return result.Documents.First();
            return null;
        }

        public static async Task<Double> GetDistanceBetweenAsync(string openid1, string openid2)
        {
            Location l1 = await GetLocationObjectByOpenidAsync(openid1);
            Location l2 = await GetLocationObjectByOpenidAsync(openid2);

            Coordinate co1 = l1.Coordinate; Coordinate co2 = l2.Coordinate;
            return GeoHelper.GetDistance(co1.Lat, co1.Lon, co2.Lat, co2.Lon);
        }
        public static double GetDistanceBetweenAsync(Coordinate c1, Coordinate c2)
        {
            return GeoHelper.GetDistance(c1.Lat, c1.Lon, c2.Lat, c2.Lon);
        }

        public static Double GetDistanceBetween(string openid1, string openid2)
        {
            Location l1 = GetLocationObjectByOpenid(openid1);
            Location l2 = GetLocationObjectByOpenid(openid2);

            Coordinate co1 = l1.Coordinate; Coordinate co2 = l2.Coordinate;
            return GeoHelper.GetDistance(co1.Lat, co1.Lon, co2.Lat, co2.Lon);
        }

        public static async Task<List<Location>> GetDistanceInKmByIdAsync(string openid,double km)
        {
            var result = await _client.SearchAsync<Location>(s => s.Query(q => q.QueryString(ss => ss.Query("Id:" + openid))));
            if(result.Total==1)
            {
                try
                {
                    var co = result.Documents.First().Coordinate;
                    var results = await _client.SearchAsync<Location>(s => s
       .Filter(f => f.GeoDistance("Coordinate", fd => fd.Distance(km, GeoUnit.Kilometers).Location(co.Lat, co.Lon)))
       .SortGeoDistance(sort => sort.OnField("Coordinate").PinTo(co.Lat, co.Lon).Ascending()));
                    return results.Documents.ToList();
                }
                catch (Exception ex)
                {
                    LogError(ex);
                    throw ex;
                }
            }
            return null;
        }
        public static List<Location> GetDistanceInKmById(string openid, double km)
        {
            var result = _client.Search<Location>(s => s.Query(q => q.QueryString(ss => ss.Query("Id:" + openid))));
            if (result.Total == 1)
            {
                try
                {
                    var co = result.Documents.First().Coordinate;
                    var results = _client.Search<Location>(s => s
       .Filter(f => f.GeoDistance("Coordinate", fd => fd.Distance(km, GeoUnit.Kilometers).Location(co.Lat, co.Lon)))
       .SortGeoDistance(sort => sort.OnField("Coordinate").PinTo(co.Lat, co.Lon).Ascending()));
                    return results.Documents.ToList();
                }
                catch (Exception ex)
                {
                    LogError(ex);
                    throw ex;
                }
            }
            return null;
        }

    }
}
