// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using OSRobot.Server.Plugins.Infrastructure.Network;
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

    protected override async Task RunMultipleIterationTaskAsync(int currentIteration)
    {
        using HttpClient client = PluginServices.CreateHttpClient();
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
            taskResponse = client.GetAsync(config.URL, _cancellationToken);
        }
        else if (config.Method == MethodType.Post)
        {
            _instanceLogger?.Info(this, $"Connecting to: {config.URL} Method: POST");
            StringContent contentParameters = new(config.Body, Encoding.UTF8, "application/json");
            taskResponse = client.PostAsync(config.URL, contentParameters, _cancellationToken);
        }
        else if (config.Method == MethodType.Put)
        {
            _instanceLogger?.Info(this, $"Connecting to: {config.URL} Method: PUT");
            StringContent contentParameters = new(config.Body, Encoding.UTF8, "application/json");
            taskResponse = client.PutAsync(config.URL, contentParameters, _cancellationToken);
        }
        else if (config.Method == MethodType.Delete)
        {
            _instanceLogger?.Info(this, $"Connecting to: {config.URL} Method: DELETE");
            taskResponse = client.DeleteAsync(config.URL, _cancellationToken);
        }
        else
            throw new ApplicationException($"Http method '{config.Method}' not supported.");

        // Wait for the response, without blocking the thread while doing so.
        using (response = await taskResponse)
        {
            response.EnsureSuccessStatusCode();

            _rawContent = await response.Content.ReadAsStringAsync(_cancellationToken);
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
