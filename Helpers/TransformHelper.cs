using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using MathNet.Numerics.LinearAlgebra;
using SixLabors.Primitives;

namespace TerminusDotNetCore.Helpers
{
    public class TransformHelper
    {
        public static Matrix4x4 ComputeTransformMatrix(int width, int height, Point newTopLeft, Point newTopRight, Point newBottomLeft, Point newBottomRight)
        {
            //FIX - generalize the calculation of A & B matrices into function - code too W E T rn
            //compute A matrix
            Matrix<double> solveA = Matrix<double>.Build.DenseOfArray(new double[,] {
                { 0, width, width },
                { 0, 0, height },
                { 1, 1, 1 }
            });

            MathNet.Numerics.LinearAlgebra.Vector<double> augmentA = MathNet.Numerics.LinearAlgebra.Vector<double>.Build.Dense(new double[]
                { 0, height, 1}
            );

            MathNet.Numerics.LinearAlgebra.Vector<double> coefficientsA = solveA.Solve(augmentA);

            Matrix<double> A = Matrix<double>.Build.DenseOfArray(new double[,] {
                { 0, coefficientsA[1] * width, coefficientsA[2] * width },
                { 0, 0, coefficientsA[2] * height },
                { coefficientsA[0], coefficientsA[1], coefficientsA[2] }
            });

            //compute B matrix
            Matrix<double> solveB = Matrix<double>.Build.DenseOfArray(new double[,] {
                { newTopLeft.X, newTopRight.X, newBottomRight.X },
                { newTopLeft.Y, newTopRight.Y, newBottomRight.Y },
                { 1, 1, 1 }
            });

            MathNet.Numerics.LinearAlgebra.Vector<double> augmentB = MathNet.Numerics.LinearAlgebra.Vector<double>.Build.Dense(new double[]
                { newBottomLeft.X, newBottomLeft.Y, 1}
            );

            MathNet.Numerics.LinearAlgebra.Vector<double> coefficientsB = solveB.Solve(augmentB);

            Matrix<double> B = Matrix<double>.Build.DenseOfArray(new double[,] {
                { coefficientsB[0] * newTopLeft.X, coefficientsB[1] * newTopRight.X, coefficientsB[2] * newBottomRight.X },
                { coefficientsB[0] * newTopLeft.Y, coefficientsB[1] * newTopRight.Y, coefficientsB[2] * newBottomRight.Y },
                { coefficientsB[0], coefficientsB[1], coefficientsB[2] }
            });

            //compute matrix C = B * A^-1 
            Matrix<double> AInv = A.Inverse();
            Matrix<double> C = B * AInv;

            //return the homogeneous transform matrix based on C 
            return new Matrix4x4((float)C[0, 0], (float)C[1, 0], 0, (float)C[2, 0],
                (float)C[0, 1], (float)C[1, 1], 0, (float)C[2, 1],
                0, 0, 1, 0,
                (float)C[0, 2], (float)C[1, 2], 0, (float)C[2, 2]);
        }
    }
}
