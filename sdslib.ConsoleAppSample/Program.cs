using sdslib.ResourceTypes;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sdslib.ConsoleAppSample
{
    class Program
    {
        static void Main(string[] args)
        {
            var appSettings = ConfigurationManager.AppSettings;
            string MafiaIIRootPath = appSettings["MafiaIIRootPath"];
            string DesktopPath = appSettings["DesktopPath"];

            /* Init of SDS file */
            using (SdsFile sdsFile = SdsFile.FromFile($@"{MafiaIIRootPath}\pc\sds\mapa\mapa_city.sds"))
            {
                /* Extracts all resources from SDS into selected directory*/
                sdsFile.ExportToDirectory($@"{DesktopPath}");

                /* Extracts single resource to the selected path */
                Texture texture = sdsFile.GetResourceByTypeAndName<Texture>("map.dds");
                texture.Extract($@"{DesktopPath}\map.dds");

                /* Extracts all textures from current SDS (if contains any) */
                sdsFile.ExtractResourcesByType<Texture>($@"{DesktopPath}\mapa_city\textures");

                /* Replaces data of the selected file */
                Mipmap mipMap = sdsFile.GetResourceByTypeAndName<Mipmap>("map.dds");
                mipMap.ReplaceData($@"{DesktopPath}\new_map.dds");

                /* Saves modified SDS file to the selected path */
                sdsFile.ExportToFile($@"{DesktopPath}\modified.sds");
            }
        }
    }
}
