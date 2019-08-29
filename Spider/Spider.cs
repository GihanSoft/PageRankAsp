using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SpiderNs
{
    public class Spider : IDisposable
    {
        [Serializable]
        public struct Page
        {
            public int id;
            public string url;
            public string content;
            public HttpStatusCode statusCode;

            public Page(string url, int id) : this(url, null, id)
            { }
            public Page(string url, string content, int id)
            {
                this.url = url;
                this.content = content;
                this.statusCode = HttpStatusCode.NoContent;
                this.id = id;
            }
        }

        bool paused = false;
        bool analizePause = false;
        bool netPause = false;

        public bool Pause
        {
            set
            {
                paused = value;
                if (!value)
                {
                    analizePause = netPause = value;
                }
            }
            get
            {
                if (analizePause && netPause)
                    return true;
                return false;
            }
        }

        public event EventHandler<SpiderCompleteEventArgs> Complete;

        public String HomePage { get; }
        public List<Page> PageList { get; } = new List<Page>();

        public int AnalyzingPage { get; private set; }
        public bool IsDone { get; private set; }

        HttpClient httpClient = new HttpClient();

        Matrix<double> matrix;

        public Spider()
        {
            var data = new DataToSave();
            try
            {
                data.Load();
            }
            catch
            {
                throw;
            }
            this.AnalyzingPage = data.analyzingPage;
            this.HomePage = data.homePage;
            this.lastGettedPage = data.lastGettedPage;
            this.matrix = data.matrix;
            this.PageList = data.pagesList;

            IsDone = false;
        }

        public Spider(string homePage)
        {
            IsDone = false;
            if (homePage.EndsWith("/"))
                homePage = homePage.Substring(0, homePage.Length - 1);
            HomePage = homePage;
            PageList.Add(new Page(HomePage, PageList.Count));

            matrix = Matrix<double>.Build.Sparse(1, 1, 0);

            AnalyzingPage = 0;
        }

        public void Start()
        {
            Task getPagesThread;
            getPagesThread = Task.Run(() => GetPages());
            Thread.Sleep(1);
            for (; AnalyzingPage < PageList.Count; AnalyzingPage++)
            {
                if (paused) analizePause = true;
                while (paused)
                    Thread.Sleep(1);

                while (PageList[AnalyzingPage].content == null)
                    Thread.Sleep(5);
                matrix[AnalyzingPage, AnalyzingPage] = 1;

                if (PageList[AnalyzingPage].statusCode != HttpStatusCode.OK)
                    continue;

                var pageLinks = GetAllLinks(PageList[AnalyzingPage].content).ToArray();

                for (int i = 0; i < pageLinks.Length; i++)
                {
                    var item = pageLinks[i];
                    if (!PageList.AsParallel().Any(x => x.url == item))
                    {
                        PageList.Add(new Page(item, PageList.Count));
                        var vector = Vector<double>.Build.Dense(matrix.RowCount, 0);
                        matrix = matrix.InsertRow(matrix.RowCount, vector);
                        vector = Vector<double>.Build.Dense(matrix.RowCount, 0);
                        matrix = matrix.InsertColumn(matrix.ColumnCount, vector);
                    }
                    var index = PageList.FindIndex(x => x.url == item);
                    matrix[index, AnalyzingPage] = 1;
                }
                var analizedPage = PageList[AnalyzingPage];
                analizedPage.content = null;
                PageList[AnalyzingPage] = analizedPage;
            }

            IsDone = true;
            OnComplete();
        }

        public string GetTag(string str, string tag, int startIndex, out int tagIndex)
        {
            var start = str.IndexOf($"<{tag}", startIndex);

            if (start == -1)
            {
                tagIndex = -1;
                return null;
            }

            var end = str.IndexOf(">", start);
            string sub = null;
            try
            {
                sub = str.Substring(start, end - start + 1);
            }
            catch
            {
                tagIndex = -1;
                return null;
            }

            tagIndex = start;
            return sub;
        }
        public IEnumerable<string> GetAllLinks(string str)
        {
            var tag = "a";
            for (int i = 0; i < str.Length;)
            {
                var fullTag = GetTag(str, tag, i, out int tagIndex);
                if (tagIndex == -1 || fullTag == null) break;
                i = tagIndex;
                i += fullTag.Length;
                var link = GetLink(fullTag);

                if (!IsLinkValid(ref link)) continue;

                yield return link;
            }
        }

        private bool IsLinkValid(ref string link)
        {
            if (string.IsNullOrWhiteSpace(link)) return false;
            if (link.StartsWith("javascript:")) return false;
            if (!link.StartsWith("http"))
            {
                if (link.StartsWith("/"))
                    link = HomePage + link;
                else
                    link = HomePage + '/' + link;
            }
            if (link.Contains("#"))
            {
                link = link.Substring(0, link.IndexOf("#"));
            }
            if (link.EndsWith("/"))
                link = link.Substring(0, link.Length - 1);

            string tempHome = HomePage;
            if (HomePage.StartsWith("http://")) tempHome = HomePage.Substring(7);
            if (HomePage.StartsWith("https://")) tempHome = HomePage.Substring(8);
            if (tempHome.StartsWith("www.")) tempHome = tempHome.Substring(4);

            if (!link.Contains(tempHome)) return false;
            return true;
        }

        /// <summary>
        /// get href value of An a HTML link tag
        /// </summary>
        /// <param name="fullTag"> Full link tag with attributes </param>
        /// <returns>Link Address of tag. null if there isn't any href is empty</returns>
        public string GetLink(string fullTag)
        {
            //make sure fullTag is an <a> html tag
            if (!fullTag.StartsWith("<a"))
                throw new ArgumentException("GetLink function just works for <a> tag.", "fullTag");

            var hrefIndex = fullTag.IndexOf("href"); //get href attribute place
            if (hrefIndex == -1) //if there is no href
                return null;
            int endOfLink = 0; //end of href attribute value

            switch (fullTag[hrefIndex + 5]) //switch first char after "herf="
            {
                case '\"': //link between ""
                    endOfLink = fullTag.IndexOf('\"', hrefIndex + 6);
                    break;
                case '\'': //link between ''
                    endOfLink = fullTag.IndexOf('\'', hrefIndex + 6);
                    break;
                default: //link between nothing
                    endOfLink = fullTag.IndexOf(' ', hrefIndex + 6);
                    break;
            }
            try
            {
                return fullTag.Substring(hrefIndex + 6, endOfLink - hrefIndex - 6);
            }
            catch
            {
                return null;
            }
        }


        int lastGettedPage = 0;
        private void GetPages()
        {
            while (!IsDone)
            {
                Thread.Sleep(1);
                
                for (; lastGettedPage < PageList.Count; lastGettedPage++)
                {
                    if (paused) netPause = true;
                    while (paused)
                        Thread.Sleep(1);

                    Page page = PageList[lastGettedPage];
                    HttpResponseMessage result = null;
                    try
                    {
                        result = httpClient.GetAsync(page.url).Result;

                        page.statusCode = result.StatusCode;
                        page.content = result.Content.ReadAsStringAsync().Result;
                    }
                    catch (Exception e)
                    {
                        page.statusCode = HttpStatusCode.NotFound;
                        page.content = e.ToString();
                    }
                    PageList[lastGettedPage] = page;
                }
            }
        }

        public void Stop()
        {
            Pause = true;
            while (!Pause) Thread.Sleep(1);
            var temp = PageList.Where(a => PageList.IndexOf(a) < AnalyzingPage).ToList();
            PageList.Clear();
            PageList.AddRange(temp);
            matrix = matrix.SubMatrix(0, AnalyzingPage, 0, AnalyzingPage);

            OnComplete();
            Dispose();
        }

        public void Dispose()
        {
            /*
            Pause = true;
            while (!Pause && !IsDone) Thread.Sleep(1);
            var data = new DataToSave
            {
                analyzingPage = AnalyzingPage,
                homePage = HomePage,
                lastGettedPage = lastGettedPage,
                matrix = matrix,
                pagesList = PageList
            };
            data.Save();
            */
        }

        public void OnComplete()
        {
            var pureMatrix = matrix.Clone();

            var pages = PageList.Select(page => new SpiderNs.Page()
            {
                ID = page.id,
                Uri = page.url,
                StatusCode = page.statusCode
            }).ToList();

            var purePages = pages.Where(a => a.StatusCode == System.Net.HttpStatusCode.OK).ToList();

            for (int i = pureMatrix.ColumnCount - 1; i >= 0; i--)
            {
                if (pages[i].StatusCode != System.Net.HttpStatusCode.OK)
                {
                    pureMatrix = pureMatrix.RemoveColumn(i);
                    pureMatrix = pureMatrix.RemoveRow(i);
                }
            }

            pureMatrix = pureMatrix.InsertColumn(pureMatrix.ColumnCount, Vector<double>.Build.Dense(pureMatrix.RowCount, 1));
            pureMatrix = pureMatrix.InsertRow(pureMatrix.RowCount, Vector<double>.Build.Dense(pureMatrix.ColumnCount, 1));

            var colsSum = pureMatrix.ColumnSums();
            var outLoopResult = Parallel.For(0, pureMatrix.ColumnCount, i =>
            {
                var colSum = colsSum[i];
                var inLoopResult = Parallel.For(0, pureMatrix.RowCount, j =>
                {
                    pureMatrix[j, i] /= colSum;
                });
                while (!inLoopResult.IsCompleted) Thread.Sleep(1);
            });
            while (!outLoopResult.IsCompleted) Thread.Sleep(1);

            var m = (Matrix<double>.Build.DiagonalIdentity(pureMatrix.RowCount, pureMatrix.ColumnCount) - pureMatrix);
            var scoresKernel = m.Kernel();
            var scoresVector = scoresKernel[0];
            scoresVector = scoresVector.SubVector(0, scoresVector.Count - 1);

            for (int i = 0; i < scoresVector.Count; i++)
            {
                var pageIndex = pages.IndexOf(pages.Find(page => purePages[i].ID == page.ID));
                pages[pageIndex].Score = Math.Abs(scoresVector[i]);
            }

            SpiderCompleteEventArgs eventArgs = new SpiderCompleteEventArgs(matrix, pages);
            Complete?.Invoke(this, eventArgs);
        }

        public List<SpiderNs.Page> GetPageTable()
        {
            var pages = PageList.Select(page => new SpiderNs.Page()
            {
                ID = page.id,
                Uri = page.url,
                StatusCode = page.statusCode
            }).ToList();
            return pages;
        }

        public double[][] GetMatrix()
        {
            return matrix.ToColumnArrays();
        }
    }
}
