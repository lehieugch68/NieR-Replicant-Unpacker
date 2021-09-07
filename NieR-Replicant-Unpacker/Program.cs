using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace NieR_Replicant_Unpacker
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Console.Title = "NieR Replicant ver.1.22474487139 Unpacker by LeHieu";
            string rootDir = folderBrowser("Game Location", "NieR Replicant ver.1.22474487139");
            if (rootDir == null) return; 
            string extractDir = folderBrowser("Extract Location", "Extracted");
            if (extractDir == null) return;
            Unpacker unpack = new Unpacker(rootDir);
            try
            {
                unpack.Unpack(extractDir, "data");
                unpack.Unpack(extractDir, @"dlc\dlc01");
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
            }
            Console.ReadKey();
        }

        static public string folderBrowser(string title, string folderName)
        {
            OpenFileDialog folderBrowser = new OpenFileDialog();
            folderBrowser.Title = title;
            folderBrowser.ValidateNames = false;
            folderBrowser.CheckFileExists = false;
            folderBrowser.CheckPathExists = true;
            folderBrowser.FileName = folderName;
            string result = null;
            if (folderBrowser.ShowDialog() == DialogResult.OK)
            {
                result = Path.GetDirectoryName(folderBrowser.FileName);
            }
            return result;
        }
    }
}
