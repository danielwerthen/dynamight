using System;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.IO;


using OctreeSearch;

namespace Demo
{
    public partial class Form1 : Form
    {
        Octree octreeDataset;

        public Form1()
        {
            InitializeComponent();

            BuildDemoDatabase();

        }

        private void button1_Click(object sender, EventArgs e)
        {


            label2.Text = FindNode(
                Double.Parse(textBox1.Text),
                Double.Parse(textBox2.Text),
                Double.Parse(textBox3.Text)).ToString();
        }
        /// <summary>
        /// 
        /// </summary>
        private object FindNode(double x, double y, double z)
        {
            return (object)octreeDataset.GetNode(x, y, z);
        }

        private void BuildDemoDatabase()
        {
            int total = 1000;
            double[][] wtest = new double[total][];
            double[] xyzMax = new double[3];
            double[] xyzMin = new double[3];

            Random rnd = new Random();

            /*
             * Builds a data set of random number. It also evaluates the maximum and 
             * minimum of the data points is not required but will speed up the search process.
             * */
            using (StreamWriter sw = new StreamWriter("demo.txt", false))
            {
                for (int j = 0; j < 3; j++)
                {
                    xyzMax[j] = -1e32;
                    xyzMin[j] = 1e32;
                }

                for (int i = 0; i < total; i++)
                {
                    wtest[i] = new double[3];
                    for (int j = 0; j < 3; j++)
                    {
                        wtest[i][j] =  rnd.NextDouble(); //*rnd.Next();
                        sw.Write(wtest[i][j] + " ");
                        xyzMax[j] = Math.Max(xyzMax[j], wtest[i][j]);
                        xyzMin[j] = Math.Min(xyzMin[j], wtest[i][j]);
                    }
                    sw.WriteLine();
                }
            }

            /*
             * build a new search data base to load data from demo.txt
             */
            octreeDataset = new Octree(
                (float)xyzMax[0], 
                (float)xyzMin[0], 
                (float)xyzMin[1], 
                (float)xyzMax[1], 
                (float)xyzMax[2], 
                (float)xyzMin[2], 
                total);

            using (StreamReader sr = new StreamReader("demo.txt"))
            {
                int i = 1;
                while (!sr.EndOfStream)
                {
                    string[] s = sr.ReadLine().Split(' ');
                    float x = float.Parse(s[0]);
                    float y = float.Parse(s[1]);
                    float z = float.Parse(s[2]);

                    octreeDataset.AddNode(x, y, z, i);
                    i++;
                }
            }

            /*
             * use this to test if the search finds all the elements loaded into octreeDataset
             * */
            /*
            ArrayList ar = new ArrayList();
            using (StreamReader sr = new StreamReader("demo.txt"))
            {
                int i = 1;
                while (!sr.EndOfStream)
                {
                    string[] s = sr.ReadLine().Split(' ');
                    float x = float.Parse(s[0]);
                    float y = float.Parse(s[1]);
                    float z = float.Parse(s[2]);

                    ar.Add((int)octreeDataset.GetNode(x, y, z));
                    i++;
                }
            }
             * */


        }
    }
}