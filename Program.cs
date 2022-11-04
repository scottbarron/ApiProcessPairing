using System.Threading.Tasks;
using ApiProcessPairing;



async Task MainProcess()
{
    using (var apiDatasetHandler = new ApiDatasetHandler())
    {

        // launch the service that will be available at http://localhost:5000
        // this could be a third party service/executable for example
        var apiDataModel = await apiDatasetHandler.ActivateDatasetService("standard-data", 5000);


        // run the code to connect to the service
        HttpClient httpClient = new HttpClient();
        var response = httpClient.GetAsync($"http://localhost:{apiDataModel.Port}/path");
        var result = response.Result;
        var stringContent = await result.Content.ReadAsStringAsync();


        // the service will remaining running until the using clause is exited - then will be killed as part of the dispose cycle
    }
}


// execute and wait
MainProcess().Wait();




