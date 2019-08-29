using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SpiderNs
{
    public class SpiderCompleteEventArgs
    {
        public Matrix<double> Matrix { get; }
        public List<Page> PageTable { get; }

        public SpiderCompleteEventArgs(Matrix<double> matrix, IEnumerable<Page> pages)
        {
            Matrix = matrix;
            PageTable = pages.ToList();
        }
    }
}
