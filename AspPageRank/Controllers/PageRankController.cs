using SpiderNs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace AspPageRank.Controllers
{
    public class PageRankController : Controller
    {
        static List<Spider> SpiderList = new List<Spider>();
        //static Spider spider;

        // GET: PageRank
        public ActionResult Index(int id = 0)
        {
            return View();
        }

        [HttpPost]
        public ActionResult Index(string page)
        {
            return RedirectToAction("Result", new { page = page });
        }

        public ActionResult Result(string page)
        {
            SpiderList.Add(new Spider(page));
            Task.Run(() => SpiderList.Last().Start());
            Models.ResultData resultData = new Models.ResultData()
            {
                spidingSite = page,
                spiderId = SpiderList.Count - 1
            };
            return View(resultData);
        }

        public string GetMatrix(int id)
        {
            var mx = SpiderList[id].GetMatrix();
            var linkList = GetGraphLinks(mx, id).ToList();
            var tb = SpiderList[id].GetPageTable();

            var loopLength = tb.Count < mx.Length ? tb.Count : mx.Length;

            GraphNode[] nodeList = new GraphNode[tb.Count];
            for (int i = 0; i < loopLength; i++)
            {
                nodeList[i] = new GraphNode() { id = tb[i].Uri, group = 1 };
            }
            Graph graph = new Graph() { links = linkList.ToArray(), nodes = nodeList };
            var jr = Newtonsoft.Json.JsonConvert.SerializeObject(graph);
            return jr;
        }

        public IEnumerable<GraphLink> GetGraphLinks(double[][] matrix, int id)
        {
            var tb = SpiderList[id].GetPageTable();

            var loopLength = tb.Count < matrix.Length ? tb.Count : matrix.Length;

            for (int i = 0; i < loopLength; i++)
            {
                for (int j = 0; j < loopLength; j++)
                {
                    if (matrix[i][j] == 1)
                    {
                        yield return new GraphLink() { source = tb[i].Uri, target = tb[j].Uri, value = 1 };
                    }
                }
            }
        }

        public void StopSpider(int id)
        {
            SpiderList[id].Stop();
        }

    }
    public struct GraphLink
    {
        public string source;
        public string target;
        public int value;
    }

    public struct GraphNode
    {
        public string id;
        public int group;

    }

    public struct Graph
    {
        public GraphNode[] nodes;
        public GraphLink[] links;
    }
}