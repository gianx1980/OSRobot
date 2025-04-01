/*======================================================================================
    Copyright 2025 by Gianluca Di Bucci (gianx1980) (https://www.os-robot.com)

    This file is part of OSRobot.

    OSRobot is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    OSRobot is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with OSRobot.  If not, see <http://www.gnu.org/licenses/>.
======================================================================================*/

using Newtonsoft.Json;
using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using System.Text;

namespace OSRobot.Server.Plugins.RESTApiTask;

public class RESTApiTask : IterationTask
{
    private string _rawContent = string.Empty;
    private string _httpResult = string.Empty;

    protected override void RunIteration(int currentIteration)
    {
        using HttpClient client = new();
        RESTApiTaskConfig tConfig = (RESTApiTaskConfig)_iterationConfig;

        client.DefaultRequestHeaders.Clear();
        foreach (RESTApiHeader apiHeader in tConfig.Headers)
        {
            client.DefaultRequestHeaders.Add(
                DynamicDataParser.ReplaceDynamicData(apiHeader.Name, _dataChain, currentIteration),
                DynamicDataParser.ReplaceDynamicData(apiHeader.Value, _dataChain, currentIteration)
                );
        }

        Task<HttpResponseMessage> taskResponse;
        HttpResponseMessage response;

        if (tConfig.Method == MethodType.Get)
        {
            _instanceLogger?.Info(this, $"Connecting to: {tConfig.URL} Method: GET");
            taskResponse = client.GetAsync(tConfig.URL);
        }
        else if (tConfig.Method == MethodType.Post)
        {
            _instanceLogger?.Info(this, $"Connecting to: {tConfig.URL} Method: POST");
            StringContent contentParameters = new(tConfig.Parameters, Encoding.UTF8, "application/json");
            taskResponse = client.PostAsync(tConfig.URL, contentParameters);
        }
        else if (tConfig.Method == MethodType.Put)
        {
            _instanceLogger?.Info(this, $"Connecting to: {tConfig.URL} Method: PUT");
            StringContent contentParameters = new(tConfig.Parameters, Encoding.UTF8, "application/json");
            taskResponse = client.PutAsync(tConfig.URL, contentParameters);
        }
        else if (tConfig.Method == MethodType.Delete)
        {
            _instanceLogger?.Info(this, $"Connecting to: {tConfig.URL} Method: DELETE");
            taskResponse = client.DeleteAsync(tConfig.URL);
        }
        else
            throw new ApplicationException($"Http method '{tConfig.Method}' not supported.");

        // Wait for task to complete
        using (response = taskResponse.Result)
        {
            response.EnsureSuccessStatusCode();

            // Wait for task to complete
            _rawContent = response.Content.ReadAsStringAsync().Result;
            _httpResult = ((int)response.StatusCode).ToString();
        }

        if (tConfig.ReturnsRecordset)
        {
            _instanceLogger?.Info(this, "Trying to deserialize JSON response...");
            List<Dictionary<string, object>>? temp = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(_rawContent) ?? throw new ApplicationException("Cannot deserialize JSON response");
            _defaultRecordset = temp;
            _instanceLogger?.Info(this, "Deserialization completed.");
        }
    }

    private void PostIteration(int currentIteration, ExecResult result, DynamicDataSet dDataSet)
    {
        RESTApiTaskConfig tConfig = (RESTApiTaskConfig)_iterationConfig;
        dDataSet.TryAdd(RESTApiTaskCommon.DynDataKeyURL, tConfig.URL);
        dDataSet.TryAdd(RESTApiTaskCommon.DynDataKeyRawContent, _rawContent);
        dDataSet.TryAdd(RESTApiTaskCommon.DynDataKeyHttpResult, _httpResult);
        if (tConfig != null && tConfig.ReturnsRecordset)
            dDataSet.TryAdd(CommonDynamicData.DefaultRecordsetName, _defaultRecordset);
    }

    protected override void PostIterationSucceded(int currentIteration, ExecResult result, DynamicDataSet dDataSet)
    {
        PostIteration(currentIteration, result, dDataSet);
    }

    protected override void PostIterationFailed(int currentIteration, ExecResult result, DynamicDataSet dDataSet)
    {
        PostIteration(currentIteration, result, dDataSet);
    }
}
