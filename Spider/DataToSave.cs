using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace SpiderNs
{
    [Serializable]
    class DataToSave
    {
        const string saveDir = @"data\spiderData.sds";

        public List<Spider.Page> pagesList;
        public int lastGettedPage;

        public string homePage;
        public int analyzingPage;
        public Matrix<double> matrix;

        public static bool DataExist
        {
            get
            {
                return File.Exists(saveDir);
            }

        }

        public void Save()
        {
            if (!Directory.Exists("data"))
                Directory.CreateDirectory("data");
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            FileStream fileStream = new FileStream(saveDir, FileMode.Create);
            binaryFormatter.Serialize(fileStream, this);
            fileStream.Close();
        }

        public void Load()
        {
            if (!File.Exists(saveDir))
                throw new Exception();
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            FileStream fileStream = new FileStream(saveDir, FileMode.Open);
            var data = binaryFormatter.Deserialize(fileStream) as DataToSave;
            fileStream.Close();

            pagesList = data.pagesList;
            lastGettedPage = data.lastGettedPage;
            homePage = data.homePage;
            analyzingPage = data.analyzingPage;
            matrix = data.matrix;
        }
    }
}
