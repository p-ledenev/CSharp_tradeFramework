#region

using System;
using System.Linq;
using core.MathLab;
using core.Model;

#endregion

namespace core.ApproximationConstructors
{
    internal class LinearApproximationConstructor : ApproximationConstructor
    {
        public override Approximation approximate(IValue[] values, int start, int depth)
        {
            Approximation ap = new Approximation();
            double[] k;
            double r2;

            OLSLinearApproximation(values, start, depth, out k, out r2);

            ap.k = k;
            ap.depth = depth;
            ap.r2 = r2;

            return ap;
        }


        public void OLSLinearApproximation(IValue[] values, int start, int depth, out double[] k, out double r2)
        {
            double[,] fmatrix = new double[depth, 2];
            double[] y = new double[depth];
            int info;
            double[] c;
            alglib.lsfitreport rep;

            for (int i = 0; i < depth; i++)
            {
                fmatrix[i, 0] = i;
                fmatrix[i, 1] = (start - depth + 1) > 0 ? values[start - depth + 1].getValue() : 0;

                y[i] = (start - depth + 1 + i) > 0 ? values[start - depth + 1 + i].getValue() : 0;
            }

            lsFitLinear(y, fmatrix, out info, out c, out rep);

            double rsstotal = 0;
            for (int i = 0; i < y.Length; i++) rsstotal += Math.Pow(y[i] - y.Average(), 2);

            k = c;
            k[1] = Math.Round(c[1]*y[0]);

            r2 = 1 - Math.Pow(rep.rmserror, 2)*y.Length/rsstotal;
        }

        public virtual void lsFitLinear(double[] y, double[,] fmatrix, out int info, out double[] c,
            out alglib.lsfitreport rep)
        {
            alglib.lsfitlinear(y, fmatrix, out info, out c, out rep);
        }
    }
}