using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiProcessPairing
{
    public class ApiDatasetHandler : ProcessHandler<ApiDataModel>
    {
        public ApiDatasetHandler()
        {

        }

        public async Task<ApiDataModel> ActivateDatasetService(string datasetName, int port)
        {

            string executable = "executable.exe";
            string arguments = $"-- dataset {datasetName} --port {port}";
            string initializationConfirmationText = "running"; // this is the text that will be scanned for in the app output which will confirm that the exe has initialized and ready to receive requests


            var (Id, dataModel) = await Run(executable, arguments, initializationConfirmationText, new ApiDataModel()
            {
                DataSetName = datasetName,
                Port = port
            });

            return dataModel;
        }

    }


    // A simple data model for handling data we are saving against the process
    public class ApiDataModel
    {
        public string DataSetName { get; set; }
        public int Port { get; set; }
    }
}
