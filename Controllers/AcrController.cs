using TilosAzureMvc.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Table;
using System.Net;

namespace TilosAzureMvc.Controllers {
    public class AcrController : Controller {
        private const string TILOSHU_STREAMID = "s-M1xLrD2";
        private static AcrCallback _lastCallback;

        private static CloudStorageAccount getStorageAccount() {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                CloudConfigurationManager.GetSetting("sosandris_AzureStorageConnectionString"));
            return storageAccount;
        }
        private static CloudTable getTable() {
            CloudStorageAccount storageAccount = getStorageAccount();
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("AcrData");
            return table;
        }
        private string getPartitionFilter(string streamId = "s-M1xLrD2") {
            return TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, streamId);
        }

        /// <summary>
        /// Visszaadja a RowKey-t az idő string alapján. 
        /// A kódolás: év, hó, nap, stb-re minden összetevőt kivon 9999-ből. 
        /// Így fordított sorrendben tárolja az Azure, és szemre vissza lehet fejteni, hogy melyik RowKey milyen időpontot rejt.
        /// </summary>
        /// <param name="sTime">Idő string formátum yyyyMMddHHmmss</param>
        /// <returns></returns>
        public static string getRowKeyFromTimeString(string sTime) {
            var y = int.Parse(sTime.Substring(0, 4));
            var mo = int.Parse(sTime.Substring(4, 2));
            var d = int.Parse(sTime.Substring(6, 2));
            var h = int.Parse(sTime.Substring(8, 2));
            var mi = int.Parse(sTime.Substring(10, 2));
            var s = int.Parse(sTime.Substring(12, 2));
            var dTime = new DateTime(y, mo, d, h, mi, s);
            var retVal = new StringBuilder();
            retVal.Append((9999 - y).ToString());
            retVal.Append((9999 - mo).ToString());
            retVal.Append((9999 - d).ToString());
            retVal.Append((9999 - h).ToString());
            retVal.Append((9999 - mi).ToString());
            retVal.Append((9999 - s).ToString());
            return retVal.ToString();
        }

        /// <summary>
        /// Készít egy AcrData táblát, ha még nincs ilyen.
        /// </summary>
        /// <returns>Létre lett-e hozva új tábla</returns>
        public ActionResult CreateTable() {
            CloudTable table = getTable();
            ViewBag.Success = table.CreateIfNotExists();
            ViewBag.TableName = table.Name;
            return View();
        }

        /// <summary>
        /// Törli a debug adatokat (PartitionKey="1"
        /// </summary>
        /// <returns>Hány sort törölt</returns>
        [HttpPost]
        public ActionResult TruncateTable() {
            var table = getTable();
            var batchOperation = new TableBatchOperation();
            var projectionQuery = new TableQuery<AcrCallback>()
                .Where(getPartitionFilter("1"))
                .Select(new string[] { "RowKey" });

            var i = 0; var count = 0;
            foreach (var entity in table.ExecuteQuery(projectionQuery)) {
                batchOperation.Delete(entity);
                i++; count++;
                if (i >= 100) { // A batch operation max 100 műveletet enged egyszerre
                    i = 0;
                    table.ExecuteBatch(batchOperation);
                }
            }
            if (i > 0) table.ExecuteBatch(batchOperation);
            return Content($"{count} sor törölve.");
        }

        /// <summary>
        /// Az utolsó x bejegyzést adja vissza
        /// </summary>
        /// <param name="streamId">Az AcrCloud Stream ID</param>
        /// <param name="limit">Hány darab bejegyzést adjon vissza</param>
        /// <param name="offset">Honnan kezdje? 0=legutolsó</param>
        /// <returns>Az utolsó x bejegyzés tömbje</returns>
        [AllowCrossSite]
        public ActionResult Last(string streamId = TILOSHU_STREAMID, int limit = 1, int offset = 0) {
            if (limit < 2 && streamId == TILOSHU_STREAMID) {
                if (_lastCallback == null) return Content("empty");
                return Content(_lastCallback.Data);
            } else {

                return Json(getLast(streamId, limit, offset), JsonRequestBehavior.AllowGet);
            }
        }

        [AllowCrossSite(origin ="http://localhost:3000")]
        public ActionResult LastDev(string streamId = TILOSHU_STREAMID, int limit = 1, int offset = 0) {
            if (limit ==1 && offset == 0) return Json(getLast(streamId, limit, offset)[0].Data, JsonRequestBehavior.AllowGet);
            return Json(getLast(streamId, limit, offset), JsonRequestBehavior.AllowGet);
        }

        private List<AcrCallback> getLast(string streamId = TILOSHU_STREAMID, int limit = 1, int offset = 0) {
            CloudTable table = getTable();
            TableQuery<AcrCallback> query = new TableQuery<AcrCallback>().Where(getPartitionFilter(streamId));

            List<AcrCallback> playlist = new List<AcrCallback>();
            TableContinuationToken token = null;
            do {
                TableQuerySegment<AcrCallback> resultSegment = table.ExecuteQuerySegmented(query, token);
                token = resultSegment.ContinuationToken;

                foreach (AcrCallback customer in resultSegment.Results) {
                    playlist.Add(customer);
                }
            } while (token != null && playlist.Count < (limit + offset));

            var result = playlist.Skip(offset).Take(limit);
            return result.ToList();

        }
        // GET: Acr
        /// <summary>
        /// Szép táblázatot rajzol az ÖSSZES lementett adatból
        /// </summary>
        /// <returns></returns>
        public ActionResult Index() {
            var table = getTable();
            TableQuery<AcrCallback> query = new TableQuery<AcrCallback>().Where(getPartitionFilter());

            List<AcrCallback> playlist = new List<AcrCallback>();
            TableContinuationToken token = null;
            do {
                TableQuerySegment<AcrCallback> resultSegment = table.ExecuteQuerySegmented(query, token);
                token = resultSegment.ContinuationToken;

                foreach (AcrCallback customer in resultSegment.Results) {
                    playlist.Add(customer);
                }
            } while (token != null);

            return View(getLast(TILOSHU_STREAMID,1000,0));
        }


        /// <summary>
        /// Két dátum közötti bejegyzéseket listázza. Dátum formátum: yyyyMMddHHmmss
        /// </summary>
        /// <param name="From">Kezdő dátum</param>
        /// <param name="To">Vége dátum</param>
        /// <returns></returns>
        public ActionResult List(string From, string To, string streamId = TILOSHU_STREAMID) {
            var table = getTable();
            var queryPartition = getPartitionFilter(streamId);
            var queryFrom = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.LessThanOrEqual, getRowKeyFromTimeString(From));
            var queryTo = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.GreaterThanOrEqual, getRowKeyFromTimeString(To));

            TableQuery<AcrCallback> query = new TableQuery<AcrCallback>().
                Where(TableQuery.CombineFilters(
                    TableQuery.CombineFilters(queryPartition, TableOperators.And, queryFrom), TableOperators.And, queryTo));

            return Json(getList(From, To, streamId),JsonRequestBehavior.AllowGet);
        }

        private List<AcrCallback> getList(string From, string To, string streamId = TILOSHU_STREAMID) {
            var table = getTable();
            var queryPartition = getPartitionFilter(streamId);
            var queryFrom = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.LessThanOrEqual, getRowKeyFromTimeString(From));
            var queryTo = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.GreaterThanOrEqual, getRowKeyFromTimeString(To));

            TableQuery<AcrCallback> query = new TableQuery<AcrCallback>().
                Where(TableQuery.CombineFilters(
                    TableQuery.CombineFilters(queryPartition, TableOperators.And, queryFrom), TableOperators.And, queryTo));

            return (table.ExecuteQuery(query).ToList());
        }

        public ActionResult Create() {
            AcrCallback item;
            CloudTable table = getTable();
            try {


                item = new AcrCallback() {
                    StreamId = "streamid1",
                    StreamUrl = "streal_url1",
                    Data = "data1",
                };
                AcrCallback acrData = new AcrCallback(item.StreamId, DateTime.Now.ToString("yyyyMMddHHmmss")) {
                    Data = item.Data,
                    StreamUrl = item.StreamUrl,
                };
                TableOperation insertOperation = TableOperation.Insert(acrData);
                TableResult result = table.Execute(insertOperation);
                ViewBag.TableName = table.Name;
                ViewBag.Result = result.HttpStatusCode;
                ViewBag.ResultDetails = result.HttpStatusCode.ToString();
                return View();
            } catch (Exception ex) {
                ViewBag.Result = (int)HttpStatusCode.InternalServerError; ;
                ViewBag.ResultDetails = ex.ToString();
                return View();
            }

        }
        // POST: Acr/Create
        [HttpPost]
        public ActionResult Create(FormCollection collection) {
            StringBuilder msg = new StringBuilder();
            CloudTable table = getTable();
            try {

                AcrCallback acrData = new AcrCallback(
                    collection["stream_id"],
                    getRowKeyFromTimeString(DateTime.Now.ToString("yyyyMMddHHmmss"))) {
                    StreamId = collection["stream_id"],
                    StreamUrl = collection["stream_url"],
                    Data = collection["data"],
                    Timestamp = DateTime.Now,
                };
                _lastCallback = acrData;
                TableOperation insertOperation = TableOperation.Insert(acrData);
                TableResult result = table.Execute(insertOperation);

                Response.StatusCode = result.HttpStatusCode;
                return Content(msg.ToString());
            } catch (Exception ex) {
                return Content(ex.ToString());
            }
        }

    }
}