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
using Newtonsoft.Json.Linq;
using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using System.Data;
using System.Text;

namespace OSRobot.Server.Plugins.RESTApiTask;

public class RESTApiTask : MultipleIterationTask
{
    private string _rawContent = string.Empty;
    private string _httpResult = string.Empty;
    private string _jsonPathData = string.Empty;

    private JContainer? ParseJson(string json)
    {
        JContainer? jParsedJson = null;

        try { jParsedJson = JObject.Parse(_rawContent); } catch { }
        if (jParsedJson == null)
        {
            try { jParsedJson = JArray.Parse(_rawContent); } catch { }
        }

        return jParsedJson;
    }

    protected override void RunMultipleIterationTask(int currentIteration)
    {
        using HttpClient client = new();
        RESTApiTaskConfig config = (RESTApiTaskConfig)_iterationTaskConfig;

        client.DefaultRequestHeaders.Clear();
        foreach (RESTApiHeader apiHeader in config.Headers)
        {
            client.DefaultRequestHeaders.Add(
                DynamicDataParser.ReplaceDynamicData(apiHeader.Name, _dataChain, currentIteration, _subInstanceIndex),
                DynamicDataParser.ReplaceDynamicData(apiHeader.Value, _dataChain, currentIteration, _subInstanceIndex)
                );
        }

        Task<HttpResponseMessage> taskResponse;
        HttpResponseMessage response;

        if (config.Method == MethodType.Get)
        {
            _instanceLogger?.Info(this, $"Connecting to: {config.URL} Method: GET");
            taskResponse = client.GetAsync(config.URL);
        }
        else if (config.Method == MethodType.Post)
        {
            _instanceLogger?.Info(this, $"Connecting to: {config.URL} Method: POST");
            StringContent contentParameters = new(config.Body, Encoding.UTF8, "application/json");
            taskResponse = client.PostAsync(config.URL, contentParameters);
        }
        else if (config.Method == MethodType.Put)
        {
            _instanceLogger?.Info(this, $"Connecting to: {config.URL} Method: PUT");
            StringContent contentParameters = new(config.Body, Encoding.UTF8, "application/json");
            taskResponse = client.PutAsync(config.URL, contentParameters);
        }
        else if (config.Method == MethodType.Delete)
        {
            _instanceLogger?.Info(this, $"Connecting to: {config.URL} Method: DELETE");
            taskResponse = client.DeleteAsync(config.URL);
        }
        else
            throw new ApplicationException($"Http method '{config.Method}' not supported.");

        // Wait for task to complete
        using (response = taskResponse.Result)
        {
            response.EnsureSuccessStatusCode();

            // Wait for task to complete
            _rawContent = response.Content.ReadAsStringAsync().Result;
            _httpResult = ((int)response.StatusCode).ToString();
        }

        if (!string.IsNullOrEmpty(config.JsonPathToData))
        {
            _instanceLogger?.Info(this, $"Extracting data from path \"{config.JsonPathToData}\"...");

            JContainer? jParsedJson = ParseJson(_rawContent) ?? throw new ApplicationException("Cannot parse JSON response");
            JToken? jJsonPathData = jParsedJson.SelectToken(config.JsonPathToData);

            if (jJsonPathData != null)
            {
                _jsonPathData = jJsonPathData.ToString();
                if (config.ReturnsRecordset)
                {
                    _instanceLogger?.Info(this, "Trying to deserialize JSON response...");
                    DataTable temp = JsonConvert.DeserializeObject<DataTable>(_jsonPathData) ?? throw new ApplicationException("Cannot deserialize JSON response");
                    _defaultRecordset = temp;
                    _instanceLogger?.Info(this, "Deserialization completed.");
                }
            }
        }
    }

    private void PostIteration(int currentIteration, ExecResult result, DynamicDataSet dDataSet)
    {
        RESTApiTaskConfig config = (RESTApiTaskConfig)_iterationTaskConfig;
        dDataSet.TryAdd(RESTApiTaskCommon.DynDataKeyURL, config.URL);
        dDataSet.TryAdd(RESTApiTaskCommon.DynDataKeyRawContent, _rawContent);
        dDataSet.TryAdd(RESTApiTaskCommon.DynDataKeyHttpResult, _httpResult);
        
        if (config != null && !string.IsNullOrEmpty(config.JsonPathToData))
        {
            dDataSet.TryAdd(RESTApiTaskCommon.DynDataKeyJsonPathData, _jsonPathData);

            if (config.ReturnsRecordset)
            {
                dDataSet.TryAdd(CommonDynamicData.DefaultRecordsetName, _defaultRecordset);
            }
        }
    }

    protected override void PostTaskSucceded(int currentIteration, ExecResult result, DynamicDataSet dDataSet)
    {
        PostIteration(currentIteration, result, dDataSet);
    }

    protected override void PostTaskFailed(int currentIteration, ExecResult result, DynamicDataSet dDataSet)
    {
        PostIteration(currentIteration, result, dDataSet);
    }
}
